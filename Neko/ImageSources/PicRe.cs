using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Dalamud.Logging;


namespace Neko.Sources;

public class PicRe : IImageSource
{
    public class Config : IImageConfig
    {
        public bool enabled = false;

        public IImageSource? LoadConfig()
        {
            if (enabled)
                return new PicRe();
            return null;
        }
    }

    public async Task<NekoImage> Next(CancellationToken ct = default)
    {
        var url = "https://pic.re/images";
        return await Common.DownloadImage(url, ct);
    }

    public override string ToString()
    {
        return "PicRe";
    }

}