using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Dalamud.Interface.Textures;
using Dalamud.Interface.Textures.TextureWraps;
using Neko.Sources;

namespace Neko.Drawing;

public class Embedded : ImageSource
{
    public static readonly Embedded ImageError = new("error.jpg");
    public static readonly Embedded ImageLoading = new("loading.jpg");
    public static readonly Embedded ImageIcon = new("icon.png");


    public override string Name => "Embedded";
    public override string ToString() => $"Embedded Image: {Filename}";
    public bool Ready => Image?.CurrentState == NekoImage.State.LoadedGPU;
    public NekoImage? Image { get; private set; }

    private readonly string Filename;
    private Download.Response? LoadedBytes;
    private readonly object LoadingLock = new();


    public Embedded(string filename) => Filename = filename;

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
                    Plugin.Log.Fatal("Could not load embedded image: {0}", resourceName);
                    throw;
                }

                Plugin.Log.Verbose("Loaded embedded image: {0}", resourceName);
                return Task.FromResult(LoadedBytes.Value);
            }
        }, this);
    }

    /// <summary>
    /// Load all embedded images. This should only be called once.
    /// </summary>
    public static void LoadAll()
    {
        // Load Embedded Images without waiting for them to finish
        Task.Run(async () =>
        {
            // Find all embedded images
            List<Embedded> embedded = new();
            foreach (var field in typeof(Embedded).GetFields())
            {
                if (field.FieldType != typeof(Embedded) || !field.IsStatic) continue;
                embedded.Add((Embedded)field.GetValue(null)!);
            }

            foreach (var emb in embedded)
            {
                var errors = 0;
                do
                {
                    // Load image
                    emb.Image = emb.Next();
                    emb.Image.RequestLoadGPU();
                    // Wait until it is loaded
                    await emb.Image.Await((s) => s is NekoImage.State.LoadedGPU or NekoImage.State.Error).ConfigureAwait(false);

                    if (errors > 0)
                        Plugin.Log.Verbose("Retrying to load embedded image: {0}", emb.Filename);
                    if (errors > 5)
                    {
                        Plugin.Log.Fatal($"Error loading embedded image: {emb.Filename}");
                        break;
                    }
                    errors++;
                } while (emb.Image.CurrentState == NekoImage.State.Error);
            }
        });
    }

    /// <summary>
    /// Gets a DalamudTextureWrap from the embedded icon.
    /// </summary>
    public ISharedImmediateTexture GetShared()
    {
        if (Image == null) throw new Exception("Embedded image was not loaded yet");

        var assembly = Assembly.GetExecutingAssembly();
        var resourceName = $"Neko.resources.{Filename}";
        return Plugin.TextureProvider.GetFromManifestResource(assembly, resourceName);
    }

    public override bool SameAs(ImageSource other) => other is Embedded e && e.Filename == Filename;

    public static implicit operator NekoImage(Embedded e)
        => e.Image ?? throw new Exception("Embedded image was not loaded yet");
}
