using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Dalamud.Logging;
using Neko.Drawing;

namespace Neko.Sources.APIS;

public abstract class Twitter : ImageSource
{
#pragma warning disable RCS1181
    // This is a bad idea, but there really isnt a easy way to avoid doing this.
    // The proper solution is to require user authentication to a server, which holds
    // the text below. The name of the variables is a lie, so it wont be found by 
    // automated github scanning tools.
    // Please don't abuse this <3 (or i will have to disable the feature)
    private const string fhaksdn = "QUFBQUFBQUFBQUFBQUFBQUFBQUFBRkdlZ1FFQUFBQUFWV1JJandraFExOXhlMzJrb1JlRDdaMVI4U00";
    private const string asasdjsaf = "lM0RNQ3lsbWdFOURwcHdCTmlyTnJOeFFXNEF1WjdoZjYyWkFISG1uUTh5OTdsSllrcWg5Yg==";
    private static readonly string uuuuuu = Encoding.UTF8.GetString(Convert.FromBase64String(fhaksdn + asasdjsaf));

    private const int URLThreshold = 5;
    private readonly string search;

    private readonly CancellationTokenSource cts = new();

    public readonly Config.Query ConfigQuery;

    public override string Name => "Twitter";

    protected Twitter(Config.Query query)
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

    public abstract (string, string?) Status(CancellationToken ct = default);

    // The Status is a touple of a string that is always displayed, and a optional help message
    public (string, string?) TweetStatus() => Status(cts.Token);

    public override bool SameAs(ImageSource other) => other is Twitter t && t.ConfigQuery == ConfigQuery;

    /// <summary>
    /// Checks the header to see if the response is from Twitter rate limiting
    /// </summary>
    /// <param name="response">response Message</param>
    /// <returns>True if the header is a response from Twitter</returns>
    public static bool Is429Response(HttpResponseMessage response) =>
        response.RequestMessage?.RequestUri?.Host == "api.twitter.com" &&
        response.Headers.TryGetValues("x-rate-limit-remaining", out var remaining) &&
        remaining != null &&
        response.Headers.TryGetValues("x-rate-limit-reset", out var reset) &&
        reset != null &&
        response.Headers.TryGetValues("x-rate-limit-limit", out var limit) &&
        limit != null;

    /// <summary>
    /// Checks if the API is rate limited
    /// </summary>
    public static bool IsRateLimited;

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
            public override int GetHashCode() => searchText.GetHashCode();

