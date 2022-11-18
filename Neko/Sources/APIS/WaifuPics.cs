using System;
using System.Threading;
using Neko.Drawing;

namespace Neko.Sources.APIS;

public class WaifuPics : ImageSource
{
    [Flags]
    public enum CategoriesSFW
    {
        None = 0, Waifu = 1, Neko = 2, Shinobu = 4, Megumin = 8, Awoo = 16
    }

    [Flags]
    public enum CategoriesNSFW
    {
        None = 0, Waifu = 1, Neko = 2, Trap = 4
    }

    public class Config : IImageConfig
    {
        public bool enabled = true;
        public CategoriesSFW sfwCategories = CategoriesSFW.Neko;
        public CategoriesNSFW nsfwCategories = CategoriesNSFW.None;

        public ImageSource? LoadConfig()
        {
            if (!enabled)
                return null;

            var comSFW = new CombinedSource();
            foreach (var f in Helper.GetFlags(sfwCategories))
            {
                var category = Enum.GetName(typeof(CategoriesSFW), f)?.ToLower() ?? "unknown";
                comSFW.AddSource(new WaifuPics("sfw", category));
            }

            var comNSFW = new CombinedSource();
            foreach (var f in Helper.GetFlags(nsfwCategories))
            {
                var category = Enum.GetName(typeof(CategoriesNSFW), f)?.ToLower() ?? "unknown";
                comNSFW.AddSource(new WaifuPics("nsfw", category));
            }

            return comSFW.Count() > 0 && comNSFW.Count() > 0 && NSFW.AllowNSFW
                ? new CombinedSource(comSFW, comNSFW)
                : comSFW.Count() > 0
                ? comSFW
                : comNSFW.Count() > 0 && NSFW.AllowNSFW
                ? comNSFW
                : null;
        }
    }

    private readonly string url;
    private readonly string type;
    private readonly string category;

    public WaifuPics(string type, string category)
    {
        url = $"https://api.waifu.pics/{type}/{category}";
        this.type = type;
        this.category = category;
    }

    public override NekoImage Next(CancellationToken ct = default)
    {
        return new NekoImage(async (img) =>
        {
            img.URLDownloadWebsite = url;
            var json = await Download.ParseJson<WaifuPicsJson>(url, ct).ConfigureAwait(false);
            img.URLDownloadWebsite = json.url;
            return await Download.DownloadImage(json.url, typeof(WaifuPics), ct).ConfigureAwait(false);
        }, this);
    }

    public override string ToString() => $"Waifu Pics ({type.ToUpper()}) {category}";

    public override string Name => "Waifu Pics";

    public override bool SameAs(ImageSource other) => other is WaifuPics w && w.type == type && w.category == category;

#pragma warning disable
    public class WaifuPicsJson
    {
        public string url { get; set; }
    }
#pragma warning restore
}
