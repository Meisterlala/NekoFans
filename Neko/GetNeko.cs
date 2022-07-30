using System.IO;
using ImGuiScene;
using Dalamud.Logging;
using System.Threading.Tasks;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Collections.Generic;
using System.Text.Json;
using System.Reflection;

namespace Neko
{

    class NekosLifeJson
    {
        public string? url { get; set; }
    }

    public class GetNeko
    {
        private static readonly HttpClient client = new HttpClient();

        async static public Task<TextureWrap> nextNeko()
        {
            var url = "https://nekos.life/api/v2/img/neko";

            // get a random image URL
            NekosLifeJson? response;
            try
            {
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(
                    new MediaTypeWithQualityHeaderValue("application/json"));

                var streamTask = client.GetStreamAsync(url);
                response = await JsonSerializer.DeserializeAsync<NekosLifeJson>(await streamTask);
            }
            catch (System.Net.Http.HttpRequestException e)
            {
                PluginLog.LogError("Could not get a random neko image.");
                PluginLog.LogError(e.Message);
                throw;
            }
            catch (JsonException e)
            {
                PluginLog.LogError("Could not parse the json, which contains the url to a random image.");
                PluginLog.LogError(e.Message);
                throw;
            }
            if (response == null || response.url == null)
            {
                PluginLog.LogError("No response from Server");
                throw new System.Exception("No response from Server.");
            }

            // download actual image
            byte[]? bytes;
            try
            {
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(
                    new MediaTypeWithQualityHeaderValue("image/jpeg"));
                client.DefaultRequestHeaders.Accept.Add(
                    new MediaTypeWithQualityHeaderValue("image/png"));
                bytes = await client.GetByteArrayAsync(response.url);
            }
            catch (System.Net.Http.HttpRequestException e)
            {
                PluginLog.LogError("Could not download " + response.url);
                PluginLog.LogError(e.Message);
                throw;
            }

            PluginLog.Log("Downloaded " + bytes.Length / 1000 + " kb from " + response.url);

            TextureWrap? image;
            try
            {
                image = await Plugin.PluginInterface.UiBuilder.LoadImageAsync(bytes);
            }
            catch (System.Exception e)
            {
                PluginLog.LogError("Could not decode image");
                PluginLog.LogError(e.ToString());
                throw;
            }

            return image;
        }


        static public TextureWrap defaultNeko()
        {
            // Load icon synchronous as a fallback from embedded
            PluginLog.LogWarning("Loading default Neko image");

            var assembly = Assembly.GetExecutingAssembly();
            var resourceName = "Neko.resources.error.jpg";

            TextureWrap? image;
            try
            {
                using (Stream? stream = assembly.GetManifestResourceStream(resourceName))
                using (var memoryStream = new MemoryStream())
                {
                    stream?.CopyTo(memoryStream);
                    image = Plugin.PluginInterface.UiBuilder.LoadImage(memoryStream.ToArray());
                }
            }
            catch (System.Exception)
            {
                PluginLog.LogError("Could not load default image");
                throw;
            }

            return image;
        }
    }
}