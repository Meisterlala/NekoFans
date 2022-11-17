using System.Threading;
using Neko.Drawing;

namespace Neko.Sources.APIS;

public class Catboys : ImageSource
{
    public class Config : IImageConfig
    {
        public bool enabled;

        public ImageSource? LoadConfig() => enabled ? new Catboys() : null;
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

    public override NekoImage Next(CancellationToken ct = default)
    {
        const string url = "https://api.catboys.com/img";
        return new NekoImage(async (img) =>
        {
            var response = await Download.ParseJson<CatboysJson>(url, ct);
            img.URLDownloadWebsite = response.url;
            return await Download.DownloadImage(response.url, typeof(Catboys), ct);
        }, this);
    }

    public override string ToString() => "Catboys";
    public override string Name => "Catboys";

    public override bool SameAs(ImageSource other) => true;
}
