using System;
using System.Net.Http;
using System.Reflection;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Dalamud.Logging;


namespace Neko.Sources;

public static class Common
{
    private static readonly HttpClient client = new(
        new HttpClientHandler()
        {
            AutomaticDecompression = System.Net.DecompressionMethods.All
        }
    )
    {
        DefaultRequestHeaders = {
            UserAgent =
             {
                new("NekoFans", Assembly.GetExecutingAssembly().GetName().Version?.ToString()),
                new("(a Plugin for Final Fantasy XIV)")
            },
        },
    };

    /// <summary>
    /// Download an Image and Store it in a <see cref="NekoImage"/>
    /// </summary>
    public static async Task<NekoImage> DownloadImage(HttpRequestMessage request, CancellationToken ct = default)
    {
        byte[]? bytes;
        try
        {
            var response = await client.SendAsync(request, ct).ConfigureAwait(false);
            if (response.RequestMessage != null)
                PluginLog.LogDebug("Sent request to download image:\n" + response.RequestMessage?.ToString());
            response.EnsureSuccessStatusCode();
            bytes = await response.Content.ReadAsByteArrayAsync(ct).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            throw new Exception("Could not download image from: " + request.RequestUri, ex);
        }

        PluginLog.Log($"Downloaded {Helper.SizeSuffix(bytes.LongLength, 1)} from {request.RequestUri}");

        ct.ThrowIfCancellationRequested();
        NekoImage? image = new(bytes, request.RequestUri?.ToString() ?? "");
        return image;
    }

    public static async Task<NekoImage> DownloadImage(string url, CancellationToken ct = default)
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
        return await DownloadImage(request, ct).ConfigureAwait(false);
    }


    /// <summary>
    /// Downloads and Parses a .json file
    /// </summary>
    public static async Task<T> ParseJson<T>(HttpRequestMessage request, CancellationToken ct = default)
    {
        // Download .json file to stream
        System.IO.Stream? stream;
        HttpResponseMessage response;
        try
        {
            response = await client.SendAsync(request, ct).ConfigureAwait(false);
            if (response.RequestMessage != null)
                PluginLog.LogDebug("Sending request to get json:\n" + request.ToString());
            response.EnsureSuccessStatusCode();
            stream = await response.Content.ReadAsStreamAsync(ct).ConfigureAwait(false);
        }
        catch (HttpRequestException ex)
        {
            if (ex.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
                throw new Exception("Exceded Limits of the API. Please try again later.", ex);

            throw new Exception("Could not Download .json from: " + request.RequestUri, ex);
        }
        catch (Exception ex)
        {
            throw new Exception("Error occured when trying to donwload: " + request.RequestUri, ex);
        }

        // Parse Json
        T? result;
        try
        {
            var context = JsonContext.GetTypeInfo<T>();
            result = await JsonSerializer.DeserializeAsync(stream, context, ct).ConfigureAwait(false);

            if (result == null)
                throw new Exception("Did not get a response from: " + request.RequestUri);
        }
        catch (Exception ex)
        {
            PluginLog.LogDebug(response.Content.ReadAsStringAsync(ct).Result);
            throw new Exception("Could not Parse .json File from: " + request.RequestUri, ex);
        }

#if DEBUG   // Log Respone in DEBUG build (this is slow)
        PluginLog.LogVerbose($"Response from {request.RequestUri} trying to get a json:\n{JsonSerializer.Serialize(result, new JsonSerializerOptions() { WriteIndented = true })}");
#endif

        ct.ThrowIfCancellationRequested();
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
