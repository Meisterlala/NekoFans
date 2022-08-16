using System;
using System.Threading;
using System.Threading.Tasks;

namespace Neko.Sources.APIS;

public class WaifuPics : IImageSource
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

        public IImageSource? LoadConfig()
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
                : (IImageSource?)null;
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

    public async Task<NekoImage> Next(CancellationToken ct = default)
    {
        var json = await Common.ParseJson<WaifuPicsJson>(url, ct);
        return await Common.DownloadImage(json.url, ct);
    }

    public override string ToString() => $"Waifu Pics ({type.ToUpper()}) {category}";

#pragma warning disable
    class WaifuPicsJson
    {
        public string url { get; set; }
    }
#pragma warning restore
}
