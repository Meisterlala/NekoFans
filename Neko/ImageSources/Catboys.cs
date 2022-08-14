using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Dalamud.Logging;


namespace Neko.Sources;

    public class Catboys : IImageSource
    {
        public class Config : IImageConfig
        {
            public bool enabled = false;

            public IImageSource? LoadConfig()
            {
                if (enabled)
                    return new Catboys();
                return null;
            }
        }


#pragma warning disable
        public class CatboysJson
        {
            public string url { get; set; }
            public string artist { get; set; }
            public string artist_url { get; set; }
            public string source_url { get; set; }
            public string error { get; set; }
        }
#pragma warning restore

        public async Task<NekoImage> Next(CancellationToken ct = default)
        {
            var url = "https://api.catboys.com/img";
            CatboysJson response = await Common.ParseJson<CatboysJson>(url, ct);
            return await Common.DownloadImage(response.url, ct);
        }

        public override string ToString()
        {
            return "Catboys";
        }

    }
