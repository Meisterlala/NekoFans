using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json.Serialization;
using System.Threading;
using Neko.Drawing;

namespace Neko.Sources.APIS;

public class Nekosia : ImageSource
{
    private const string BaseUrl = "https://api.nekosia.cat/api/v1/images";
    private const int PageSize = 20;
    private static readonly string SessionId = Guid.NewGuid().ToString("N");

    internal static bool IsRateLimited;

    public enum Rating
    {
        Safe,
        Suggestive,
    }

    public static bool Is429Response(HttpResponseMessage response) =>
        response.RequestMessage?.RequestUri?.Host == "api.nekosia.cat";

    public class Config : IImageConfig, IJsonOnDeserialized
    {
        public bool enabled = true;
        public List<string> includeTags = DefaultIncludeTags();
        public List<string> excludeTags = DefaultExcludeTags();
        public Rating rating = Rating.Safe;

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public Category categories = Category.None;

        public void OnDeserialized()
        {
            if (categories == Category.None)
                return;

            includeTags = TagsFromCategories(categories);
            categories = Category.None;
        }

        public ImageSource? LoadConfig()
        {
            if (!enabled)
                return null;

            includeTags = NormalizeTags(includeTags);
            excludeTags = NormalizeTags(excludeTags);

            return includeTags.Count > 0 ? new Nekosia(includeTags, excludeTags, rating) : null;
        }
    }

    public static string RatingDisplayName(Rating rating) => rating switch
    {
        Rating.Suggestive => "Prefer Suggestive",
        _ => "SFW",
    };

    private static string RatingApiName(Rating rating) => rating switch
    {
        Rating.Suggestive => "suggestive",
        _ => "safe",
    };

    private static Rating EffectiveRating(Rating rating) => NSFW.AllowNSFW ? rating : Rating.Safe;

