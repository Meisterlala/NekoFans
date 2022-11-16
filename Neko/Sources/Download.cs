using System;
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
        await DebugHelper.RandomDelay(DebugHelper.Delay.DownloadImage, ct);

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
            PluginLog.Log($"Downloaded {Helper.SizeSuffix(bytes.LongLength, 1)} from {request.RequestUri}");

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
        return await DownloadImage(request, called, ct);
    }

    /// <summary>
    /// Downloads and Parses a .json file
    /// </summary>
    public static async Task<T> ParseJson<T>(HttpRequestMessage request, CancellationToken ct = default)
    {
        DebugHelper.RandomThrow(DebugHelper.ThrowChance.ParseJson);
        await DebugHelper.RandomDelay(DebugHelper.Delay.ParseJson, ct);

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
            if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
                throw new Exception("Exceded Limits of the API. Please try again later.", ex);

            DebugHelper.LogNetwork(() => $"Error Downloading Json from {request.RequestUri}:\n{JsonSerializer.Serialize(response.Content.ReadAsStringAsync(ct).Result, new JsonSerializerOptions() { WriteIndented = true })}");
            var exception = new HttpRequestException($"Could not Download .json from: {request.RequestUri} ({response.StatusCode})", ex);
            exception.Data.Add("StatusCode", response.StatusCode);
            exception.Data.Add("Content", response.Content.ReadAsStringAsync(ct).Result);
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
            PluginLog.LogDebug(response.Content.ReadAsStringAsync(ct).Result);
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
