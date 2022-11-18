using System.Threading;
using Neko.Drawing;

namespace Neko.Sources.APIS;

public class NekosLife : ImageSource
{
    public class Config : IImageConfig
    {
        public bool enabled = true;

        public ImageSource? LoadConfig() => enabled ? new NekosLife() : null;
    }

    public override string Name => "Nekos.life";

#pragma warning disable
    public class NekosLifeJson
    {
        public string url { get; set; }
    }
#pragma warning restore

    public override NekoImage Next(CancellationToken ct = default)
    {
        const string url = "https://nekos.life/api/v2/img/neko";
        return new NekoImage(async (img) =>
        {
            img.URLDownloadWebsite = url;
            var response = await Download.ParseJson<NekosLifeJson>(url, ct).ConfigureAwait(false);
            img.URLDownloadWebsite = response.url;
            return await Download.DownloadImage(response.url, typeof(NekosLife), ct).ConfigureAwait(false);
        }, this);
    }

    public override string ToString() => "Nekos.life";

    public override bool SameAs(ImageSource other) => true;
}
