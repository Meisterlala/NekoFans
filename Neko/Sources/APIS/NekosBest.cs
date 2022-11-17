using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using System.Threading;
using Neko.Drawing;

namespace Neko.Sources.APIS;

public class NekosBest : ImageSource
{
    public class Config : IImageConfig
    {
        public bool enabled = true;
        public Category categories = Category.Neko;

        public ImageSource? LoadConfig()
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

    private readonly string categoryName;
    private readonly MultiURLs<NekosBestJson> urls;

    public NekosBest(string categoryName)
    {
        this.categoryName = categoryName;
        urls = new($"https://nekos.best/api/v2/{categoryName}?amount=20", this, 15);
    }

    public override NekoImage Next(CancellationToken ct = default)
    {
        return new NekoImage(async (img) =>
        {
            var url = await urls.GetURL(ct);
            img.URLDownloadWebsite = url;
            return await Download.DownloadImage(url, typeof(NekosBest), ct);
        }, this);
    }

    public override string ToString() => $"nekos.best {categoryName} {urls}";
    public override string Name => "nekos.best";

    public override bool SameAs(ImageSource other) => other is NekosBest nb && nb.categoryName == categoryName;

#pragma warning disable
    public class NekosBestJson : IJsonToList<string>
    {
        [JsonPropertyName("results")]
        public List<Result> Results { get; set; }

        public List<string> ToList()
        {
            var list = new List<string>();

            if (Results is null || Results.Count == 0)
                throw new Exception($"No results in response from nekos.best");

            foreach (var result in Results)
                list.Add(result.Url);
            return list;
        }

        public class Result
        {
            [JsonPropertyName("artist_href")]
            public string? ArtistHref { get; set; }

            [JsonPropertyName("artist_name")]
            public string? ArtistName { get; set; }

            [JsonPropertyName("source_url")]
            public string? SourceUrl { get; set; }

            [JsonPropertyName("url")]
            public string Url { get; set; }
        }
    }
#pragma warning restore
}
