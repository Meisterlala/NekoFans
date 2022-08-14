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
        private bool isOffline = false;
        private const int URLCount = 100;
        private const int URLThreshold = 1;

        private readonly ConcurrentQueue<String> shibeURLs = new();
        private Task? getNewURLs;


        public ShibeOnline()
        {
            GetURLsAsync();
        }

        private void GetURLsAsync()
        {
            getNewURLs = GetURLs();
            getNewURLs.ContinueWith((task) =>
            {
                foreach (var ex in task.Exception?.Flatten().InnerExceptions ?? new(Array.Empty<Exception>()))
                {
                    PluginLog.LogError(ex.ToString());
                    isOffline = true;
                }
                getNewURLs = null;
            });
        }

        public async Task<NekoImage> Next(CancellationToken ct = default)
        {
            if (isOffline)
                return await NekoImage.DefaultNeko();

            // Get new Urls to images
            if (shibeURLs.Count < URLThreshold && getNewURLs == null)
                GetURLsAsync();

            // Wait if empty
            if (shibeURLs.IsEmpty && getNewURLs != null)
                await getNewURLs;

            shibeURLs.TryDequeue(out string? url);

            if (url == null)
                throw new Exception("Could not Dequeue shibe url");

            return await Common.DownloadImage(url, ct);
        }

        private async Task GetURLs()
        {
            var url = "http://shibe.online/api/shibes?count=" + URLCount + "&urls=true&httpsUrls=true";

            var urls = await Common.ParseJson<List<string>>(url);

            foreach (var s in urls)
            {
                shibeURLs.Enqueue(s);
            }
        }

        public override string ToString()
        {
            return "Shibe.online Remaining urls:" + shibeURLs.Count;
        }
    }
}