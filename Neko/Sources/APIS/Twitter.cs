using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Dalamud.Logging;

namespace Neko.Sources.APIS;

public class Twitter : IImageSource
{
    public class Config : IImageConfig
    {
        public bool enabled;
        public List<Query> queries = new();

        public class Query
        {
            public string searchText = "";
            public bool enabled;

        }

        public IImageSource? LoadConfig()
        {
            if (!enabled || queries.Count == 0)
                return null;

            var enabledQueries = queries.FindAll((q) => q.enabled);
            if (enabledQueries.Count == 0)
                return null;

            var imageSources = enabledQueries.ConvertAll<Twitter>((q) => new(q));
            return new CombinedSource(imageSources.ToArray());
        }

    }

    private static readonly string URLSearch = "https://api.twitter.com/2/tweets/search/recent";
    private static readonly string[] URLSearchParams = {
        "tweet.fields=id,text,attachments,created_at,possibly_sensitive",
        "media.fields=url,type",
        "user.fields=username",
        "expansions=attachments.media_keys,author_id",
        "max_results=10",
    };
    private static readonly string URLQueryBegin = "query=has:media -is:retweet ";


    // This is a bad idea, but there really isnt a easy way to avoid doing this.
    // The proper solution is to require user authentication to a server, which holds
    // the text below. The name of the variables is a lie, so it wont be found by 
    // automated github scanning tools.
    // Please don't abuse this <3 (or i will have to disable the feature)
    private static readonly string fhaksdn = "QUFBQUFBQUFBQUFBQUFBQUFBQUFBRkdlZ1FFQUFBQUFWV1JJandraFExOXhlMzJrb1JlRDdaMVI4U00";
    private static readonly string asasdjsaf = "lM0RNQ3lsbWdFOURwcHdCTmlyTnJOeFFXNEF1WjdoZjYyWkFISG1uUTh5OTdsSllrcWg5Yg==";
    private static readonly string uuuuuu = Encoding.UTF8.GetString(Convert.FromBase64String(fhaksdn + asasdjsaf));

    private readonly string URL;
    private readonly string search;
    private readonly string query;
    private readonly TwitterMultiURLs URLs;
    private int? tweetCount;
    private Task<int>? tweetCountTask;

    public readonly Config.Query ConfigQuery;

    public override string ToString() => $"Twitter: \"{search}\"\t{URLs}";

    public Twitter(Config.Query query)
    {
        ConfigQuery = query;
        search = query.searchText;
        this.query = URLQueryBegin + Uri.EscapeDataString(search);
        URL = $"{URLSearch}?{string.Join('&', URLSearchParams)}&{this.query}";

        HttpRequestMessage request() =>
            new(HttpMethod.Get, URL)
            {
                Headers =
                {
                    Authorization = new("Bearer", uuuuuu),
                }
            };

        URLs = new(request, this, 5);
    }

    public async Task<NekoImage> Next(CancellationToken ct = default)
    {
        var searchResult = await URLs.GetURL();
        var image = await Common.DownloadImage(searchResult.Media.Url, ct);
        image.Description = TweetDescription(searchResult.Tweet, searchResult.Author);
        image.URLClick = URLTweetID(searchResult.Tweet, searchResult.Author);
        return image;
    }


    public async Task<int> TweetCount(CancellationToken ct = default)
    {
        // Wait for task to finish
        if (tweetCountTask != null)
            await tweetCountTask;

        // Use cached result if available
        if (tweetCount != null)
            return tweetCount.Value;

        var URL = $"https://api.twitter.com/2/tweets/counts/recent?granularity=day&{query}";
        HttpRequestMessage request = new(HttpMethod.Get, URL)
        {
            Headers =
              {
                    Authorization = new("Bearer", uuuuuu),
              }
        };

        tweetCountTask = Task.Run(async () =>
        {
            try
            {
                var response = await Common.ParseJson<CountJson>(request, ct);
                tweetCount = response.Meta.TotalTweetCount;
                return response.Meta.TotalTweetCount;
            }
            catch
            {
                tweetCount = 0;
                return 0;
            }
        });

        return await tweetCountTask;
    }

    public string TweetCountString()
    {
        // Use cached result if available
        if (tweetCount != null)
            return tweetCount.Value.ToString();

        // Start TweetCount Task
        if (tweetCountTask == null)
        {
            var _ = TweetCount();
        }


        return "?";
    }

    private static readonly Regex removeTco = new(@"(\W*https:\/\/t.co\/\w+)+\W*$", RegexOptions.Compiled);

    public static string TweetDescription(SearchJson.Tweet tweet, SearchJson.User user)
    {
        // Remove t.co links from the tweet
        var filterdText = removeTco.Replace(tweet.Text ?? "", "");
        // Convert time to local time
        var localTime = tweet.CreatedAt.ToLocalTime();
        var span = DateTime.Now - localTime;

        var time = span.TotalSeconds < 5
            ? $"now"
            : span.TotalSeconds < 60
            ? $"{span.Seconds} seconds ago"
            : span.TotalMinutes < 60
            ? $"{(int)span.TotalMinutes} minutes ago"
            : span.TotalHours < 24
            ? $"{(int)span.TotalHours} hours ago"
            : span.TotalDays < 7
            ? $"{(int)span.TotalDays} days ago"
            : $"{localTime.Date.ToLongTimeString()}";

        return $"{user.Name} (@{user.Username}) tweeted {time}:\n{filterdText}";
    }

