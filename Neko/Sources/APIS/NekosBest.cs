using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace Neko.Sources.APIS;

public class NekosBest : IImageSource
{
    public class Config : IImageConfig
    {
        public bool enabled;
        public Category categories = Category.None;

        public IImageSource? LoadConfig()
        {
            if (!enabled)
                return null;

            var com = new CombinedSource();
            foreach (var f in Helper.GetFlags(categories))
            {
                var category = Enum.GetName(typeof(Category), f)?.ToLower() ?? "unknown";
                com.AddSource(new NekosBest(category));
            }

            return com.Count() > 0 ? com : null;
        }

        [Flags]
        public enum Category
        {
            None = 0, Waifu = 1, Neko = 2, Kitsune = 4, Husbando = 8
        }
    }

    public bool Faulted { get; set; }

    private readonly string categoryName;
    private readonly string url;

    public NekosBest(string categoryName)
    {
        this.categoryName = categoryName;
        url = $"https://nekos.best/api/v2/{categoryName}";
    }

    public async Task<NekoImage> Next(CancellationToken ct = default)
    {
        var json = await Download.ParseJson<NekosBestJson>(url, ct);

        return json.Results is null || json.Results.Count == 0
            ? throw new Exception($"No results in response from {url}")
            : await Download.DownloadImage(json.Results[0].Url, typeof(NekosBest), ct);
    }

    public override string ToString() => $"nekos.best {categoryName}";

    public string Name => "nekos.best";

    public bool Equals(IImageSource? other) => other != null && other is NekosBest nb && nb.categoryName == categoryName;

#pragma warning disable
    public class NekosBestJson
    {
        [JsonPropertyName("results")]
        public List<Result> Results { get; set; }

        public class Result
        {
            [JsonPropertyName("artist_href")]
            public string ArtistHref { get; set; }

            [JsonPropertyName("artist_name")]
            public string ArtistName { get; set; }

            [JsonPropertyName("source_url")]
            public string SourceUrl { get; set; }

            [JsonPropertyName("url")]
            public string Url { get; set; }
        }
    }
#pragma warning restore
}
