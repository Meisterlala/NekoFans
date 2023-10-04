using System;
using System.Collections.Generic;
using System.Threading;
using Neko.Drawing;

namespace Neko.Sources.APIS;

public class NekosLife : ImageSource
{
    public class Config : IImageConfig
    {
        public bool enabled = true;
        public Category categories = Category.Neko | Category.Pat;

        public ImageSource? LoadConfig()
        {
            if (!enabled || categories == Category.None)
                return null;

            var flags = Helper.GetFlags(categories);
            var comb = new CombinedSource();
            foreach (var flag in flags)
            {
                if (CategoryInfo.TryGetValue(flag, out var info))
                {
                    if (info.NSFW && !NSFW.AllowNSFW)
                        continue;
                    comb.AddSource(new NekosLife(info.APIName));
                }
                else
                {
                    Plugin.Log.Error($"NekosLife: Unknown category {flag}");
                }
            }
            return comb.Count() > 0 ? comb : null;
        }
    }

    [Flags]
    public enum Category
    {
        None = 0,
        /* Images */
        Neko = 1 << 0,
        Lizard = 1 << 1,
        Meow = 1 << 2,
        /* GIFs */
        Cuddle = 1 << 3,
        Feed = 1 << 4,
        FoxGirl = 1 << 5,
        Hug = 1 << 6,
        Kiss = 1 << 7,
        Ngif = 1 << 8,
        Pat = 1 << 9,
        Slap = 1 << 10,
        Smug = 1 << 11,
        Spank = 1 << 12,
        Tickle = 1 << 13,
    }

    public struct Info
    {
        public string DisplayName;
        public string APIName;
        public bool NSFW;
    }

    public static readonly Dictionary<Category, Info> CategoryInfo = new() {
        { Category.Neko,    new Info{ DisplayName = "Neko",             APIName = "neko"} },
        { Category.Lizard,  new Info{ DisplayName = "Lizard",           APIName = "lizard"} },
        { Category.Meow,    new Info{ DisplayName = "Meow",             APIName = "meow"} },
        { Category.Cuddle,  new Info{ DisplayName = "Cuddle GIF",       APIName = "cuddle"} },
        { Category.Feed,    new Info{ DisplayName = "Feed GIF",         APIName = "feed"} },
        { Category.FoxGirl, new Info{ DisplayName = "Fox Girl",         APIName = "fox_girl"} },
        { Category.Hug,     new Info{ DisplayName = "Hug GIF",          APIName = "hug"} },
        { Category.Kiss,    new Info{ DisplayName = "Kiss GIF",         APIName = "kiss"} },
        { Category.Ngif,    new Info{ DisplayName = "Neko GIF (NSFW)",  APIName = "ngif", NSFW = true} },
        { Category.Pat,     new Info{ DisplayName = "Pat GIF",          APIName = "pat"} },
        { Category.Slap,    new Info{ DisplayName = "Slap GIF",         APIName = "slap"} },
        { Category.Smug,    new Info{ DisplayName = "Smug GIF",         APIName = "smug"} },
        { Category.Spank,   new Info{ DisplayName = "Spank GIF (NSFW)", APIName = "spank", NSFW = true} },
        { Category.Tickle,  new Info{ DisplayName = "Tickle GIF",       APIName = "tickle"} },
    };

    public override string Name => "Nekos.life";
    public override string ToString() => $"Nekos.life {Endpoint}";
    private readonly string Endpoint;

    public override bool SameAs(ImageSource other) => other is NekosLife nl && nl.Endpoint == Endpoint;

    public NekosLife(string endpoint) => Endpoint = endpoint;

    public override NekoImage Next(CancellationToken ct = default)
    {
        var url = $"https://nekos.life/api/v2/img/{Endpoint}";
        return new NekoImage(async (img) =>
        {
            img.URLDownloadWebsite = url;
            var response = await Download.ParseJson<NekosLifeJson>(url, ct).ConfigureAwait(false);
            img.URLDownloadWebsite = response.url;
            return await Download.DownloadImage(response.url, typeof(NekosLife), ct).ConfigureAwait(false);
        }, this);
    }

#pragma warning disable
    public class NekosLifeJson
    {
        public string url { get; set; }
    }
#pragma warning restore
}
