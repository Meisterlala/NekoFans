using System.Threading;
using Neko.Drawing;

namespace Neko.Sources.APIS;

public class NekosLife : IImageSource
{
    public class Config : IImageConfig
    {
        public bool enabled = true;

        public IImageSource? LoadConfig() => enabled ? new NekosLife() : null;
    }

    public bool Faulted { get; set; }

    public string Name => "Nekos.life";

#pragma warning disable
    public class NekosLifeJson
    {
        public string url { get; set; }
    }
#pragma warning restore

    public NekoImage Next(CancellationToken ct = default)
    {
        const string url = "https://nekos.life/api/v2/img/neko";
        return new NekoImage(async (_) =>
        {
            var response = await Download.ParseJson<NekosLifeJson>(url, ct);
            return await Download.DownloadImage(response.url, typeof(NekosLife), ct);
        });
    }

    public override string ToString() => "Nekos.life";

    public bool Equals(IImageSource? other) => other != null && other.GetType() == typeof(NekosLife);
}
