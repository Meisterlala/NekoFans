using System;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Dalamud.Logging;
using Neko.Sources;

namespace Neko.Drawing;

public class Embedded : ImageSource
{
    public static readonly Embedded ImageError = new("error.jpg");
    public static readonly Embedded ImageLoading = new("loading.jpg");

    public override string Name => "Embedded";
    public override string ToString() => $"Embedded Image: {Filename}";
    public bool Ready => Image.CurrentState == NekoImage.State.LoadedGPU;
    public readonly NekoImage Image;

    private readonly string Filename;
    private Download.Response? LoadedBytes;
    private readonly object LoadingLock = new();

    public Embedded(string filename)
    {
        Filename = filename;
        Image = Next();
    }

    public override NekoImage Next(CancellationToken ct = default)
    {
        return new NekoImage((_) =>
        {
            // Only load texture if it was never loaded. 
            if (LoadedBytes.HasValue) return Task.FromResult(LoadedBytes.Value);

            lock (LoadingLock)
            {
                // Return if it was loaded while waiting for the lock.
                if (LoadedBytes.HasValue) return Task.FromResult(LoadedBytes.Value);

                // Load embedded error icon
                var assembly = Assembly.GetExecutingAssembly();
                var resourceName = $"Neko.resources.{Filename}";

                try
                {
                    using var stream = assembly.GetManifestResourceStream(resourceName);
                    using var memoryStream = new MemoryStream();
                    stream?.CopyTo(memoryStream);
                    var bytes = memoryStream.ToArray();
                    LoadedBytes = new Download.Response { Data = bytes, Url = resourceName };
                }
                catch (Exception)
                {
                    PluginLog.LogFatal("Could not load embedded image: {0}", resourceName);
                    throw;
                }

                PluginLog.LogVerbose("Loaded embedded image: {0}", resourceName);
                return Task.FromResult(LoadedBytes.Value);
            }
        }, this);
    }

    public override bool SameAs(ImageSource other) => other is Embedded e && e.Filename == Filename;

    public static implicit operator NekoImage(Embedded e) => e.Image;
}
