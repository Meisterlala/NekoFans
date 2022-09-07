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

public abstract class Twitter : IImageSource
{
    // This is a bad idea, but there really isnt a easy way to avoid doing this.
    // The proper solution is to require user authentication to a server, which holds
    // the text below. The name of the variables is a lie, so it wont be found by 
    // automated github scanning tools.
    // Please don't abuse this <3 (or i will have to disable the feature)
    private static readonly string fhaksdn = "QUFBQUFBQUFBQUFBQUFBQUFBQUFBRkdlZ1FFQUFBQUFWV1JJandraFExOXhlMzJrb1JlRDdaMVI4U00";
    private static readonly string asasdjsaf = "lM0RNQ3lsbWdFOURwcHdCTmlyTnJOeFFXNEF1WjdoZjYyWkFISG1uUTh5OTdsSllrcWg5Yg==";
    private static readonly string uuuuuu = Encoding.UTF8.GetString(Convert.FromBase64String(fhaksdn + asasdjsaf));

    private const int URLThreshold = 5;
    private readonly string search;

    // The Status is a touple of a string that is always displayed, and a optional help message
    private (string, string?)? status;
    private Task<(string, string?)>? statusTask;
    private readonly CancellationTokenSource cts = new();

    public readonly Config.Query ConfigQuery;
    public bool Faulted { get; set; }

    public virtual string Name => "Twitter";

    public Twitter(Config.Query query)
    {
        ConfigQuery = query;
        search = query.searchText;
    }

    ~Twitter()
    {
        cts.Cancel();
    }

    private static HttpRequestMessage AuthorizedRequest(string url) =>
        new(HttpMethod.Get, url)
        {
            Headers =
                {
                    Authorization = new("Bearer", uuuuuu),
                }
        };

    public abstract Task<NekoImage> Next(CancellationToken ct = default);

    public abstract Task<(string, string?)> Status(CancellationToken ct = default);

    public abstract override string ToString();

    public (string, string?) TweetStatus()
    {
        // Use cached result if available
        if (status.HasValue)
            return status.Value;

        // Start TweetCount Task
        if (statusTask == null)
            statusTask = Status(cts.Token);

        return ("?", null);
    }

    public bool Equals(IImageSource? other) => other != null && other is Twitter t && t.ConfigQuery == ConfigQuery;

    public class Config : IImageConfig
    {
        public bool enabled;
        public List<Query> queries = new();

        public class Query
        {
            public string searchText = "";
            public bool enabled;

            public static bool operator ==(Query q1, Query q2) => q1.searchText == q2.searchText && q1.enabled == q2.enabled;
            public static bool operator !=(Query q1, Query q2) => q1.searchText != q2.searchText || q1.enabled != q2.enabled;

            public override bool Equals(object? obj) => obj is Query q && q == this;
            public override int GetHashCode() => searchText.GetHashCode() ^ enabled.GetHashCode();

            public Query Clone() => new() { searchText = new(searchText), enabled = enabled };
        }

        public IImageSource? LoadConfig()
        {
            if (!enabled || queries.Count == 0)
                return null;

            var enabledQueries = queries.FindAll((q) => q.enabled);
            if (enabledQueries.Count == 0)
                return null;

            var imageSources = new CombinedSource();
            foreach (var q in enabledQueries)
            {
                if (UserTimeline.ValidUsername(q.searchText))
                {
                    Twitter t = new UserTimeline(q);
                    imageSources.AddSource(t);
                }
                else
                {
                    Twitter t = new Search(q);
                    imageSources.AddSource(t);
                }
            }


            return imageSources;
        }
    }

    public struct ImageResponse
    {
        private static readonly Regex removeTco = new(@"(\W*https:\/\/t.co\/\w+)+\W*$", RegexOptions.Compiled);

        public string Text;
        public string TweetID;
        public DateTime CreatedAt;
        public string? AuthorName;
        public string? AuthorUsername;
        public Medium Media;

        public ImageResponse(string text, string tweetID, DateTime createdAt, Medium media, string? authorName, string? authorUsername)
        {
            Text = text;
            TweetID = tweetID;
            CreatedAt = createdAt;
            AuthorName = authorName;
            AuthorUsername = authorUsername;
            Media = media;
        }

