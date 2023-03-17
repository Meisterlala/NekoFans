using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using Neko.Drawing;

namespace Neko.Sources.APIS;

public class Waifuim : ImageSource
{
    public class Config : IImageConfig
    {
        public bool enabled;
        public Category categories = Category.SFW | Category.SFWGIF;

        public ImageSource? LoadConfig()
        {
            if (!enabled)
                return null;
            var com = new CombinedSource();
            foreach (var f in Helper.GetFlags(categories))
            {
                if (CategoryInfo.TryGetValue(f, out var info))
                {
                    if (info.NSFW && !NSFW.AllowNSFW)
                        continue;
                    com.AddSource(new Waifuim(info));
                }
                else
                {
                    Dalamud.Logging.PluginLog.LogError($"Waifuim: Unknown category {f}");
                }
            }
            return com.Count() > 0 ? com : null;
        }
    }

    [Flags]
    public enum Category
    {
        None = 0,

        SFW = 1 << 0,
        SFWGIF = 1 << 1,
        NSFW = 1 << 2,
        NSFWGIF = 1 << 3,
    }

    public struct Info
    {
        public string DisplayName;
        public bool NSFW;
        public bool GIF;
    }

    public static readonly Dictionary<Category, Info> CategoryInfo = new()
    {
        { Category.SFW,     new Info { DisplayName = "Images",      NSFW = false, GIF = false } },
        { Category.SFWGIF,  new Info { DisplayName = "GIFs",  NSFW = false, GIF = true  } },
        { Category.NSFW,    new Info { DisplayName = "NSFW Images",     NSFW = true,  GIF = false } },
        { Category.NSFWGIF, new Info { DisplayName = "NSFW GIFs", NSFW = true,  GIF = true  } },
    };

    public override string Name => "waifu.im";
    public override string ToString() => $"waifu.im {CurrentInfo.DisplayName}\t{URLs}";

    private readonly MultiURLs<WaifuImJson> URLs;
    private readonly Info CurrentInfo;

    private const string ApiVersion = "v5";

    public Waifuim(Info i)
    {
        CurrentInfo = i;
        var nsfw = CurrentInfo.NSFW && NSFW.AllowNSFW ? "true" : "false";
        var gif = CurrentInfo.GIF ? "true" : "false";

        HttpRequestMessage requestWithVersion()
        {
            HttpRequestMessage request = new(HttpMethod.Get, $"https://api.waifu.im/search/?is_nsfw={nsfw}&gif={gif}&many=true");
            request.Headers.Add("Accept-Version", ApiVersion);
            return request;
        }

        URLs = new(requestWithVersion, this);
    }

    public override NekoImage Next(CancellationToken ct = default)
    {
        return new NekoImage(async (img) =>
        {
            var url = await URLs.GetURL(ct).ConfigureAwait(false);
            img.URLDownloadWebsite = url;
            return await Download.DownloadImage(url, typeof(Waifuim), ct).ConfigureAwait(false);
        }, this);
    }

    public override bool SameAs(ImageSource other) => other is Waifuim w && w.CurrentInfo.NSFW == CurrentInfo.NSFW && w.CurrentInfo.GIF == CurrentInfo.GIF;

#pragma warning disable
    public class WaifuImJson : IJsonToList<string>
    {
        public class Image
        {
            public class Tag
            {
                public int tag_id { get; set; }
                public string name { get; set; }
                public string description { get; set; }
                public bool is_nsfw { get; set; }
            }

            public string file { get; set; }
            public string extension { get; set; }
            public int image_id { get; set; }
            public int favorites { get; set; }
            public string dominant_color { get; set; }
            public string source { get; set; }
            public DateTime uploaded_at { get; set; }
            public bool is_nsfw { get; set; }
            public int width { get; set; }
            public int height { get; set; }
            public string url { get; set; }
            public string preview_url { get; set; }
            public List<Tag> tags { get; set; }
        }
        public List<Image> images { get; set; }

        public List<string> ToList()
        {
            List<string> res = new();
            foreach (var img in images)
            {
                res.Add(img.url);
            }
            return res;
        }
    }
#pragma warning restore
}
