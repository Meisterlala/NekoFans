using System.Threading;
using Neko.Drawing;

namespace Neko.Sources.APIS;

public class PicRe : IImageSource
{
    public class Config : IImageConfig
    {
        public bool enabled;

        public IImageSource? LoadConfig() => enabled ? new PicRe() : null;
    }

    public bool Faulted { get; set; }

    public string Name => "PicRe";

    public NekoImage Next(CancellationToken ct = default)
    {
        const string url = "https://pic.re/images";
        return new NekoImage(async (_)
            => await Download.DownloadImage(url, typeof(PicRe), ct));
    }

    public override string ToString() => "PicRe";

    public bool Equals(IImageSource? other) => other != null && other.GetType() == typeof(PicRe);
}
