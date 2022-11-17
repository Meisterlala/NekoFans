using System;
using System.Collections.Generic;
using System.Threading;
using Neko.Drawing;

namespace Neko.Sources.APIS;

public class Waifuim : ImageSource
{
    public class Config : IImageConfig
    {
        public bool enabled;
        public bool nsfw;
        public bool sfw = true;
        public int ContentComboboxIndex;

        public ImageSource? LoadConfig()
        {
            return !enabled
            ? null
            : sfw && nsfw && NSFW.AllowNSFW
            ? new CombinedSource(new Waifuim(false), new Waifuim(true))
            : new Waifuim(nsfw && NSFW.AllowNSFW);
        }
    }

    public override string Name => "waifu.im";

    private readonly MultiURLs<WaifuImJson> URLs;
    private readonly bool nsfw;

    public Waifuim(bool nsfw)
    {
        this.nsfw = nsfw && NSFW.AllowNSFW; // NSFW Check
        URLs = this.nsfw
            ? (new("https://api.waifu.im/random/?is_nsfw=true&gif=false&many=true", this))
            : (new("https://api.waifu.im/random/?is_nsfw=false&gif=false&many=true", this));
    }

    public override NekoImage Next(CancellationToken ct = default)
    {
        return new NekoImage(async (img) =>
        {
            var url = await URLs.GetURL(ct);
            img.URLDownloadWebsite = url;
            return await Download.DownloadImage(url, typeof(Waifuim), ct);
        }, this);
    }

    public override string ToString() => $"waifu.im ({(nsfw ? "NSFW" : "SFW")})\t{URLs}";

    public override bool SameAs(ImageSource other) => other is Waifuim w && w.nsfw == nsfw;

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
            public int favourites { get; set; }
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
