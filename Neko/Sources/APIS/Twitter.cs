using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;


namespace Neko.Sources.APIS;

public class Twitter : IImageSource
{
    public class Config : IImageConfig
    {
        public bool enabled;

        public IImageSource? LoadConfig() => enabled ? new NekosLife() : null;
    }

    private static readonly string URLSearch = "https://api.twitter.com/2/tweets/search/recent?query=";
    private readonly string URL;

    public Twitter(string search, string user)
    {
    }

    public Twitter(string search)
    {
        URL = URLSearch + search;
    }

    private void Authenticate(){

    }

    public Task<NekoImage> Next(CancellationToken ct = default) => throw new System.NotImplementedException();
}