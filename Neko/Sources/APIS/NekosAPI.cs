using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json.Serialization;
using System.Threading;
using Neko.Drawing;

namespace Neko.Sources.APIS;

public class NekosAPI : ImageSource
{
    private const string BaseUrl = "https://api.nekosapi.com/v4/images";
    private const int Limit = 25;

    [Flags]
    public enum Rating
    {
        None = 0,
        Safe = 1 << 0,
        Suggestive = 1 << 1,
        Borderline = 1 << 2,
        Explicit = 1 << 3,
    }

    public class Config : IImageConfig, IJsonOnDeserialized
    {
        public bool enabled = true;
        public Rating ratings = Rating.Safe;

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public Rating rating = Rating.Safe;
        public int offset;

        public void OnDeserialized()
        {
            if (ratings == Rating.None)
                ratings = rating == Rating.None ? Rating.Safe : rating;

            rating = Rating.Safe;
        }

        public ImageSource? LoadConfig() => enabled ? new NekosAPI(this) : null;
    }

    public static string RatingDisplayName(Rating rating) => rating switch
    {
        Rating.Safe => "SFW",
        Rating.Suggestive => "Suggestive",
        Rating.Borderline => "Borderline",
        Rating.Explicit => "Explicit",
        Rating.None => throw new NotImplementedException(),
        _ => throw new NotImplementedException(),
    };

    public static string RatingPreview(Rating ratings)
    {
        var effectiveRatings = EffectiveRatings(ratings);
        var names = Helper.GetFlags(effectiveRatings).Select(RatingDisplayName).ToList();
        return names.Count > 0 ? string.Join(", ", names) : RatingDisplayName(Rating.Safe);
    }

    private static string RatingApiName(Rating rating) => rating switch
    {
        Rating.Safe => "safe",
        Rating.Suggestive => "suggestive",
        Rating.Borderline => "borderline",
        Rating.Explicit => "explicit",
        Rating.None => throw new NotImplementedException(),
        _ => throw new NotImplementedException(),
    };

    private static Rating EffectiveRatings(Rating ratings) => !NSFW.AllowNSFW ? Rating.Safe : ratings == Rating.None ? Rating.Safe : ratings;

    private readonly Config config;
    private readonly Rating ratings;
    private readonly NekosAPIURLs urls;

    public NekosAPI(Config config)
    {
        this.config = config;
        ratings = EffectiveRatings(config.ratings);
        urls = new NekosAPIURLs(() => new HttpRequestMessage(HttpMethod.Get, RequestUrl()), this, config);
    }

    private string RequestUrl()
    {
        var offset = Math.Max(0, config.offset);
        var ratingParameter = string.Join(",", Helper.GetFlags(ratings).Select(RatingApiName));
        return $"{BaseUrl}?limit={Limit}&offset={offset}&rating={Uri.EscapeDataString(ratingParameter)}";
    }

    public override NekoImage Next(CancellationToken ct = default)
    {
        return new NekoImage(async (img) =>
        {
            var result = await urls.GetURL(ct).ConfigureAwait(false);
            img.URLDownloadWebsite = result.Url;
            img.URLOpenOnClick = result.OpenInBrowserUrl();
            img.Description = result.Tooltip();
            return await Download.DownloadImage(result.Url, typeof(NekosAPI), ct).ConfigureAwait(false);
        }, this);
    }

    public override string ToString() => $"NekosAPI {RatingPreview(ratings)} offset {config.offset} {urls}";
    public override string Name => "NekosAPI";
    public override bool SameAs(ImageSource other) => other is NekosAPI n && n.ratings == ratings;

    private sealed class NekosAPIURLs(Func<HttpRequestMessage> requestGen, ImageSource caller, Config config)
        : MultiURLsGeneric<NekosAPIJson, NekosAPIJson.Image>(requestGen, caller, Limit)
    {
        private readonly Config config = config;

        protected override void OnTaskSuccessfull(NekosAPIJson result)
        {
            base.OnTaskSuccessfull(result);
            config.offset = result.NextOffset(config.offset);
            Plugin.Config.Save();
        }
    }

#pragma warning disable
    public class NekosAPIJson : IJsonToList<NekosAPIJson.Image>
    {
        [JsonPropertyName("items")]
        public List<Image> Items { get; set; }

        [JsonPropertyName("count")]
        public int Count { get; set; }

        public List<Image> ToList()
        {
            if (Items is not { Count: > 0 })
                throw new Exception("No results in response from NekosAPI");

            return Items;
        }

        public int NextOffset(int currentOffset)
        {
            if (Count <= 0 || Items is not { Count: > 0 })
                return 0;

            var nextOffset = Math.Max(0, currentOffset) + Items.Count;
            return nextOffset >= Count ? 0 : nextOffset;
        }

        public class Image
        {
            [JsonPropertyName("id")]
            public int Id { get; set; }

            [JsonPropertyName("url")]
            public string Url { get; set; }

            [JsonPropertyName("rating")]
            public string Rating { get; set; }

            [JsonPropertyName("artist_name")]
            public string? ArtistName { get; set; }

            [JsonPropertyName("tags")]
            public List<string> Tags { get; set; }

            [JsonPropertyName("source_url")]
            public string? SourceUrl { get; set; }

            public string Tooltip()
            {
                List<string> lines = new();
                var artist = SafeTooltipValue(ArtistName);
                if (!string.IsNullOrWhiteSpace(artist))
                    lines.Add($"Artist: {artist}");

                lines.Add($"Tags: {(Tags is { Count: > 0 } ? string.Join(", ", Tags.Select(DisplayTag)) : "None")}");
                return string.Join("\n", lines);
            }

            public string OpenInBrowserUrl() => string.IsNullOrWhiteSpace(SourceUrl) ? Url : SourceUrl;

            private static string DisplayTag(string tag)
            {
                var normalized = SafeTooltipValue(tag)?.Replace('_', ' ').Replace('-', ' ').Trim();
                return string.IsNullOrEmpty(normalized)
                    ? "Unknown"
                    : char.ToUpperInvariant(normalized[0]) + normalized[1..];
            }

            private static string? SafeTooltipValue(string? value)
                => value?.Replace("%", "%%").Replace("\r", " ").Replace("\n", " ");
        }
    }
#pragma warning restore
}
