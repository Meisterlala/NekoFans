using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using System.Threading;
using Neko.Drawing;

namespace Neko.Sources.APIS;

public class NekosBest : ImageSource
{
    public class Config : IImageConfig
    {
        public bool enabled = true;
        public Category categories = Category.Neko;

        public ImageSource? LoadConfig()
        {
            if (!enabled)
                return null;

            var com = new CombinedSource();
            foreach (var f in Helper.GetFlags(categories))
            {
                if (CategoryInfo.TryGetValue(f, out var info))
                    com.AddSource(new NekosBest(info.APIName));
                else
                    Plugin.Log.Error($"NekosBest: Unknown category {f}");
            }

            return com.Count() > 0 ? com : null;
        }
    }

    [Flags]
    public enum Category : long
    {
        None = 0,
        /* Images */
        Waifu = 1L << 0,
        Neko = 1L << 1,
        Kitsune = 1L << 2,
        Husbando = 1L << 3,
        /* GIFs */
        Baka = 1L << 4,
        Bite = 1L << 5,
        Blush = 1L << 6,
        Bored = 1L << 7,
        Cry = 1L << 8,
        Cuddle = 1L << 9,
        Dance = 1L << 10,
        Facepalm = 1L << 11,
        Feed = 1L << 12,
        Handhold = 1L << 13,
        Happy = 1L << 14,
        Highfive = 1L << 15,
        Hug = 1L << 16,
        Kick = 1L << 17,
        Kiss = 1L << 18,
        Laugh = 1L << 19,
        Pat = 1L << 20,
        Poke = 1L << 21,
        Pout = 1L << 22,
        Punch = 1L << 23,
        Shoot = 1L << 24,
        Slap = 1L << 25,
        Sleep = 1L << 26,
        Smile = 1L << 27,
        Smug = 1L << 28,
        Stare = 1L << 29,
        Think = 1L << 30,
        Thumbsup = 1L << 31,
        Tickle = 1L << 32,
        Wave = 1L << 33,
        Wink = 1L << 34,
    }

    public struct Info
    {
        public string DisplayName;
        public string APIName;
    }

    public static readonly Dictionary<Category, Info> CategoryInfo = new() {
        { Category.Waifu,   new Info{ DisplayName = "Waifu",          APIName = "waifu"} },
        { Category.Neko,    new Info{ DisplayName = "Neko",           APIName = "neko"} },
        { Category.Kitsune, new Info{ DisplayName = "Kitsune",        APIName = "kitsune"} },
        { Category.Husbando,new Info{ DisplayName = "Husbando",       APIName = "husbando"} },

        { Category.Baka,    new Info{ DisplayName = "Baka GIF",       APIName = "baka"}},
        { Category.Bite,    new Info{ DisplayName = "Bite GIF",       APIName = "bite"}},
        { Category.Blush,   new Info{ DisplayName = "Blush GIF",      APIName = "blush"}},
        { Category.Bored,   new Info{ DisplayName = "Bored GIF",      APIName = "bored"}},
        { Category.Cry,     new Info{ DisplayName = "Cry GIF",        APIName = "cry"}},
        { Category.Cuddle,  new Info{ DisplayName = "Cuddle GIF",     APIName = "cuddle"}},
        { Category.Dance,   new Info{ DisplayName = "Dance GIF",      APIName = "dance"}},
        { Category.Facepalm,new Info{ DisplayName = "Facepalm GIF",   APIName = "facepalm"}},
        { Category.Feed,    new Info{ DisplayName = "Feed GIF",       APIName = "feed"}},
        { Category.Handhold,new Info{ DisplayName = "Handhold GIF",   APIName = "handhold"}},
        { Category.Happy,   new Info{ DisplayName = "Happy GIF",      APIName = "happy"}},
        { Category.Highfive,new Info{ DisplayName = "Highfive GIF",   APIName = "highfive"}},
        { Category.Hug,     new Info{ DisplayName = "Hug GIF",        APIName = "hug"}},
        { Category.Kick,    new Info{ DisplayName = "Kick GIF",       APIName = "kick"}},
        { Category.Kiss,    new Info{ DisplayName = "Kiss GIF",       APIName = "kiss"}},
        { Category.Laugh,   new Info{ DisplayName = "Laugh GIF",      APIName = "laugh"}},
        { Category.Pat,     new Info{ DisplayName = "Pat GIF",        APIName = "pat"}},
        { Category.Poke,    new Info{ DisplayName = "Poke GIF",       APIName = "poke"}},
        { Category.Pout,    new Info{ DisplayName = "Pout GIF",       APIName = "pout"}},
        { Category.Punch,   new Info{ DisplayName = "Punch GIF",      APIName = "punch"}},
        { Category.Shoot,   new Info{ DisplayName = "Shoot GIF",      APIName = "shoot"}},
        { Category.Slap,    new Info{ DisplayName = "Slap GIF",       APIName = "slap"}},
        { Category.Sleep,   new Info{ DisplayName = "Sleep GIF",      APIName = "sleep"}},
        { Category.Smile,   new Info{ DisplayName = "Smile GIF",      APIName = "smile"}},
        { Category.Smug,    new Info{ DisplayName = "Smug GIF",       APIName = "smug"}},
        { Category.Stare,   new Info{ DisplayName = "Stare GIF",      APIName = "stare"}},
        { Category.Think,   new Info{ DisplayName = "Think GIF",      APIName = "think"}},
        { Category.Thumbsup,new Info{ DisplayName = "Thumbsup GIF",   APIName = "thumbsup"}},
        { Category.Tickle,  new Info{ DisplayName = "Tickle GIF",     APIName = "tickle"}},
        { Category.Wave,    new Info{ DisplayName = "Wave GIF",       APIName = "wave"}},
        { Category.Wink,    new Info{ DisplayName = "Wink GIF",       APIName = "wink"}},
    };

    private readonly string categoryName;
    private readonly MultiURLs<NekosBestJson> urls;

    public NekosBest(string categoryName)
    {
        this.categoryName = categoryName;
        urls = new($"https://nekos.best/api/v2/{categoryName}?amount=10", this, 5);
    }

    public override NekoImage Next(CancellationToken ct = default)
    {
        return new NekoImage(async (img) =>
        {
            var url = await urls.GetURL(ct).ConfigureAwait(false);
            img.URLDownloadWebsite = url;
            return await Download.DownloadImage(url, typeof(NekosBest), ct).ConfigureAwait(false);
        }, this);
    }

    public override string ToString() => $"nekos.best {categoryName} {urls}";
    public override string Name => "nekos.best";

    public override bool SameAs(ImageSource other) => other is NekosBest nb && nb.categoryName == categoryName;

#pragma warning disable
    public class NekosBestJson : IJsonToList<string>
    {
        [JsonPropertyName("results")]
        public List<Result> Results { get; set; }

        public List<string> ToList()
        {
            var list = new List<string>();

            if (Results is null || Results.Count == 0)
                throw new Exception($"No results in response from nekos.best");

            foreach (var result in Results)
                list.Add(result.Url);
            return list;
        }

        public class Result
        {
            [JsonPropertyName("artist_href")]
            public string? ArtistHref { get; set; }

            [JsonPropertyName("artist_name")]
            public string? ArtistName { get; set; }

            [JsonPropertyName("source_url")]
            public string? SourceUrl { get; set; }

            [JsonPropertyName("url")]
            public string Url { get; set; }
        }
    }
#pragma warning restore
}
