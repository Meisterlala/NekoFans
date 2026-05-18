using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Text.Json;
using System.Threading;

namespace Neko.Sources;

public static class Download
{
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
                MarkRateLimited(response, called);
                await WaitForSourceRateLimit(rateLimitKey, RetryAfterMilliseconds(response), ct).ConfigureAwait(false);

                return await DownloadImage(Helper.RequestClone(request), called, ct).ConfigureAwait(false);
            }
            response.EnsureSuccessStatusCode();
            bytes = await response.Content.ReadAsByteArrayAsync(ct).ConfigureAwait(false);
        }
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
            MarkRateLimitSucceeded(response, called);
        }
        catch (HttpRequestException ex)
        {
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

                MarkRateLimited(response, called);
                await WaitForSourceRateLimit(rateLimitKey, RetryAfterMilliseconds(response), ct).ConfigureAwait(false);

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
    {
        var rateLimit = SourceRateLimits.GetOrAdd(key, _ => new SourceRateLimit(key));
        await rateLimit.Wait(retryAfterMilliseconds, ct).ConfigureAwait(false);
    }

    private static string RateLimitKey(HttpRequestMessage request, Type? called = default) => called == null ? request.RequestUri?.Host ?? "unknown" : called.FullName ?? called.Name;

    private static void MarkRateLimited(HttpResponseMessage response, Type? called = default)
    {
        if (called == typeof(APIS.Nekosia) || APIS.Nekosia.IsApiResponse(response))
            APIS.Nekosia.IsRateLimited = true;
    }

    private static void MarkRateLimitSucceeded(HttpResponseMessage response, Type? called = default)
    {
        if (called == typeof(APIS.Nekosia) && APIS.Nekosia.IsApiResponse(response))
            APIS.Nekosia.IsRateLimited = false;
    }

    private sealed class SourceRateLimit(string key)
    {
        private readonly SemaphoreSlim semaphore = new(1, 1);
        private readonly string key = key;
        private DateTimeOffset waitUntil = DateTimeOffset.MinValue;

        public async Task Wait(CancellationToken ct)
        {
            var delay = waitUntil - DateTimeOffset.UtcNow;
            if (delay > TimeSpan.Zero)
                await Task.Delay(delay, ct).ConfigureAwait(false);
        }

        public async Task Wait(int retryAfterMilliseconds, CancellationToken ct)
        {
            await semaphore.WaitAsync(ct).ConfigureAwait(false);
            try
            {
                var now = DateTimeOffset.UtcNow;
                var existingDelay = waitUntil - now;
                if (existingDelay.TotalMilliseconds < retryAfterMilliseconds - 250)
                {
                    waitUntil = now.AddMilliseconds(retryAfterMilliseconds);
                    Plugin.Log.Information($"{key} returned 429 (Too Many Requests). Waiting {retryAfterMilliseconds / 1000.0} seconds before trying again.");
                }
            }
            finally
            {
                semaphore.Release();
            }

            await Wait(ct).ConfigureAwait(false);
        }
    }

    private static int RetryAfterMilliseconds(HttpResponseMessage response)
    {
        if (response.Headers.TryGetValues("Retry-After", out var values) && values.Any())
        {
            var val = values.First();
            if (double.TryParse(val, out var seconds))
                return Math.Clamp((int)(seconds * 1000), 1000, 60000);
        }

        return 2000;
    }
}
