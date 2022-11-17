using System.Collections.Generic;
using System.Threading;
using Neko.Drawing;

namespace Neko.Sources.APIS;

public class ShibeOnline : ImageSource
{
    public class Config : IImageConfig
    {
        public bool enabled;

        public ImageSource? LoadConfig() => enabled ? new ShibeOnline() : null;
    }
    public override string Name => "Shibe.online";

    private const int URL_COUNT = 5;
    private readonly MultiURLs<ShibeOnlineJson> URLs;

    public ShibeOnline() =>
        URLs = new("http://shibe.online/api/shibes?count=" + URL_COUNT + "&urls=true&httpsUrls=true", this);

    public override NekoImage Next(CancellationToken ct = default)
    {
        return new NekoImage(async (img) =>
        {
            var url = await URLs.GetURL(ct);
            img.URLDownloadWebsite = url;
            return await Download.DownloadImage(url, typeof(ShibeOnline), ct);
        }, this);
    }

    public override string ToString() => $"Shibe.online\t{URLs}";

    public override bool SameAs(ImageSource other) => true;

#pragma warning disable
    public class ShibeOnlineJson : List<string>, IJsonToList<string>
    {
        public List<string> ToList() => this;
    }
#pragma warning restore
}
