using System.Threading;
using Neko.Drawing;

namespace Neko.Sources.APIS;

public class Catboys : IImageSource
{
    public class Config : IImageConfig
    {
        public bool enabled;

        public IImageSource? LoadConfig() => enabled ? new Catboys() : null;
    }

    public bool Faulted { get; set; }

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

    public NekoImage Next(CancellationToken ct = default)
    {
        const string url = "https://api.catboys.com/img";
        return new NekoImage(async (_) =>
        {
            var response = await Download.ParseJson<CatboysJson>(url, ct);
            return await Download.DownloadImage(response.url, typeof(Catboys), ct);
        });
    }

    public override string ToString() => "Catboys";
    public string Name => "Catboys";

    public bool Equals(IImageSource? other) => other != null && other.GetType() == typeof(Catboys);
}
