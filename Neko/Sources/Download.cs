using System;
using System.Collections.Concurrent;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Neko.Sources;

public static class Download
{
    private static readonly TimeSpan ImageRateLimitTimeout = TimeSpan.FromSeconds(90);

    public sealed class SkippedImageException(string message) : Exception(message) { }

    public struct Response
    {
        public byte[] Data;
        public string Url;
    }

    private static readonly JsonSerializerOptions JsonSerializerOptions = new() { WriteIndented = true };
    private static readonly ConcurrentDictionary<string, SourceRateLimit> SourceRateLimits = new();

    /// <summary>
    /// Downloads a file from the internet and returns the data in from of a <see cref="Response"/>.
    /// </summary>
    public static async Task<Response> DownloadImage(HttpRequestMessage request, Type? called = default, CancellationToken ct = default)
        => await DownloadImage(request, called, null, ct).ConfigureAwait(false);

    private static async Task<Response> DownloadImage(HttpRequestMessage request, Type? called, DateTimeOffset? firstRateLimit, CancellationToken ct)
    {
        DebugHelper.RandomThrow(DebugHelper.ThrowChance.DownloadImage);
        await DebugHelper.RandomDelay(DebugHelper.Delay.DownloadImage, ct).ConfigureAwait(false);

        byte[]? bytes;
        try
        {
            var rateLimitKey = RateLimitKey(request, called);
            await WaitForSourceRateLimit(rateLimitKey, ct).ConfigureAwait(false);

            var response = await Plugin.HttpClient.SendAsync(request, ct).ConfigureAwait(false);
            if (response.RequestMessage != null)
                DebugHelper.LogNetwork(() => "Sent request to download image:\n" + response.RequestMessage?.ToString());
            if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
            {
                if (!HasRetryAfter(response))
                {
                    RegisterSourceRateLimit(rateLimitKey, RetryAfterMilliseconds(response), request.RequestUri?.ToString() ?? "unknown", RateLimitAction.CoolDownAndSkip);
                    throw new SkippedImageException($"Skipped rate-limited image without Retry-After: {request.RequestUri}");
                }

                firstRateLimit ??= DateTimeOffset.UtcNow;
                var elapsed = DateTimeOffset.UtcNow - firstRateLimit.Value;
                if (elapsed >= ImageRateLimitTimeout)
                    throw new HttpRequestException($"Could not download image from: {request.RequestUri} ({response.StatusCode})", null, response.StatusCode);

                await WaitForSourceRateLimit(rateLimitKey, RetryAfterMilliseconds(response), request.RequestUri?.ToString() ?? "unknown", ImageRateLimitTimeout - elapsed, ct).ConfigureAwait(false);

                return await DownloadImage(Helper.RequestClone(request), called, firstRateLimit, ct).ConfigureAwait(false);
            }
            if (response.StatusCode == System.Net.HttpStatusCode.Forbidden)
            {
                LogForbidden(response, called);
                throw new HttpRequestException($"Could not download image from: {request.RequestUri} ({response.StatusCode})", null, response.StatusCode);
            }
            response.EnsureSuccessStatusCode();
            MarkRateLimitSucceeded(rateLimitKey);
            bytes = await response.Content.ReadAsByteArrayAsync(ct).ConfigureAwait(false);
        }
        catch (SkippedImageException) { throw; }
        catch (Exception ex)
        {
            throw new Exception("Could not download image from: " + request.RequestUri, ex);
        }

        // Log Download if its not the Telemetry API
        if (!request.RequestUri?.ToString().StartsWith(Plugin.ControlServer) ?? false)
            Plugin.Log.Info($"Downloaded {Helper.SizeSuffix(bytes.LongLength, 1)} from {request.RequestUri}");

        if (called != null)
            Telemetry.RegisterDownload(called);

        ct.ThrowIfCancellationRequested();

        return new Response { Data = bytes, Url = request.RequestUri?.ToString() ?? "" };
    }

    public static async Task<Response> DownloadImage(string url, Type? called = default, CancellationToken ct = default)
    {
        HttpRequestMessage request = new(HttpMethod.Get, url)
        {
            Headers =
                {
                    Accept =
                    {
                        new("image/jpeg"),
                        new("image/png"),
                    }
                }
        };
        return await DownloadImage(request, called, ct).ConfigureAwait(false);
    }

    /// <summary>
    /// Downloads and Parses a .json file
    /// </summary>
    public static async Task<T> ParseJson<T>(HttpRequestMessage request, CancellationToken ct = default)
        => await ParseJson<T>(request, null, ct).ConfigureAwait(false);

