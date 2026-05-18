using System;
using System.Collections.Generic;
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

    public static bool IsRateLimited;

    public static bool Is429Response(HttpResponseMessage response) =>
        response.RequestMessage?.RequestUri?.Host == "api.nekosia.cat";

    public class Config : IImageConfig
    {
        public bool enabled = true;
        public Category categories = Category.All;

        public ImageSource? LoadConfig()
        {
            if (!enabled)
                return null;

            var com = new CombinedSource();
            foreach (var f in Helper.GetFlags(categories))
            {
                if (f == Category.All)
                    continue;

                if (CategoryInfo.TryGetValue(f, out var info))
                    com.AddSource(new Nekosia(info.APIName));
                else
                    Plugin.Log.Error($"Nekosia: Unknown category {f}");
            }

            return com.Count() > 0 ? com : null;
        }
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

    public struct Info
    {
        public string DisplayName;
        public string APIName;
    }

    public static readonly Dictionary<Category, Info> CategoryInfo = new()
    {
        { Category.Random, new Info { DisplayName = "Random", APIName = "random" } },
        { Category.Catgirl, new Info { DisplayName = "Catgirl", APIName = "catgirl" } },
        { Category.Foxgirl, new Info { DisplayName = "Foxgirl", APIName = "foxgirl" } },
        { Category.Wolfgirl, new Info { DisplayName = "Wolfgirl", APIName = "wolfgirl" } },
        { Category.AnimalEars, new Info { DisplayName = "Animal Ears", APIName = "animal-ears" } },
        { Category.Tail, new Info { DisplayName = "Tail", APIName = "tail" } },
        { Category.TailWithRibbon, new Info { DisplayName = "Tail With Ribbon", APIName = "tail-with-ribbon" } },
        { Category.TailFromUnderSkirt, new Info { DisplayName = "Tail From Under Skirt", APIName = "tail-from-under-skirt" } },
        { Category.Cute, new Info { DisplayName = "Cute", APIName = "cute" } },
        { Category.CutenessIsJustice, new Info { DisplayName = "Cuteness Is Justice", APIName = "cuteness-is-justice" } },
        { Category.BlueArchive, new Info { DisplayName = "Blue Archive", APIName = "blue-archive" } },
        { Category.Girl, new Info { DisplayName = "Girl", APIName = "girl" } },
        { Category.YoungGirl, new Info { DisplayName = "Young Girl", APIName = "young-girl" } },
        { Category.Maid, new Info { DisplayName = "Maid", APIName = "maid" } },
        { Category.MaidUniform, new Info { DisplayName = "Maid Uniform", APIName = "maid-uniform" } },
        { Category.Vtuber, new Info { DisplayName = "VTuber", APIName = "vtuber" } },
        { Category.WSitting, new Info { DisplayName = "W Sitting", APIName = "w-sitting" } },
        { Category.LyingDown, new Info { DisplayName = "Lying Down", APIName = "lying-down" } },
        { Category.HandsFormingAHeart, new Info { DisplayName = "Hands Forming A Heart", APIName = "hands-forming-a-heart" } },
        { Category.Wink, new Info { DisplayName = "Wink", APIName = "wink" } },
        { Category.Valentine, new Info { DisplayName = "Valentine", APIName = "valentine" } },
        { Category.Headphones, new Info { DisplayName = "Headphones", APIName = "headphones" } },
        { Category.ThighHighSocks, new Info { DisplayName = "Thigh High Socks", APIName = "thigh-high-socks" } },
        { Category.KneeHighSocks, new Info { DisplayName = "Knee High Socks", APIName = "knee-high-socks" } },
        { Category.WhiteTights, new Info { DisplayName = "White Tights", APIName = "white-tights" } },
        { Category.BlackTights, new Info { DisplayName = "Black Tights", APIName = "black-tights" } },
        { Category.Heterochromia, new Info { DisplayName = "Heterochromia", APIName = "heterochromia" } },
        { Category.Uniform, new Info { DisplayName = "Uniform", APIName = "uniform" } },
        { Category.SailorUniform, new Info { DisplayName = "Sailor Uniform", APIName = "sailor-uniform" } },
        { Category.Hoodie, new Info { DisplayName = "Hoodie", APIName = "hoodie" } },
        { Category.Ribbon, new Info { DisplayName = "Ribbon", APIName = "ribbon" } },
        { Category.WhiteHair, new Info { DisplayName = "White Hair", APIName = "white-hair" } },
        { Category.BlueHair, new Info { DisplayName = "Blue Hair", APIName = "blue-hair" } },
        { Category.LongHair, new Info { DisplayName = "Long Hair", APIName = "long-hair" } },
        { Category.Blonde, new Info { DisplayName = "Blonde", APIName = "blonde" } },
        { Category.BlueEyes, new Info { DisplayName = "Blue Eyes", APIName = "blue-eyes" } },
        { Category.PurpleEyes, new Info { DisplayName = "Purple Eyes", APIName = "purple-eyes" } },
    };

    private readonly string category;
    private readonly MultiURLsGeneric<NekosiaJson, NekosiaJson.Result> urls;

    public Nekosia(string category)
    {
        this.category = category;
        urls = new(() => new HttpRequestMessage(HttpMethod.Get, $"{BaseUrl}/{category}?count={PageSize}&session=id&id={SessionId}"), this, 5);
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

    public override string ToString() => $"Nekosia {category} {urls}";
    public override string Name => "Nekosia";
    public override bool SameAs(ImageSource other) => other is Nekosia n && n.category == category;

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
                var artist = SafeTooltipValue(Attribution.Artist.Username ?? Attribution.Artist.Profile);
                if (!string.IsNullOrWhiteSpace(artist))
                    lines.Add($"Artist: {artist}");

                var source = SafeTooltipValue(Source.Url ?? Source.Direct) ?? "Unknown";
                var tags = Tags.Count > 0 ? string.Join(", ", Tags.ConvertAll(SafeTooltipValue)) : "None";
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
