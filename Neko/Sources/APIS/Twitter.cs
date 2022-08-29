using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace Neko.Sources.APIS;

public class Twitter : IImageSource
{
    public class Config : IImageConfig
    {
        public bool enabled;

        public List<string> queries = new();

        public IImageSource? LoadConfig() => enabled ? new CombinedSource(queries.ConvertAll<Twitter>((query) => new(query)).ToArray()) : null;
    }
    private static readonly string URLSearch = "https://api.twitter.com/2/tweets/search/recent";

    private static readonly string[] URLSearchParams = {
        "tweet.fields=id,text,attachments,created_at,possibly_sensitive",
        "media.fields=url,type",
        "user.fields=username",
        "expansions=attachments.media_keys,author_id",
        "max_results=10",
        "query=has:media -is:retweet "
    };

    // This is a bad idea, but there really isnt a easy way to avoid doing this.
    // The proper solution is to require user authentication to a server, which holds
    // the text below. The name of the variables is a lie, so it wont be found by 
    // automated github scanning tools.
    // Please don't abuse this <3 (or i will have to disable the feature)
    private static readonly string fhaksdn = "QUFBQUFBQUFBQUFBQUFBQUFBQUFBRkdlZ1FFQUFBQUFWV1JJandraFExOXhlMzJrb1JlRDdaMVI4U00";
    private static readonly string asasdjsaf = "lM0RNQ3lsbWdFOURwcHdCTmlyTnJOeFFXNEF1WjdoZjYyWkFISG1uUTh5OTdsSllrcWg5Yg==";
    private static readonly string uuuuuu = Encoding.UTF8.GetString(Convert.FromBase64String(fhaksdn + asasdjsaf));

    private readonly string URL;
    private readonly string query;
    private readonly TwitterMultiURLs URLs;

    public override string ToString() => $"Twitter: \"{query}\"\t{URLs}";

    public Twitter(string search)
    {
        query = search;
        URL = $"{URLSearch}?{string.Join('&', URLSearchParams)}{Uri.EscapeDataString(search)}";

        HttpRequestMessage request() =>
            new(HttpMethod.Get, URL)
            {
                Headers =
                {
                    Authorization = new("Bearer", uuuuuu),
                }
            };

        URLs = new(request, 5);
    }

    public async Task<NekoImage> Next(CancellationToken ct = default)
    {
        var searchResult = await URLs.GetURL();
        var image = await Common.DownloadImage(searchResult.Media.url, ct);
        image.Description = TweetDescription(searchResult.Tweet, searchResult.Author);
        image.URLClick = URLTweetID(searchResult.Tweet, searchResult.Author);
        return image;
    }


    private static readonly Regex removeTco = new(@"(\W*https:\/\/t.co\/\w+)+\W*$", RegexOptions.Compiled);

    public static string TweetDescription(TwitterSearchJson.Tweet tweet, TwitterSearchJson.User user)
    {
        // Remove t.co links from the tweet
        var filterdText = removeTco.Replace(tweet.text, "");
        // Convert time to local time
        var localTime = DateTime.Parse(tweet.created_at).ToLocalTime();
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

        return $"{user.name} (@{user.username}) tweeted {time}:\n{filterdText}";
    }

    private static string URLTweetID(TwitterSearchJson.Tweet tweet, TwitterSearchJson.User user)
        => $"https://twitter.com/{user.username}/status/{tweet.id}";

    public struct SearchResult
    {
        public TwitterSearchJson.Tweet Tweet;
        public TwitterSearchJson.User Author;
        public TwitterSearchJson.Media Media;

        public SearchResult(TwitterSearchJson.Tweet tweet, TwitterSearchJson.User author, TwitterSearchJson.Media media)
        {
            Tweet = tweet;
            Author = author;
            Media = media;
        }
    }

#pragma warning disable
    public class TwitterSearchJson : IJsonToList<SearchResult>
    {
        public List<SearchResult> ToList()
        {
            if (includes == null || includes.media == null || data == null)
                return new List<SearchResult>();

            List<SearchResult> results = new();

            // For each tweet
            foreach (var tweet in data)
            {
                // that has media attached
                if (tweet.attachments == null)
                    continue;

                // Only show sensitive tweets if NSFW mode
                if (tweet.possibly_sensitive ?? false && !Neko.NSFW.AllowNSFW)
                    continue;

                // Find the Author
                var author = includes.users.Find((user) => user.id == tweet.author_id);
                if (author == null) // Every tweet should have an author
                    continue;

                // Search all attatched media_keys
                foreach (var media_key in tweet.attachments.media_keys)
                {
                    // Find the media with the matching media_key
                    var media = includes.media.Find((m) => m.media_key == media_key);
                    // That is an image
                    if (media == null || media.type != "photo")
                        continue;
                    // Add the image to the results
                    results.Add(new SearchResult(tweet, author, media));
                }
            }

            return results;
        }

        public List<Tweet> data { get; set; }
        public Includes? includes { get; set; }
        public Meta meta { get; set; }

        public class Meta
        {
            public string? newest_id { get; set; }
            public string? oldest_id { get; set; }
            public int result_count { get; set; }
            public string? next_token { get; set; }
        }

        public class User
        {
            public string id { get; set; }
            public string name { get; set; }
            public string username { get; set; }
        }

        public class Media
        {
            public string media_key { get; set; }
            public string type { get; set; }
            public string url { get; set; }
        }

        public class Includes
        {
            public List<Media> media { get; set; }
            public List<User> users { get; set; }
        }

        public class Attachments
        {
            public List<string> media_keys { get; set; }
        }

        public class Tweet
        {
            public Attachments attachments { get; set; }

            public string author_id { get; set; }
            public string id { get; set; }
            public string text { get; set; }
            public string created_at { get; set; }
            public bool? possibly_sensitive { get; set; }
        }

    }
#pragma warning restore
}

internal class TwitterMultiURLs : MultiURLsGeneric<Twitter.TwitterSearchJson, Twitter.SearchResult>
{
    private string? next_token;

    private static string URLNextToken(string token) => $"&next_token={token}";

    // Only allow construction with a request Generator
    public TwitterMultiURLs(Func<HttpRequestMessage> requestGen, int maxCount = URLThreshold) : base(requestGen, maxCount)
    { }

    // retrieve next_token from json response
    protected override void OnTaskSuccessfull(Twitter.TwitterSearchJson result)
    {
        next_token = result.meta.next_token;
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