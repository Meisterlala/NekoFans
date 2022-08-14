using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Dalamud.Logging;
using ImGuiNET;


namespace Neko.Sources
{
    public static class Common
    {
        static readonly JsonSerializerOptions jsonOptions = new()
        {
            PropertyNameCaseInsensitive = true,
            AllowTrailingCommas = true,
            ReadCommentHandling = JsonCommentHandling.Skip
        };

        static readonly ProductInfoHeaderValue userAgent =
            new("NekoFans", Assembly.GetExecutingAssembly().GetName().Version?.ToString());

        public async static Task<NekoImage> DownloadImage(string url, CancellationToken ct = default)
        {
            HttpClient client = new();
            byte[]? bytes;
            try
            {
                client.DefaultRequestHeaders.Accept.Add(
                    new MediaTypeWithQualityHeaderValue("image/jpeg"));
                client.DefaultRequestHeaders.Accept.Add(
                    new MediaTypeWithQualityHeaderValue("image/png"));
                client.DefaultRequestHeaders.UserAgent.Add(userAgent);
                bytes = await client.GetByteArrayAsync(url, ct);
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
            HttpClient client = new();
            T? result;
            try
            {
                client.DefaultRequestHeaders.Accept.Add(
                    new MediaTypeWithQualityHeaderValue("application/json"));
                client.DefaultRequestHeaders.UserAgent.Add(userAgent);
                var stream = await client.GetStreamAsync(url, ct);
                result = await JsonSerializer.DeserializeAsync<T>(stream, jsonOptions, ct);
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
}