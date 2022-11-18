using System.Threading;
using Neko.Drawing;

namespace Neko.Sources.APIS;

public class PicRe : ImageSource
{
    public class Config : IImageConfig
    {
        public bool enabled;

        public ImageSource? LoadConfig() => enabled ? new PicRe() : null;
    }

    public override string Name => "PicRe";

    public override NekoImage Next(CancellationToken ct = default)
    {
        const string url = "https://pic.re/images";
        return new NekoImage(async (_)
            => await Download.DownloadImage(url, typeof(PicRe), ct).ConfigureAwait(false), this)
        {
            URLDownloadWebsite = url
        };
    }

    public override string ToString() => "PicRe";

    public override bool SameAs(ImageSource other) => true;
}
