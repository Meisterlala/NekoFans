using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Dalamud.Logging;


namespace Neko.Sources
{
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

        private const int URLCount = 100;
        private static readonly MultiURLs<List<string>> URLs = new(
                "http://shibe.online/api/shibes?count=" + URLCount + "&urls=true&httpsUrls=true",
                (list) => list);

        public async Task<NekoImage> Next(CancellationToken ct = default)
        {
            string url = await URLs.GetURL();
            return await Common.DownloadImage(url, ct);
        }

        public override string ToString()
        {
            return "Shibe.online Remaining urls:" + URLs.URLCount;
        }

    }
}