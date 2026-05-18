using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using System.Threading;
using Neko.Drawing;

namespace Neko.Sources.APIS;

public class Purrbot(string path) : ImageSource
{
    private const string BaseUrl = "https://api.purrbot.site/v2";

    public class Config : IImageConfig
    {
        public bool enabled = true;
        public Category categories = Category.Images;

        public ImageSource? LoadConfig()
        {
            if (!enabled)
                return null;

            var com = new CombinedSource();
            foreach (var f in Helper.GetFlags(categories))
            {
                if (!CategoryInfo.TryGetValue(f, out var info))
                {
                    Plugin.Log.Error($"Purrbot: Unknown category {f}");
                    continue;
                }

                if (info.NSFW && !NSFW.AllowNSFW)
                    continue;

                foreach (var path in info.Paths)
                    com.AddSource(new Purrbot(path));
            }

            return com.Count() > 0 ? com : null;
        }
    }

    [Flags]
    public enum Category : long
    {
        None = 0,
        GIFs = 1L << 0,
        Images = 1L << 1,
        NSFWGIFs = 1L << 2,
        NSFWImages = 1L << 3,
    }

    public readonly struct Info
    {
        public Info() => NSFW = false;

        public required string DisplayName { get; init; }
        public required string[] Paths { get; init; }
        public bool NSFW { get; init; }
    }

    public static readonly Dictionary<Category, Info> CategoryInfo = new()
    {
        { Category.GIFs, new Info { DisplayName = "Reaction GIFs", Paths = [
            "/img/sfw/angry/gif", "/img/sfw/bite/gif", "/img/sfw/blush/gif", "/img/sfw/comfy/gif", "/img/sfw/cry/gif",
            "/img/sfw/cuddle/gif", "/img/sfw/dance/gif", "/img/sfw/eevee/gif", "/img/sfw/fluff/gif", "/img/sfw/hug/gif",
            "/img/sfw/kiss/gif", "/img/sfw/lay/gif", "/img/sfw/lick/gif", "/img/sfw/neko/gif", "/img/sfw/pat/gif",
            "/img/sfw/poke/gif", "/img/sfw/pout/gif", "/img/sfw/slap/gif", "/img/sfw/smile/gif", "/img/sfw/tail/gif",
            "/img/sfw/tickle/gif",
        ] } },
        { Category.Images, new Info { DisplayName = "Images", Paths = [
            "/img/sfw/eevee/img", "/img/sfw/holo/img", "/img/sfw/kitsune/img", "/img/sfw/neko/img", "/img/sfw/senko/img",
            "/img/sfw/shiro/img",
        ] } },
        { Category.NSFWGIFs, new Info { DisplayName = "NSFW GIFs", NSFW = true, Paths = [
            "/img/nsfw/anal/gif", "/img/nsfw/blowjob/gif", "/img/nsfw/cum/gif", "/img/nsfw/fuck/gif", "/img/nsfw/neko/gif",
            "/img/nsfw/pussylick/gif", "/img/nsfw/solo/gif", "/img/nsfw/solo_male/gif", "/img/nsfw/threesome_fff/gif",
            "/img/nsfw/threesome_ffm/gif", "/img/nsfw/threesome_mmf/gif", "/img/nsfw/yaoi/gif", "/img/nsfw/yuri/gif",
        ] } },
        { Category.NSFWImages, new Info { DisplayName = "NSFW Images", NSFW = true, Paths = [
            "/img/nsfw/neko/img",
        ] } },
    };

    private readonly string path = path;

    public override NekoImage Next(CancellationToken ct = default)
    {
        return new NekoImage(async (img) =>
        {
            var url = BaseUrl + path;
            img.URLDownloadWebsite = url;
            var json = await Download.ParseJson<PurrbotJson>(url, ct).ConfigureAwait(false);
            img.URLDownloadWebsite = json.Link;
            return await Download.DownloadImage(json.Link, typeof(Purrbot), ct).ConfigureAwait(false);
        }, this);
    }

    public override string ToString() => $"Purrbot {path}";
    public override string Name => "Purrbot";
    public override bool SameAs(ImageSource other) => other is Purrbot p && p.path == path;

#pragma warning disable
    public class PurrbotJson
    {
        [JsonPropertyName("link")]
        public string Link { get; set; }

        [JsonPropertyName("error")]
        public bool Error { get; set; }

        [JsonPropertyName("time")]
        public int Time { get; set; }
    }
#pragma warning restore
}
