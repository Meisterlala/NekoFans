using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System;
using Dalamud.Logging;

#if DEBUG
using System.IO;
#endif


namespace Neko
{


#pragma warning disable 
    class NekosLifeJson
    {
        public string? url { get; set; }
    }
#pragma warning restore

    public static class GetNeko
    {
        private static readonly HttpClient client = new();

        /// <summary>
        /// Load the next image form the web to ram, not to vram yet
        /// </summary>
        public async static Task<NekoImage> NextNeko(CancellationToken ct = default)
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

            PluginLog.Log("Downloaded {0} from {1}", Helper.SizeSuffix(bytes.LongLength, 1), response.url);
#if DEBUG
            // Write last image to disk
            Directory.CreateDirectory("C:\\Temp\\neko");
            File.WriteAllBytes("C:\\Temp\\neko\\last.jpg", bytes);
#endif

            ct.ThrowIfCancellationRequested();

            // Create Image, dont load it to GPU yet
            NekoImage? image = new(bytes);
            return image;
        }
    }
}