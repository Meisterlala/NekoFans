using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Dalamud.Logging;

namespace Neko.Sources
{

#pragma warning disable
    class NekosLifeJson
    {
        public string? url { get; set; }
    }
#pragma warning restore

    enum Category
    {
        Neko,
    }

    public class NekoLife : ImageSource
    {
        private static readonly HttpClient client = new();

        public async Task<NekoImage> Next(CancellationToken ct = default)
        {
            HttpClient client = new();
            var url = "https://nekos.life/api/v2/img/neko";
            HttpClient client = new();
            
            // Get a random image URL
            NekosLifeJson? response;
            try
            {
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(
                    new MediaTypeWithQualityHeaderValue("application/json"));

                var streamTask = client.GetStreamAsync(url, ct);
                response = await JsonSerializer.DeserializeAsync<NekosLifeJson>(utf8Json: await streamTask, cancellationToken: ct);
            }
            catch (HttpRequestException ex)
            {
                throw new Exception("Could not get a random neko image.", ex);
            }
            catch (JsonException ex)
            {
                throw new Exception("Could not parse the json, which contains the url to a random image.", ex);
            }
            catch (Exception ex)
            {
                throw new Exception("Could not connect to Server.", ex);
            }
            if (response == null || response.url == null)
            {
                throw new Exception($"No response from Server: {url}");
            }

            ct.ThrowIfCancellationRequested();

            // Download actual image
            byte[]? bytes;
            try
            {
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(
                    new MediaTypeWithQualityHeaderValue("image/jpeg"));
                client.DefaultRequestHeaders.Accept.Add(
                    new MediaTypeWithQualityHeaderValue("image/png"));
                bytes = await client.GetByteArrayAsync(response.url, ct);
            }
            catch (HttpRequestException ex)
            {
                throw new Exception("Could not download " + response.url, ex);
            }

            Helper.LogDownload(bytes.LongLength, response.url);

            ct.ThrowIfCancellationRequested();

            NekoImage? image = new(bytes);
            return image;
        }
    }

}