    public static async Task<T> ParseJson<T>(HttpRequestMessage request, Type? called, CancellationToken ct = default)
    {
        DebugHelper.RandomThrow(DebugHelper.ThrowChance.ParseJson);
        await DebugHelper.RandomDelay(DebugHelper.Delay.ParseJson, ct).ConfigureAwait(false);

        // Download .json file to stream
        System.IO.Stream? stream;
        HttpResponseMessage response;
        var rateLimitKey = RateLimitKey(request, called);
        try
        {
            await WaitForSourceRateLimit(rateLimitKey, ct).ConfigureAwait(false);

            response = await Plugin.HttpClient.SendAsync(request, ct).ConfigureAwait(false);
            if (response.RequestMessage != null)
                DebugHelper.LogNetwork(() => "Sending request to get json:\n" + request.ToString());
            stream = await response.Content.ReadAsStreamAsync(ct).ConfigureAwait(false);
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex)
        {
            throw new Exception("Error occured when trying to donwload: " + request.RequestUri, ex);
        }

        // Ensure Success
        try
        {
            response.EnsureSuccessStatusCode();
            MarkRateLimitSucceeded(rateLimitKey);
        }
        catch (HttpRequestException ex)
        {
            if (response.StatusCode == System.Net.HttpStatusCode.Forbidden)
            {
                LogForbidden(response, called);
                var forbiddenException = new HttpRequestException($"Could not Download .json from: {request.RequestUri} ({response.StatusCode})", ex, response.StatusCode);
                forbiddenException.Data.Add("StatusCode", response.StatusCode);
                forbiddenException.Data.Add("Content", await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false));
                throw forbiddenException;
            }

            // Handle 429 (Too Many Requests) by waiting and retrying
            if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
            {
                DebugHelper.LogNetwork(() => "API retuned 429 (Too Many Requests)\n" + response.Headers.ToString());

                // Twitter API limit reached
                if (APIS.Twitter.Is429Response(response))
                {
                    APIS.Twitter.IsRateLimited = true;
                    throw new Exception("Twitter API limit reached. Wait a few days until the limit gets reset", ex);
                }

                await WaitForSourceRateLimit(rateLimitKey, RetryAfterMilliseconds(response), request.RequestUri?.ToString() ?? "unknown", null, ct).ConfigureAwait(false);

                // Clone request, because you cant send the same one twice
                var newRequest = Helper.RequestClone(request);
                return await ParseJson<T>(newRequest, called, ct).ConfigureAwait(false);
            }

            DebugHelper.LogNetwork(() => $"Error Downloading Json from {request.RequestUri}:\n{JsonSerializer.Serialize(response.Content.ReadAsStringAsync(ct).Result, JsonSerializerOptions)}");
            var exception = new HttpRequestException($"Could not Download .json from: {request.RequestUri} ({response.StatusCode})", ex);
            exception.Data.Add("StatusCode", response.StatusCode);
            exception.Data.Add("Content", await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false));
            throw exception;
        }

        // Stop Early if requested
        ct.ThrowIfCancellationRequested();

        // Parse Json
        T? result;
        try
        {
            var context = JsonContext.GetTypeInfo<T>();
            result = await JsonSerializer.DeserializeAsync(stream, context, ct).ConfigureAwait(false) ?? throw new Exception("Did not get a response from: " + request.RequestUri);
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex)
        {
            Plugin.Log.Debug(await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false));
            throw new Exception("Could not Parse .json File from: " + request.RequestUri, ex);
        }

        DebugHelper.LogNetwork(() => $"Response from {request.RequestUri} trying to get a json:\n{JsonSerializer.Serialize(result, JsonSerializerOptions)}");

        return result;
    }

    public static async Task<T> ParseJson<T>(string url, CancellationToken ct = default)
        => await ParseJson<T>(url, null, ct).ConfigureAwait(false);

    public static async Task<T> ParseJson<T>(string url, Type? called, CancellationToken ct = default)
    {
        HttpRequestMessage request = new(HttpMethod.Get, url)
        {
            Headers =
                {
                    Accept =
                    {
                        new("application/json"),
                    }
                }
        };
        return await ParseJson<T>(request, called, ct).ConfigureAwait(false);
    }

    private static async Task WaitForSourceRateLimit(string key, CancellationToken ct)
    {
        if (!SourceRateLimits.TryGetValue(key, out var rateLimit))
            return;

        await rateLimit.Wait(ct).ConfigureAwait(false);
    }

    private static async Task WaitForSourceRateLimit(string key, int retryAfterMilliseconds, CancellationToken ct)
        => await WaitForSourceRateLimit(key, retryAfterMilliseconds, null, null, ct).ConfigureAwait(false);

    private static async Task WaitForSourceRateLimit(string key, int retryAfterMilliseconds, string? url, TimeSpan? maxWait, CancellationToken ct)
    {
        var rateLimit = SourceRateLimits.GetOrAdd(key, _ => new SourceRateLimit(key));
        await rateLimit.Wait(retryAfterMilliseconds, url, maxWait, RateLimitAction.WaitAndRetry, ct).ConfigureAwait(false);
    }

    private static void RegisterSourceRateLimit(string key, int retryAfterMilliseconds, string? url, RateLimitAction action)
    {
        var rateLimit = SourceRateLimits.GetOrAdd(key, _ => new SourceRateLimit(key));
        rateLimit.Register(retryAfterMilliseconds, url, null, action);
    }

    internal static bool IsRateLimited(Type source)
    {
        var keyPrefix = SourceRateLimitKeyPrefix(source) + " ";
        return SourceRateLimits.Any(entry => entry.Key.StartsWith(keyPrefix, StringComparison.Ordinal)
            && entry.Value.IsActive);
    }

    private static void MarkSourceRateLimitSucceeded(string key)
    {
        if (SourceRateLimits.TryGetValue(key, out var rateLimit))
            rateLimit.Succeeded();
    }

    private static string RateLimitKey(HttpRequestMessage request, Type? called = default)
    {
        var host = request.RequestUri?.Host ?? "unknown";
        return called == null ? host : $"{SourceRateLimitKeyPrefix(called)} {host}";
    }

    private static string SourceRateLimitKeyPrefix(Type source) => source.FullName ?? source.Name;

    private static void MarkRateLimitSucceeded(string key)
    {
        MarkSourceRateLimitSucceeded(key);
    }

    private static void LogForbidden(HttpResponseMessage response, Type? called = default)
    {
        var source = called?.FullName ?? called?.Name ?? response.RequestMessage?.RequestUri?.Host ?? "unknown source";
        Plugin.Log.Warning($"{source} returned 403 (Forbidden) for {response.RequestMessage?.RequestUri}. Marking request as failed.");
    }

    private enum RateLimitAction
    {
        WaitAndRetry,
        CoolDownAndSkip,
    }

    private sealed class SourceRateLimit(string key)
    {
        private readonly SemaphoreSlim semaphore = new(1, 1);
        private readonly string key = key;
        private int consecutiveFailures;
        private DateTimeOffset waitUntil = DateTimeOffset.MinValue;

        public bool IsActive => DateTimeOffset.UtcNow < waitUntil;

        public async Task Wait(CancellationToken ct)
        {
            var delay = waitUntil - DateTimeOffset.UtcNow;
            if (delay > TimeSpan.Zero)
                await Task.Delay(delay, ct).ConfigureAwait(false);
        }

        public async Task Wait(int retryAfterMilliseconds, string? url, TimeSpan? maxWait, RateLimitAction action, CancellationToken ct)
        {
            await semaphore.WaitAsync(ct).ConfigureAwait(false);
            try
            {
                RegisterLocked(retryAfterMilliseconds, url, maxWait, action);
            }
            finally
            {
                semaphore.Release();
            }

            await Wait(ct).ConfigureAwait(false);
        }

        public void Register(int retryAfterMilliseconds, string? url, TimeSpan? maxWait, RateLimitAction action)
        {
            semaphore.Wait();
            try
            {
                RegisterLocked(retryAfterMilliseconds, url, maxWait, action);
            }
            finally
            {
                semaphore.Release();
            }
        }

        public void Succeeded()
        {
            consecutiveFailures = 0;
        }

        private int BackoffMilliseconds(int retryAfterMilliseconds, TimeSpan? maxWait)
        {
            var backoffSeconds = Math.Min(60, 1 << Math.Min(consecutiveFailures, 5));
            consecutiveFailures++;
            var delay = Math.Clamp(Math.Max(retryAfterMilliseconds, backoffSeconds * 1000), 1000, 60000);
            if (maxWait == null)
                return delay;

            return Math.Clamp(Math.Min(delay, (int)maxWait.Value.TotalMilliseconds), 1000, 60000);
        }

        private void RegisterLocked(int retryAfterMilliseconds, string? url, TimeSpan? maxWait, RateLimitAction action)
        {
            var now = DateTimeOffset.UtcNow;
            var retryDelay = BackoffMilliseconds(retryAfterMilliseconds, maxWait);
            var existingDelay = waitUntil - now;
            if (existingDelay.TotalMilliseconds < retryDelay - 250)
            {
                waitUntil = now.AddMilliseconds(retryDelay);
                var actionText = action == RateLimitAction.CoolDownAndSkip
                    ? $"Cooling down source for {retryDelay / 1000.0} seconds and skipping current image."
                    : $"Waiting {retryDelay / 1000.0} seconds before trying again.";
                Plugin.Log.Information($"{key} returned 429 (Too Many Requests) for {url ?? "unknown URL"}. {actionText}");
            }
        }
    }

    private static int RetryAfterMilliseconds(HttpResponseMessage response)
    {
        if (response.Headers.TryGetValues("Retry-After", out var values) && values.Any())
        {
            var val = values.First();
            if (double.TryParse(val, out var seconds))
                return Math.Clamp((int)(seconds * 1000), 1000, 60000);

            if (DateTimeOffset.TryParse(val, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var retryAt))
            {
                var milliseconds = (retryAt - DateTimeOffset.UtcNow).TotalMilliseconds;
                if (milliseconds > 0)
                    return (int)Math.Clamp(milliseconds, 1000, 60000);
            }
        }

        return 2000;
    }

    private static bool HasRetryAfter(HttpResponseMessage response) =>
        response.Headers.TryGetValues("Retry-After", out var values) && values.Any();
}
