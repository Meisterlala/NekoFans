using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Dalamud.Logging;
using ImGuiNET;


namespace Neko.Sources;

public static class Common
{
    static readonly HttpClient client = new()
    {
        DefaultRequestHeaders = {
            UserAgent =
             {
                new("NekoFans", Assembly.GetExecutingAssembly().GetName().Version?.ToString()),
                new("(a Plugin for Final Fantasy XIV)")
            }
        }
    };

    static readonly JsonSerializerOptions jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        AllowTrailingCommas = true,
        ReadCommentHandling = JsonCommentHandling.Skip
    };

    public async static Task<NekoImage> DownloadImage(string url, CancellationToken ct = default)
    {
        byte[]? bytes;
        try
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

            var response = await client.SendAsync(request, ct).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();
            bytes = await response.Content.ReadAsByteArrayAsync(ct).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            throw new Exception("Could not download image from: " + url, ex);
        }

        PluginLog.Log($"Downloaded {Helper.SizeSuffix(bytes.LongLength, 1)} from {url}");

        ct.ThrowIfCancellationRequested();
        NekoImage? image = new(bytes);
        return image;
    }

    public async static Task<T> ParseJson<T>(string url, CancellationToken ct = default)
    {
        T? result;
        try
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

            var response = await client.SendAsync(request, ct).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();
            var stream = await response.Content.ReadAsStreamAsync(ct).ConfigureAwait(false);
            result = await JsonSerializer.DeserializeAsync<T>(stream, jsonOptions, ct).ConfigureAwait(false);
        }
        catch (HttpRequestException ex)
        {
            throw new Exception("Could not Download .json form: " + url, ex);
        }
        catch (Exception ex)
        {
            throw new Exception("Could not Parse .json File from: " + url, ex);
        }

        if (result == null)
            throw new Exception("Could not Parse .json File from: " + url);

        ct.ThrowIfCancellationRequested();
        return result;
    }

}
