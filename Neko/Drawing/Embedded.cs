using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Dalamud.Logging;

namespace Neko.Drawing;

public class Embedded
{
    public static readonly Embedded ImageError = new("error.jpg");
    public static readonly Embedded ImageLoading = new("loading.jpg");

    public volatile NekoImage? Image;
    public bool Ready { get; private set; }
    private readonly string Filename;

    public Embedded(string filename) => Filename = filename;

    public async Task<NekoImage> Load()
    {
        // Only load texture if it was never loaded. 
        if (Image != null) return Image;

        // Load embedded error icon
        if (Ready)
            PluginLog.LogWarning("Reloading exsisting embedded image");
        var assembly = Assembly.GetExecutingAssembly();
        var resourceName = $"Neko.resources.{Filename}";

        try
        {
            using var stream = assembly.GetManifestResourceStream(resourceName);
            using var memoryStream = new MemoryStream();
            stream?.CopyTo(memoryStream);
            var bytes = memoryStream.ToArray();
            var img = new NekoImage(bytes);
            await img.DecodeAndLoadGPUAsync();
            Image = img;
        }
        catch (Exception)
        {
            PluginLog.LogFatal("Could not load embedded image: {0}", resourceName);
            throw;
        }

        PluginLog.LogVerbose("Loaded embedded image: {0}", resourceName);
        Ready = true;
        return Image;
    }

    public override string ToString() => $"Embedded Image: {Filename}";

    public static implicit operator NekoImage(Embedded embedded) => embedded.Image ?? throw new Exception("await Load() before accessing the texture");
    public static implicit operator Task<NekoImage>(Embedded embedded) => embedded.Load();
}