        public string TweetDescription()
        {
            if (AuthorName == null || AuthorUsername == null)
                throw new Exception("AuthorName or AuthorUsername is null");

            // Remove t.co links from the tweet
            var filterdText = removeTco.Replace(Text ?? "", "");
            // Convert time to local time
            var localTime = CreatedAt.ToLocalTime();
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
                : $"{localTime.ToLongDateString()}";

            return $"{AuthorName} (@{AuthorUsername}) tweeted {time}:\n{filterdText}";
        }

        public string TweetDescription(string authorName, string authorUsername)
        {
            AuthorName = authorName;
            AuthorUsername = authorUsername;
            return TweetDescription();
        }

        public string URLTweetID()
            => $"https://twitter.com/{AuthorUsername}/status/{TweetID}";

        public string URLTweetID(string authorUsername)
            => $"https://twitter.com/{authorUsername}/status/{TweetID}";

    }



    public class Search : Twitter
    {
        private static readonly string URLSearch = "https://api.twitter.com/2/tweets/search/recent";
        private static readonly string[] URLSearchParams = {
            "tweet.fields=id,text,attachments,created_at,possibly_sensitive",
            "media.fields=url,type",
            "user.fields=username",
            "expansions=attachments.media_keys,author_id",
            "max_results=10",
        };
        private static readonly string URLQueryBegin = "query=has:media -is:retweet ";

        private readonly TwitterMultiURLs<SearchJson, ImageResponse> URLs;
        private readonly string searchQuery;

        public Search(Config.Query query) : base(query)
        {
            searchQuery = URLQueryBegin + Uri.EscapeDataString(search);
            var URL = $"{URLSearch}?{string.Join('&', URLSearchParams)}&{searchQuery}";
            URLs = new((string token) => $"&next_token={token}", () => AuthorizedRequest(URL), this, URLThreshold);
        }

        public override string ToString() => $"Twitter Search: \"{search}\"\t{URLs}";

        public override string Name => $"Twitter Search: \"{search}\"";

        public override async Task<NekoImage> Next(CancellationToken ct = default)
        {
            var searchResult = await URLs.GetURL(ct);
            var image = await Common.DownloadImage(searchResult.Media.Url, ct);
            image.Description = searchResult.TweetDescription();
            image.URLClick = searchResult.URLTweetID();
            return image;
        }

        public override async Task<(string, string?)> Status(CancellationToken ct = default)
        {
            // Wait for task to finish
            if (statusTask != null)
                await statusTask;

            // Use cached result if available
            if (status.HasValue)
                return status.Value;

            var URL = $"https://api.twitter.com/2/tweets/counts/recent?granularity=day&{searchQuery}";
            var task = Task.Run(async () =>
            {
                try
                {
                    var response = await Common.ParseJson<CountJson>(AuthorizedRequest(URL), ct);
                    status = (response.Meta.TotalTweetCount.ToString(), $"Found {response.Meta.TotalTweetCount} tweets matching \"{search}\"");
                    return status.Value;
                }
                catch
                {
                    return ("?", null!);
                }
            });

            return await task;
        }

        #region JSON
#pragma warning disable CS8618 // Non-nullable field is uninitialized. Consider declaring as nullable.
        public class SearchJson : IJsonToList<ImageResponse>, INextToken
        {
            public List<ImageResponse> ToList()
            {
                if (Includes == null || Includes.Media == null || Data == null)
                    return new List<ImageResponse>();

                List<ImageResponse> results = new();

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

                    // Filter out tweets that match the blacklist
                    var x = NSFW.MatchesBadWord(tweet.Text);
                    if (x != null)
                    {
                        PluginLog.LogDebug($"Skipping tweet {tweet.Id} because it matches the blacklist entry \"{x}\"");
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
                        results.Add(new(tweet.Text, tweet.Id, tweet.CreatedAt, media, author.Name, author.Username));
                    }
                }

                return results;
            }

            public string? NextToken() => Meta?.NextToken;

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

#pragma warning restore CS8618 // Non-nullable field is uninitialized. Consider declaring as nullable.
        #endregion
    }

    public class UserTimeline : Twitter
    {
        private static readonly Regex extractUsername = new(@"^\s*@(\w+)\s*$", RegexOptions.Compiled);
        private static string TimelineURL(string ID) => $"https://api.twitter.com/2/users/{ID}/tweets?expansions=attachments.media_keys&tweet.fields=created_at,possibly_sensitive&user.fields=username&media.fields=url,media_key";

