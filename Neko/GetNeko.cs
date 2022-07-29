using System.IO;
using ImGuiScene;
using Dalamud.Logging;
using System.Threading.Tasks;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Collections.Generic;
using System.Text.Json;

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
            byte[] bytes;
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

            return Plugin.PluginInterface.UiBuilder.LoadImage(bytes);
        }


        static public TextureWrap defaultNeko()
        {
            PluginLog.LogWarning("Loading default Neko image");

            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("image/jpeg"));

            client.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("image/png"));

            // Load icon syncron as a fallback
            var task = client.GetByteArrayAsync("https://raw.githubusercontent.com/Meisterlala/NekoLife/master/icon.png");
            task.Wait();
            return Plugin.PluginInterface.UiBuilder.LoadImage(task.Result);
        }
    }
}