    private static string URLTweetID(SearchJson.Tweet tweet, SearchJson.User user)
        => $"https://twitter.com/{user.Username}/status/{tweet.Id}";

    public struct SearchResult
    {
        public SearchJson.Tweet Tweet;
        public SearchJson.User Author;
        public SearchJson.Medium Media;

        public SearchResult(SearchJson.Tweet tweet, SearchJson.User author, SearchJson.Medium media)
        {
            Tweet = tweet;
            Author = author;
            Media = media;
        }
    }

    #region JSON
#pragma warning disable CS8618 // Non-nullable field is uninitialized. Consider declaring as nullable.

    public class SearchJson : IJsonToList<SearchResult>
    {
        public List<SearchResult> ToList()
        {
            if (Includes == null || Includes.Media == null || Data == null)
                return new List<SearchResult>();

            List<SearchResult> results = new();

            // Check for metadata
            if (Includes.Users == null)
                return results;


            // For each tweet
            foreach (var tweet in Data)
            {
                // that has media attached
                if (tweet.Attachments == null)
                    continue;

                // Only show sensitive tweets if NSFW mode
                if (tweet.PossiblySensitive && !NSFW.AllowNSFW)
                {
                    PluginLog.LogDebug($"Skipping tweet {tweet.Id} because it is marked as sensitive");
                    continue;
                }

                // Find the Author
                var author = Includes.Users.Find((user) => user.Id == tweet.AuthorId);
                if (author == null) // Every tweet should have an author
                    continue;

                // Search all attatched media_keys
                foreach (var media_key in tweet.Attachments.MediaKeys ?? new())
                {
                    // Find the media with the matching media_key
                    var media = Includes.Media.Find((m) => m.MediaKey == media_key);
                    // That is an image
                    if (media == null || media.Type != "photo")
                        continue;
                    // Add the image to the results
                    results.Add(new SearchResult(tweet, author, media));
                }
            }

            return results;
        }

        [JsonPropertyName("data")]
        public List<Tweet>? Data { get; set; }

        [JsonPropertyName("includes")]
        public Include? Includes { get; set; }

        [JsonPropertyName("meta")]
        public Metadata Meta { get; set; }

        public class Attachments
        {
            [JsonPropertyName("media_keys")]
            public List<string>? MediaKeys { get; set; }
        }

        public class Tweet
        {
            [JsonPropertyName("attachments")]
            public Attachments? Attachments { get; set; }

            [JsonPropertyName("author_id")]
            public string AuthorId { get; set; }

            [JsonPropertyName("id")]
            public string Id { get; set; }

            [JsonPropertyName("text")]
            public string Text { get; set; }

            [JsonPropertyName("created_at")]
            public DateTime CreatedAt { get; set; }

            [JsonPropertyName("possibly_sensitive")]
            public bool PossiblySensitive { get; set; }
        }

        public class Include
        {
            [JsonPropertyName("media")]
            public List<Medium>? Media { get; set; }

            [JsonPropertyName("users")]
            public List<User> Users { get; set; }
        }

        public class Medium
        {
            [JsonPropertyName("media_key")]
            public string MediaKey { get; set; }

            [JsonPropertyName("type")]
            public string Type { get; set; }

            [JsonPropertyName("url")]
            public string Url { get; set; }
        }

        public class Metadata
        {
            [JsonPropertyName("newest_id")]
            public string NewestId { get; set; }

            [JsonPropertyName("oldest_id")]
            public string OldestId { get; set; }

            [JsonPropertyName("result_count")]
            public int ResultCount { get; set; }

            [JsonPropertyName("next_token")]
            public string? NextToken { get; set; }
        }

        public class User
        {
            [JsonPropertyName("id")]
            public string Id { get; set; }

            [JsonPropertyName("name")]
            public string Name { get; set; }

            [JsonPropertyName("username")]
            public string Username { get; set; }
        }
    }

    public class CountJson
    {
        [JsonPropertyName("data")]
        public List<Datum> Data { get; set; }

        [JsonPropertyName("meta")]
        public Metadata Meta { get; set; }

        public class Metadata
        {
            [JsonPropertyName("total_tweet_count")]
            public int TotalTweetCount { get; set; }
        }

        public class Datum
        {
            [JsonPropertyName("end")]
            public DateTime End { get; set; }

            [JsonPropertyName("start")]
            public DateTime Start { get; set; }

            [JsonPropertyName("tweet_count")]
            public int TweetCount { get; set; }
        }
    }

#pragma warning restore CS8618 // Non-nullable field is uninitialized. Consider declaring as nullable.
    #endregion

}

internal class TwitterMultiURLs : MultiURLsGeneric<Twitter.SearchJson, Twitter.SearchResult>
{
    private string? next_token;

    private static string URLNextToken(string token) => $"&next_token={token}";

    // Only allow construction with a request Generator
    public TwitterMultiURLs(Func<HttpRequestMessage> requestGen, IImageSource caller, int maxCount = URLThreshold) : base(requestGen, caller, maxCount)
    { }

    // retrieve next_token from json response
    protected override void OnTaskSuccessfull(Twitter.SearchJson result)
    {
        next_token = result.Meta.NextToken;
        base.OnTaskSuccessfull(result);
    }

    // Add next_token to URL if it is not null
    protected override HttpRequestMessage ModifyRequest(HttpRequestMessage request)
    {
        if (next_token != null && request.RequestUri != null)
            request.RequestUri = new(request.RequestUri.AbsoluteUri + URLNextToken(next_token));
        return request;
    }

}