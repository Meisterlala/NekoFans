using System.Threading;
using System.Threading.Tasks;

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

    public async Task<NekoImage> Next(CancellationToken ct = default)
    {
        var url = "https://api.catboys.com/img";
        var response = await Common.ParseJson<CatboysJson>(url, ct);
        return await Common.DownloadImage(response.url, ct);
    }

    public override string ToString() => "Catboys";

}