    public static List<string> NormalizeTags(IEnumerable<string>? tags)
        => tags?
            .Where(tag => !string.IsNullOrWhiteSpace(tag))
            .Select(CanonicalCategorySlug)
            .Where(tag => tag != null)
            .Select(tag => tag!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Order(StringComparer.OrdinalIgnoreCase)
            .ToList()
        ?? new List<string>();

    public static List<string> DefaultIncludeTags()
        => NormalizeTags([
            "animal-ears",
            "catgirl",
            "cute",
            "foxgirl",
            "maid",
            "tail",
            "wolfgirl",
        ]);

    public static List<string> DefaultExcludeTags()
        => NormalizeTags(["blue-archive"]);

    public static List<string> CategorySlugs()
        => Helper.GetFlags(Category.All)
            .Where(category => category != Category.All)
            .Select(CategorySlug)
            .Order(StringComparer.OrdinalIgnoreCase)
            .ToList();

    public static string CategoryDisplayName(string slug)
    {
        return string.Join(" ", slug.Split('-', StringSplitOptions.RemoveEmptyEntries).Select(HumanizeCategoryWord));
    }

    private static string? CanonicalCategorySlug(string tag)
    {
        var trimmed = tag.Trim();
        foreach (var slug in CategorySlugs())
            if (slug.Equals(trimmed, StringComparison.OrdinalIgnoreCase))
                return slug;

        return null;
    }

    private static string CategorySlug(Category category)
    {
        var name = Enum.GetName(category);
        if (string.IsNullOrEmpty(name))
            return "";

        return string.Concat(name.Select((character, index) => index > 0 && char.IsUpper(character) ? $"-{char.ToLowerInvariant(character)}" : char.ToLowerInvariant(character).ToString()));
    }

    private static string HumanizeCategoryWord(string word) => word switch
    {
        "vtuber" => "VTuber",
        "w" => "W",
        _ => char.ToUpperInvariant(word[0]) + word[1..],
    };

    private static List<string> TagsFromCategories(Category categories)
    {
        var tags = new List<string>();
        foreach (var flag in Helper.GetFlags(categories))
        {
            if (flag == Category.All)
                continue;

            tags.Add(CategorySlug(flag));
        }

        return NormalizeTags(tags);
    }

    [Flags]
    public enum Category : long
    {
        None = 0,
        Random = 1L << 0,
        Catgirl = 1L << 1,
        Foxgirl = 1L << 2,
        Wolfgirl = 1L << 3,
        AnimalEars = 1L << 4,
        Tail = 1L << 5,
        TailWithRibbon = 1L << 6,
        TailFromUnderSkirt = 1L << 7,
        Cute = 1L << 8,
        CutenessIsJustice = 1L << 9,
        BlueArchive = 1L << 10,
        Girl = 1L << 11,
        YoungGirl = 1L << 12,
        Maid = 1L << 13,
        MaidUniform = 1L << 14,
        Vtuber = 1L << 15,
        WSitting = 1L << 16,
        LyingDown = 1L << 17,
        HandsFormingAHeart = 1L << 18,
        Wink = 1L << 19,
        Valentine = 1L << 20,
        Headphones = 1L << 21,
        ThighHighSocks = 1L << 22,
        KneeHighSocks = 1L << 23,
        WhiteTights = 1L << 24,
        BlackTights = 1L << 25,
        Heterochromia = 1L << 26,
        Uniform = 1L << 27,
        SailorUniform = 1L << 28,
        Hoodie = 1L << 29,
        Ribbon = 1L << 30,
        WhiteHair = 1L << 31,
        BlueHair = 1L << 32,
        LongHair = 1L << 33,
        Blonde = 1L << 34,
        BlueEyes = 1L << 35,
        PurpleEyes = 1L << 36,
        All = Random | Catgirl | Foxgirl | Wolfgirl | AnimalEars | Tail | TailWithRibbon | TailFromUnderSkirt |
              Cute | CutenessIsJustice | BlueArchive | Girl | YoungGirl | Maid | MaidUniform | Vtuber | WSitting |
              LyingDown | HandsFormingAHeart | Wink | Valentine | Headphones | ThighHighSocks | KneeHighSocks |
              WhiteTights | BlackTights | Heterochromia | Uniform | SailorUniform | Hoodie | Ribbon | WhiteHair |
              BlueHair | LongHair | Blonde | BlueEyes | PurpleEyes,
    }

    private readonly List<string> includeTags;
    private readonly List<string> excludeTags;
    private readonly Rating rating;
    private readonly string sourceKey;
    private readonly MultiURLsGeneric<NekosiaJson, NekosiaJson.Result> urls;

    public Nekosia(List<string> includeTags, List<string> excludeTags, Rating rating)
    {
        this.includeTags = NormalizeTags(includeTags);
        this.excludeTags = NormalizeTags(excludeTags);
        this.rating = rating;
        sourceKey = SourceKey(this.includeTags, this.excludeTags, rating);
        urls = new(() => new HttpRequestMessage(HttpMethod.Get, RequestUrl()), this, 5);
    }

    private string RequestUrl()
    {
        var included = NormalizeTags(includeTags);
        var includedSet = new HashSet<string>(included, StringComparer.OrdinalIgnoreCase);
        var excluded = NormalizeTags(excludeTags).Where(tag => !includedSet.Contains(tag)).ToList();

        var parameters = new List<string>
        {
            $"count={PageSize}",
            $"rating={Uri.EscapeDataString(RatingApiName(EffectiveRating(rating)))}",
            $"session=id",
            $"id={Uri.EscapeDataString(SessionId)}",
        };

        if (included.Count > 0)
            parameters.Add($"additionalTags={Uri.EscapeDataString(string.Join(",", included))}");

        if (excluded.Count > 0)
            parameters.Add($"blacklistedTags={Uri.EscapeDataString(string.Join(",", excluded))}");

        return $"{BaseUrl}/nothing?{string.Join("&", parameters)}";
    }

    public override NekoImage Next(CancellationToken ct = default)
    {
        return new NekoImage(async (img) =>
        {
            var result = await urls.GetURL(ct).ConfigureAwait(false);
            var url = result.Image.Original.Url;
            img.URLDownloadWebsite = url;
            img.URLOpenOnClick = result.Source.Url ?? result.Source.Direct ?? url;
            img.Description = result.Tooltip();
            return await Download.DownloadImage(url, typeof(Nekosia), ct).ConfigureAwait(false);
        }, this);
    }

    public override string ToString() => $"Nekosia {string.Join(", ", includeTags)} {urls}";
    public override string Name => "Nekosia";
    public override bool SameAs(ImageSource other) => other is Nekosia n && n.sourceKey == sourceKey;

    private static string SourceKey(List<string> includeTags, List<string> excludeTags, Rating rating)
    {
        var included = NormalizeTags(includeTags);
        var includedSet = new HashSet<string>(included, StringComparer.OrdinalIgnoreCase);
        var excluded = NormalizeTags(excludeTags).Where(tag => !includedSet.Contains(tag)).ToList();

        return string.Join("\n", included)
           + "\n--\n"
           + string.Join("\n", excluded)
           + $"\n--\n{rating}";
    }

#pragma warning disable
    public class NekosiaJson : IJsonToList<NekosiaJson.Result>
    {
        [JsonPropertyName("images")]
        public List<Result>? Images { get; set; }

        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("image")]
        public ImageData Image { get; set; }

        [JsonPropertyName("category")]
        public string Category { get; set; }

        [JsonPropertyName("tags")]
        public List<string> Tags { get; set; }

        [JsonPropertyName("source")]
        public SourceData Source { get; set; }

        [JsonPropertyName("attribution")]
        public AttributionData Attribution { get; set; }

        public List<Result> ToList()
        {
            if (Images is { Count: > 0 })
                return Images;

            return [new Result { Id = Id, Image = Image, Category = Category, Tags = Tags, Source = Source, Attribution = Attribution }];
        }

        public class Result
        {
            [JsonPropertyName("id")]
            public string Id { get; set; }

            [JsonPropertyName("image")]
            public ImageData Image { get; set; }

            [JsonPropertyName("category")]
            public string Category { get; set; }

            [JsonPropertyName("tags")]
            public List<string> Tags { get; set; }

            [JsonPropertyName("source")]
            public SourceData Source { get; set; }

            [JsonPropertyName("attribution")]
            public AttributionData Attribution { get; set; }

            public string Tooltip()
            {
                List<string> lines = new();
                var artist = SafeTooltipValue(Attribution?.Artist?.Username ?? Attribution?.Artist?.Profile);
                if (!string.IsNullOrWhiteSpace(artist))
                    lines.Add($"Artist: {artist}");

                var source = SafeTooltipValue(Source?.Url ?? Source?.Direct) ?? "Unknown";
                var tags = Tags is { Count: > 0 } ? string.Join(", ", Tags.Select(tag => SafeTooltipValue(CategoryDisplayName(tag)))) : "None";
                lines.Add($"Source: {source}");
                lines.Add($"Tags: {tags}");
                return string.Join("\n", lines);
            }

            private static string? SafeTooltipValue(string? value)
                => value?.Replace("%", "%%").Replace("\r", " ").Replace("\n", " ");
        }

        public class ImageData
        {
            [JsonPropertyName("original")]
            public ImageUrl Original { get; set; }
        }

        public class ImageUrl
        {
            [JsonPropertyName("url")]
            public string Url { get; set; }
        }

        public class SourceData
        {
            [JsonPropertyName("url")]
            public string? Url { get; set; }

            [JsonPropertyName("direct")]
            public string? Direct { get; set; }
        }

        public class AttributionData
        {
            [JsonPropertyName("artist")]
            public Artist Artist { get; set; }
        }

        public class Artist
        {
            [JsonPropertyName("username")]
            public string? Username { get; set; }

            [JsonPropertyName("profile")]
            public string? Profile { get; set; }
        }
    }
#pragma warning restore
}
