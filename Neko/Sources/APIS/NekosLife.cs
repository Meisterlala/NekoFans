using System.Threading;
using System.Threading.Tasks;

namespace Neko.Sources.APIS;

public class NekosLife : IImageSource
{
    public class Config : IImageConfig
    {
        public bool enabled = true;

        public IImageSource? LoadConfig() => enabled ? new NekosLife() : (IImageSource?)null;
    }

#pragma warning disable
    class NekosLifeJson
    {
        public string url { get; set; }
    }
#pragma warning restore

    public async Task<NekoImage> Next(CancellationToken ct = default)
    {
        var url = "https://nekos.life/api/v2/img/neko";
        // Get a random image URL
        var response = await Common.ParseJson<NekosLifeJson>(url, ct);
        // Download  image
        return await Common.DownloadImage(response.url, ct); ;
    }

    public override string ToString() => "Nekos.life";

}

