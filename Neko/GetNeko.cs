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
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));

            var streamTask = client.GetStreamAsync(url);
            // Should be safety checked
            var response = await JsonSerializer.DeserializeAsync<NekosLifeJson>(await streamTask);

            // Might even throw an exception before this point and crash
            if (response == null)
            {
                return defaultNeko();
            }

            // download actual image
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("image/jpeg"));
            var bytes = await client.GetByteArrayAsync(response.url);
            PluginLog.Log("Downloaded " + bytes.Length / 1000 + " kb from " + response.url);

            return Plugin.PluginInterface.UiBuilder.LoadImage(bytes);
        }

        static public TextureWrap currentNeko()
        {
            var assemblyLocation = Plugin.PluginInterface.AssemblyLocation.DirectoryName!;
            var imagePath = Path.Combine(assemblyLocation, $@"images\fox.jpg");

            return Plugin.PluginInterface.UiBuilder.LoadImage("");
        }

        static public TextureWrap defaultNeko()
        {
            var assemblyLocation = Plugin.PluginInterface.AssemblyLocation.DirectoryName!;
            var imagePath = Path.Combine(assemblyLocation, $@"images\fox.png");
            PluginLog.Log("Imagepath: " + imagePath);
            return Plugin.PluginInterface.UiBuilder.LoadImage(imagePath);
        }
    }
}