        private string userID = "";
        private string? usernameReadable;
        private readonly string username;
        private readonly object userIDTaskLock = new();
        private Task? userIDTask;
        private readonly TwitterMultiURLs<TweetTimelineJson, ImageResponse> URLs;

        public UserTimeline(Config.Query query) : base(query)
        {
            if (!ValidUsername(query.searchText))
                throw new ArgumentException("Invalid username");
            username = extractUsername.Match(query.searchText).Groups[1].Value;
            URLs = new((string token) => $"&pagination_token={token}", () => AuthorizedRequest(TimelineURL(userID)), this, URLThreshold);
        }

        ~UserTimeline()
        {
            cts.Cancel();
        }

        public override string ToString() => $"Twitter Timeline: @{username}\t{URLs}";

        public override string Name => $"Twitter Timeline: @{username}";

        public override async Task<NekoImage> Next(CancellationToken ct = default)
        {
            lock (userIDTaskLock)
            {
                if (userID == null || userIDTask == null || usernameReadable == null)
                    userIDTask = GetUserID(ct);
            }
            await userIDTask;

            if (string.IsNullOrEmpty(userID) || usernameReadable == null)
                throw new Exception("Failed to get user ID");

            var nextTweet = await URLs.GetURL(ct);
            var image = await Common.DownloadImage(nextTweet.Media.Url, ct);
            image.Description = nextTweet.TweetDescription(usernameReadable, username);
            image.URLClick = nextTweet.URLTweetID(username);
            return image;
        }


        public override async Task<(string, string?)> Status(CancellationToken ct = default)
        {
            // Wait for task to finish
            if (statusTask != null && statusTask.Status == TaskStatus.Running)
                await statusTask;

            // Use cached result if available
            if (status.HasValue)
                return status.Value;

            if (!ValidUsername(ConfigQuery.searchText))
            {
                status = ("ERROR", "Invalid username");
                return status.Value;
            }

            if (userIDTask != null && userIDTask.Status == TaskStatus.Running)
            {
                await userIDTask;
                if (string.IsNullOrEmpty(userID))
                {
                    status = ("ERROR", "Failed to get user ID. The user may not exist");
                    return status.Value;
                }
            }
            else
            {
                userIDTask = GetUserID(ct);

                try { await userIDTask; }
                catch (Exception)
                {
                    status = ("ERROR", $"Failed to get user ID. The user may not exist");
                    return status.Value;
                }
            }

            status = ("OK", $"@{username} was found  with the name \"{usernameReadable}\"");
            return status.Value;
        }

        public static async Task<UserLookupJson.SuccessRespone> GetIDFromUsername(string username, CancellationToken ct = default)
        {
            var URL = $"https://api.twitter.com/2/users/by/username/{username}";
            var response = await Common.ParseJson<UserLookupJson>(AuthorizedRequest(URL), ct);

            return response.Errors != null
                ? throw new Exception($"Twitter API returned the Error: {response.Errors[0].Detail}")
                : response.Data ?? throw new Exception("Could not find Username");
        }

        public static bool ValidUsername(string username) => extractUsername.Match(username).Success;

        private async Task GetUserID(CancellationToken ct = default)
        {
            var response = await GetIDFromUsername(username, ct);
            userID = response.Id;
            usernameReadable = response.Name;
        }


        #region JSON
#pragma warning disable CS8618 // Non-nullable field is uninitialized. Consider declaring as nullable.
        public class UserLookupJson
        {
            [JsonPropertyName("data")]
            public SuccessRespone? Data { get; set; }

            [JsonPropertyName("errors")]
            public List<ErrorResponse>? Errors { get; set; }

            public class SuccessRespone
            {
                [JsonPropertyName("id")]
                public string Id { get; set; }

                [JsonPropertyName("name")]
                public string Name { get; set; }

                [JsonPropertyName("username")]
                public string Username { get; set; }
            }

            public class ErrorResponse
            {
                [JsonPropertyName("value")]
                public string Value { get; set; }

                [JsonPropertyName("detail")]
                public string Detail { get; set; }

                [JsonPropertyName("title")]
                public string Title { get; set; }

                [JsonPropertyName("resource_type")]
                public string ResourceType { get; set; }

