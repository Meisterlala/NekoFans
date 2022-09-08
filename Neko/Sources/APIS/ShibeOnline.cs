using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;


namespace Neko.Sources.APIS;

public class ShibeOnline : IImageSource
{
    public class Config : IImageConfig
    {
        public bool enabled;

        public IImageSource? LoadConfig() => enabled ? new ShibeOnline() : null;
    }

    public bool Faulted { get; set; }

    public string Name => "Shibe.online";

    private const int URL_COUNT = 5;
    private readonly MultiURLs<ShibeOnlineJson> URLs;

    public ShibeOnline() =>
        URLs = new("http://shibe.online/api/shibes?count=" + URL_COUNT + "&urls=true&httpsUrls=true", this);

    public async Task<NekoImage> Next(CancellationToken ct = default)
    {
        var url = await URLs.GetURL(ct);
        return await Common.DownloadImage(url, ct);
    }

    public override string ToString() => $"Shibe.online\t{URLs}";

    public bool Equals(IImageSource? other) => other != null && other.GetType() == typeof(ShibeOnline);

#pragma warning disable
    public class ShibeOnlineJson : List<string>, IJsonToList<string>
    {
        public List<string> ToList() => this;
    }
#pragma warning restore
}