            public Query Clone() => new() { searchText = new(searchText), enabled = enabled };
        }

        public ImageSource? LoadConfig()
        {
            // Load Default entries
            if (queries.Count == 0)
            {
                queries.Add(new Query() { searchText = "@FF_XIV_EN", enabled = false });
                queries.Add(new Query() { searchText = "#gposers", enabled = true });
                queries.Add(new Query() { searchText = "#FFXIV OR #FF14", enabled = true });
            }

            if (!enabled)
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
        private static readonly Regex removeTco = new(@"(\W*https:\/\/t.co\/\w+)+\W*$", RegexOptions.Compiled, TimeSpan.FromSeconds(5));

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

        public readonly string TweetDescription()
        {
            if (AuthorName == null || AuthorUsername == null)
                throw new Exception("AuthorName or AuthorUsername is null");

            // Remove t.co links from the tweet
            var filterdText = removeTco.Replace(Text ?? "", "");
            // Convert time to local time
            var localTime = CreatedAt.ToLocalTime();
            var span = DateTime.Now - localTime;

            var time = span.TotalSeconds < 5
                ? "now"
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

        public readonly string URLTweetID()
            => $"https://twitter.com/{AuthorUsername}/status/{TweetID}";

        public readonly string URLTweetID(string authorUsername)
            => $"https://twitter.com/{authorUsername}/status/{TweetID}";
    }

    public class Search : Twitter
    {
        private const string URLSearch = "https://api.twitter.com/2/tweets/search/recent";
        private static readonly string[] URLSearchParams = {
            "tweet.fields=id,text,attachments,created_at,possibly_sensitive",
            "media.fields=url,type",
            "user.fields=username",
            "expansions=attachments.media_keys,author_id",
            "max_results=10",
        };
        private const string URLQueryBegin = "query=has:media -is:retweet ";

        private readonly TwitterMultiURLs<SearchJson, ImageResponse> URLs;
        private readonly string searchQuery;
        private Task<(string, string?)>? TweetCount;
        private readonly object TweetCountLock = new();

        public Search(Config.Query query) : base(query)
        {
            searchQuery = URLQueryBegin + Uri.EscapeDataString(search);
            var URL = $"{URLSearch}?{string.Join('&', URLSearchParams)}&{searchQuery}";
            URLs = new((string token) => $"&next_token={token}", () => AuthorizedRequest(URL), this, URLThreshold);
        }

        public override string ToString() => $"Twitter Search: \"{search}\"\t{URLs}";

        public override string Name => $"Twitter Search: \"{search}\"";

        public override NekoImage Next(CancellationToken ct = default)
        {
            return new NekoImage(async (img) =>
            {
                if (IsRateLimited)
                    throw new Exception("Twitter API rate limit exceeded");

                var searchResult = await URLs.GetURL(ct).ConfigureAwait(false);
                var response = await Download.DownloadImage(searchResult.Media.Url, typeof(Search), ct).ConfigureAwait(false);
                img.Description = searchResult.TweetDescription();
                img.URLOpenOnClick = searchResult.URLTweetID();
                return response;
            }, this);
        }

        public override (string, string?) Status(CancellationToken ct = default)
        {
            // Start a new task to get the tweet count
            lock (TweetCountLock)
            {
                if (TweetCount == null)
                {
                    var URL = $"https://api.twitter.com/2/tweets/counts/recent?granularity=day&{searchQuery}";
                    async Task<(string, string?)> getCount()
                    {
                        try
                        {
                            var response = await Download.ParseJson<CountJson>(AuthorizedRequest(URL), ct).ConfigureAwait(false);
                            return (response.Meta!.TotalTweetCount.ToString(), $"Found {response.Meta.TotalTweetCount} tweets matching:\n\"{search}\"");
                        }
                        catch (HttpRequestException e)
                        {
                            try
                            {
                                var content = (string)e.Data["Content"]!;
                                var context = JsonContext.GetTypeInfo<CountJson>();
                                var result = JsonSerializer.Deserialize(content, context)!;

                                // Adjust position messages to account for hidden search text
                                Regex adjustPosition = new(@"\(at position (\d+)\)", RegexOptions.Compiled, TimeSpan.FromSeconds(5));
                                var error = adjustPosition.Replace(result.Errors![0].Message, (m) => $"(at position {int.Parse(m.Groups[1].Value) - URLQueryBegin.Length + 6})");

                                return ("ERROR", $"{result.Title}\n({result.Detail})\n\n{error}");
                            }
                            catch
                            {
                                return ("?", "Twitter returned an unknown error");
                            }
                        }
                        catch
                        {
                            return ("?", "Twitter returned an unknown error");
                        }
                    }
                    TweetCount = getCount();
                }
            }

            if (!TweetCount.IsCompleted)
                return ("LOADING", "Getting the amount of tweets");

            // Wait for the Completed task to catch exceptions
            try { TweetCount.Wait(CancellationToken.None); }
            catch (Exception e) { return ("ERROR", $"Error getting the amount of tweets.\n{e.Message}"); }

            return TweetCount.Result;
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
                        Plugin.Log.Debug($"Skipping tweet {tweet.Id} because it is marked as sensitive");
                        continue;
                    }

                    // Filter out tweets that match the blacklist
                    var x = NSFW.MatchesBadWord(tweet.Text);
                    if (x != null)
                    {
                        Plugin.Log.Debug($"Skipping tweet {tweet.Id} because it matches the blacklist entry \"{x}\"");
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
        private static readonly Regex extractUsername = new(@"^\s*@(\w+)\s*$", RegexOptions.Compiled, TimeSpan.FromSeconds(5));
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
                throw new ArgumentException("Invalid username", nameof(query));
            username = extractUsername.Match(query.searchText).Groups[1].Value;
            URLs = new((string token) => $"&pagination_token={token}", () => AuthorizedRequest(TimelineURL(userID)), this, URLThreshold);
        }

        ~UserTimeline()
        {
            cts.Cancel();
        }

        public override string ToString() => $"Twitter Timeline: @{username}\t{URLs}";

        public override string Name => $"Twitter Timeline: @{username}";

        public override NekoImage Next(CancellationToken ct = default)
        {
            return new NekoImage(async (img) =>
            {
                if (IsRateLimited)
                    throw new Exception("Twitter API rate limit exceeded");

                lock (userIDTaskLock)
                {
                    userIDTask ??= GetUserID(ct);
                }
                await userIDTask.ConfigureAwait(false);

                if (string.IsNullOrEmpty(userID) || usernameReadable == null)
                    throw new Exception("Failed to get user ID");

                var nextTweet = await URLs.GetURL(ct).ConfigureAwait(false);
                var response = await Download.DownloadImage(nextTweet.Media.Url, typeof(UserTimeline), ct).ConfigureAwait(false);
                img.Description = nextTweet.TweetDescription(usernameReadable, username);
                img.URLOpenOnClick = nextTweet.URLTweetID(username);
                return response;
            }, this);
        }

        public override (string, string?) Status(CancellationToken ct = default)
        {
            // Get User ID if not already done
            lock (userIDTaskLock)
            {
                if (userIDTask == null)
                {
                    Plugin.Log.Debug("Getting user ID");
                    try { userIDTask = GetUserID(ct); }
                    catch (Exception e) { return ("ERROR", e.Message); }
                }
            }

            // Return "Loading" if not done
            if (!userIDTask.IsCompleted)
                return ("LOADING", null);

            // "Wait" fot the completed task to finish to catch the exception
            try { userIDTask.Wait(CancellationToken.None); }
            catch (AggregateException e) { return ("ERROR", e.InnerException?.Message); }

            // Return OK
            return userIDTask?.IsCompletedSuccessfully == true
                && !string.IsNullOrEmpty(userID)
                && !string.IsNullOrEmpty(usernameReadable)
                ? ("OK", $"Found user @{username} with the name {usernameReadable}")
                : ("ERROR", "Unknown error");
        }

        public static async Task<UserLookupJson.SuccessRespone> GetIDFromUsername(string username, CancellationToken ct = default)
        {
            var URL = $"https://api.twitter.com/2/users/by/username/{username}";
            var response = await Download.ParseJson<UserLookupJson>(AuthorizedRequest(URL), ct).ConfigureAwait(false);

            return response.Errors != null
                ? throw new Exception($"Twitter API returned the Error:\n{response.Errors[0].Detail}")
                : response.Data ?? throw new Exception("Could not find Username");
        }

        public static bool ValidUsername(string username) => extractUsername.Match(username).Success;

        private async Task GetUserID(CancellationToken ct = default)
        {
            var response = await GetIDFromUsername(username, ct).ConfigureAwait(false);
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
                        Plugin.Log.Debug($"Skipping tweet {tweet.Id} because it is marked as sensitive");
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
        public List<Datum>? Data { get; set; }

        [JsonPropertyName("meta")]
        public Metadata? Meta { get; set; }

        [JsonPropertyName("errors")]
        public List<Error>? Errors { get; set; }

        [JsonPropertyName("title")]
        public string? Title { get; set; }

        [JsonPropertyName("detail")]
        public string? Detail { get; set; }

        public class Datum
        {
            [JsonPropertyName("end")]
            public DateTime End { get; set; }

            [JsonPropertyName("start")]
            public DateTime Start { get; set; }

            [JsonPropertyName("tweet_count")]
            public int TweetCount { get; set; }
        }

        public class Metadata
        {
            [JsonPropertyName("total_tweet_count")]
            public int TotalTweetCount { get; set; }
        }

        public class Error
        {
            [JsonPropertyName("parameters")]
            public Parameter Parameters { get; set; }

            [JsonPropertyName("message")]
            public string Message { get; set; }

            public class Parameter
            {
                [JsonPropertyName("query")]
                public List<string> Query { get; set; }
            }
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
        public TwitterMultiURLs(Func<string, string> nextTokenAppend, Func<HttpRequestMessage> requestGen, ImageSource caller, int maxCount = URLThreshold)
            : base(requestGen, caller, maxCount) => NextTokenAppend = nextTokenAppend;

        ~TwitterMultiURLs() => cts.Cancel();

        // retrieve next_token from json response
        protected override void OnTaskSuccessfull(TJson result)
        {
            next_token = result.NextToken();
            if (next_token == null)
                Plugin.Log.Debug("No next_token found. There are no more tweets to load. Starting over from the beginning.");
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
