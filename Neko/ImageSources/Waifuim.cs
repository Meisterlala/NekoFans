using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Dalamud.Logging;


namespace Neko.Sources;

public class Waifuim : IImageSource
{

    public class Config : IImageConfig
    {
        public bool enabled = false;
        public bool nsfw = false;
        public bool sfw = true;
        public int ContentComboboxIndex = 0;

        public IImageSource? LoadConfig()
        {
            if (!enabled)
                return null;

            if (sfw && nsfw)
                return new CombinedSource(new Waifuim(false), new Waifuim(true));

            return new Waifuim(nsfw);
        }
    }

    private readonly MultiURLs<WaifuImJson> URLs;
    private readonly bool nsfw;

    public Waifuim(bool nsfw)
    {
        this.nsfw = nsfw;
        if (nsfw)
            URLs = new(
                "https://api.waifu.im/random/?is_nsfw=true&gif=false&many=true",
                ExtractStrings);
        else
            URLs = new(
                "https://api.waifu.im/random/?is_nsfw=false&gif=false&many=true",
                ExtractStrings);
    }

    public async Task<NekoImage> Next(CancellationToken ct = default)
    {
        string url = await URLs.GetURL();
        return await Common.DownloadImage(url, ct);
    }

    public override string ToString()
    {
        return $"waifu.im ({(nsfw ? "NSFW" : "SFW")}) Remaining urls: {URLs.URLCount}";
    }

    private List<String> ExtractStrings(WaifuImJson json)
    {
        List<String> res = new();
        foreach (var img in json.images)
        {
            res.Add(img.url);
        }
        return res;
    }

#pragma warning disable
    public class WaifuImJson
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
    }
#pragma warning restore
}
