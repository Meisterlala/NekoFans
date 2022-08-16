using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Dalamud.Logging;


namespace Neko.Sources;

public class ShibeOnline : IImageSource
{
    public class Config : IImageConfig
    {
        public bool enabled = false;

        public IImageSource? LoadConfig()
        {
            if (enabled)
                return new ShibeOnline();
            return null;
        }
    }

    private const int URL_COUNT = 100;
    private static readonly MultiURLs<ShibeOnlineJson> URLs = new(
            "http://shibe.online/api/shibes?count=" + URL_COUNT + "&urls=true&httpsUrls=true");

    public async Task<NekoImage> Next(CancellationToken ct = default)
    {
        string url = await URLs.GetURL();
        return await Common.DownloadImage(url, ct);
    }

    public override string ToString()
    {
        return "Shibe.online\tURLs: " + URLs.URLCount;
    }

#pragma warning disable
    public class ShibeOnlineJson : List<string>, IJsonToList
    {
        public List<string> ToList() => this;
    }
#pragma warning restore

}
