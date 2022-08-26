using System.Threading;
using System.Threading.Tasks;


namespace Neko.Sources.APIS;

public class PicRe : IImageSource
{
    public class Config : IImageConfig
    {
        public bool enabled;

        public IImageSource? LoadConfig() => enabled ? new PicRe() : null;
    }

    public async Task<NekoImage> Next(CancellationToken ct = default)
    {
        var url = "https://pic.re/images";
        return await Common.DownloadImage(url, ct);
    }

    public override string ToString() => "PicRe";

}
