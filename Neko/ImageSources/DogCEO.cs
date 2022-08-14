using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Dalamud.Logging;


namespace Neko.Sources;

public class DogCEO : IImageSource
{
    public class Config : IImageConfig
    {
        public bool enabled = false;

        public IImageSource? LoadConfig()
        {
            if (enabled)
                return new DogCEO();
            return null;
        }
    }

    private const int URL_COUNT = 10; // max 50
    private static readonly MultiURLs<DogCEOJson> URLs = new(
            "https://dog.ceo/api/breeds/image/random/" + URL_COUNT);


    public async Task<NekoImage> Next(CancellationToken ct = default)
    {
        var url = await URLs.GetURL();
        return await Common.DownloadImage(url, ct);
    }

    public override string ToString()
    {
        return "Dog CEO\tURLs: " + URLs.URLCount;
    }

#pragma warning disable
    public class DogCEOJson : IJsonToList
    {
        public List<string> message { get; set; }
        public string status { get; set; }

        public List<string> ToList() => message;
    }
#pragma warning restore
}