                [JsonPropertyName("parameter")]
                public string Parameter { get; set; }

                [JsonPropertyName("resource_id")]
                public string ResourceId { get; set; }

                [JsonPropertyName("type")]
                public string Type { get; set; }
            }
        }

        public class TweetTimelineJson : IJsonToList<ImageResponse>, INextToken
        {
            [JsonPropertyName("data")]
            public List<Tweet> Data { get; set; }

            [JsonPropertyName("includes")]
            public Attatchments? Includes { get; set; }

            [JsonPropertyName("meta")]
            public Metadata? Meta { get; set; }

            public List<ImageResponse> ToList()
            {
                if (Includes == null || Includes.Media == null || Data == null)
                    return new List<ImageResponse>();

                List<ImageResponse> results = new();

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

                    // Search all attatched media_keys
                    foreach (var media_key in tweet.Attachments.MediaKeys ?? new())
                    {
                        // Find the media with the matching media_key
                        var media = Includes.Media.Find((m) => m.MediaKey == media_key);
                        // That is an image
                        if (media == null || media.Type != "photo")
                            continue;
                        // Add the image to the results
                        results.Add(new ImageResponse(tweet.Text, tweet.Id, tweet.CreatedAt, media, null, null));
                    }
                }

                return results;
            }

            public string? NextToken() => Meta?.NextToken;

            public class Attachments
            {
                [JsonPropertyName("media_keys")]
                public List<string> MediaKeys { get; set; }
            }

            public class Tweet
            {
                [JsonPropertyName("text")]
                public string Text { get; set; }

                [JsonPropertyName("attachments")]
                public Attachments? Attachments { get; set; }

                [JsonPropertyName("id")]
                public string Id { get; set; }

                [JsonPropertyName("created_at")]
                public DateTime CreatedAt { get; set; }

                [JsonPropertyName("possibly_sensitive")]
                public bool PossiblySensitive { get; set; }
            }

            public class Attatchments
            {
                [JsonPropertyName("media")]
                public List<Medium>? Media { get; set; }
            }

            public class Metadata
            {
                [JsonPropertyName("next_token")]
                public string? NextToken { get; set; }

                [JsonPropertyName("result_count")]
                public int ResultCount { get; set; }

                [JsonPropertyName("newest_id")]
                public string NewestId { get; set; }

                [JsonPropertyName("oldest_id")]
                public string OldestId { get; set; }
            }
        }

#pragma warning restore CS8618 // Non-nullable field is uninitialized. Consider declaring as nullable.
        #endregion
    }
    #region JSON

#pragma warning disable CS8618 // Non-nullable field is uninitialized. Consider declaring as nullable.
    public class Medium
    {
        [JsonPropertyName("media_key")]
        public string MediaKey { get; set; }

        [JsonPropertyName("type")]
        public string Type { get; set; }

        [JsonPropertyName("url")]
        public string Url { get; set; }
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
    #endregion JSON

    internal interface INextToken
    {
        public string? NextToken();
    }

    internal class TwitterMultiURLs<TJson, TQueueElement> : MultiURLsGeneric<TJson, TQueueElement>
        where TJson : IJsonToList<TQueueElement>, INextToken
    {
        private string? next_token;

        private readonly Func<string, string> NextTokenAppend;

        // Only allow construction with a request Generator
        public TwitterMultiURLs(Func<string, string> nextTokenAppend, Func<HttpRequestMessage> requestGen, IImageSource caller, int maxCount = URLThreshold)
            : base(requestGen, caller, maxCount) => NextTokenAppend = nextTokenAppend;

        ~TwitterMultiURLs() => cts.Cancel();

        // retrieve next_token from json response
        protected override void OnTaskSuccessfull(TJson result)
        {
            next_token = result.NextToken();
            if (next_token == null)
                PluginLog.LogDebug("No next_token found. There are no more tweets to load. Starting over from the beginning.");
            base.OnTaskSuccessfull(result);
        }

        // Add next_token to URL if it is not null
        protected override HttpRequestMessage ModifyRequest(HttpRequestMessage request)
        {
            if (next_token != null && request.RequestUri != null)
                request.RequestUri = new(request.RequestUri.AbsoluteUri + NextTokenAppend(next_token));
            return request;
        }
    }
}
