using System;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Dalamud.Logging;

namespace Neko.Sources;

public static class Download
{
    public struct Response
    {
        public byte[] Data;
        public string Url;
    }

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
            var response = await Plugin.HttpClient.SendAsync(request, ct).ConfigureAwait(false);
            if (response.RequestMessage != null)
                DebugHelper.LogNetwork(() => "Sent request to download image:\n" + response.RequestMessage?.ToString());
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
    {
        DebugHelper.RandomThrow(DebugHelper.ThrowChance.ParseJson);
        await DebugHelper.RandomDelay(DebugHelper.Delay.ParseJson, ct).ConfigureAwait(false);

        // Download .json file to stream
        System.IO.Stream? stream;
        HttpResponseMessage response;
        try
        {
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
        }
        catch (HttpRequestException ex)
        {
            // Handle 429 (Too Many Requests) by waiting and retrying
            if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
            {
                DebugHelper.LogNetwork(() => "API retuned 429 (Too Many Requests)\n" + response.Headers.ToString());

                var retryAfter = 2000; // in ms
                // Respect timeout header for WAIFU.IM
                if (response.Headers.TryGetValues("Retry-After", out var values) && values.Any())
                {
                    var val = values.First();
                    if (double.TryParse(val, out var seconds))
                        retryAfter = (int)(seconds * 1000);
                }

                // Twitter API limit reached
                if (APIS.Twitter.Is429Response(response))
                {
                    APIS.Twitter.IsRateLimited = true;
                    throw new Exception("Twitter API limit reached. Wait a few days until the limit gets reset", ex);
                }

                Plugin.Log.Information($"API retuned 429 (Too Many Requests). Waiting {retryAfter / 1000.0} seconds before trying again.");
                // Wait 2 seconds and retry
                await Task.Delay(retryAfter, ct).ConfigureAwait(false);
                ct.ThrowIfCancellationRequested();
                // Clone request, because you cant send the same one twice
                var newRequest = Helper.RequestClone(request);
                return await ParseJson<T>(newRequest, ct).ConfigureAwait(false);
            }

            DebugHelper.LogNetwork(() => $"Error Downloading Json from {request.RequestUri}:\n{JsonSerializer.Serialize(response.Content.ReadAsStringAsync(ct).Result, new JsonSerializerOptions() { WriteIndented = true })}");
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
            result = await JsonSerializer.DeserializeAsync(stream, context, ct).ConfigureAwait(false);

            if (result == null)
                throw new Exception("Did not get a response from: " + request.RequestUri);
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex)
        {
            Plugin.Log.Debug(await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false));
            throw new Exception("Could not Parse .json File from: " + request.RequestUri, ex);
        }

        DebugHelper.LogNetwork(() => $"Response from {request.RequestUri} trying to get a json:\n{JsonSerializer.Serialize(result, new JsonSerializerOptions() { WriteIndented = true })}");

        return result;
    }

    public static async Task<T> ParseJson<T>(string url, CancellationToken ct = default)
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
        return await ParseJson<T>(request, ct).ConfigureAwait(false);
    }
}
