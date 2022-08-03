using System;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Dalamud.Logging;
using ImGuiScene;

namespace Neko
{
    public class NekoImage
    {
        // Support for multi CPU computers
        private enum Architecture
        {
            Workstation32Bit, Workstation64Bit, Server32Bit, Server64Bit
        }

        // Realistically only Workstation64Bit and Server64Bit will ever be used, since FFXIV is 64Bit only
        // see https://docs.microsoft.com/en-us/dotnet/standard/garbage-collection/workstation-server-gc
        // and https://mattwarren.org/2016/08/16/Preventing-dotNET-Garbage-Collections-with-the-TryStartNoGCRegion-API/
        private static long EphemeralMemorySizeTable(Architecture arch) =>
            arch switch
            {
                Architecture.Workstation32Bit => 1024 * 1024 * 15,  //  15 MB
                Architecture.Workstation64Bit => 1024 * 1024 * 243, // 243 MB
                Architecture.Server32Bit => 1024 * 1024 * 255,      // 255 MB
                Architecture.Server64Bit => 1024 * 1024 * 1024,     //   1 GB
                _ => throw new ArgumentOutOfRangeException(nameof(arch)),
            };


        // see https://docs.microsoft.com/en-us/dotnet/standard/garbage-collection/fundamentals#ephemeral-generations-and-segments
        private static long? _ephemeralMemorySize;
        private static long EphemeralMemorySize
        {
            get
            {
                if (_ephemeralMemorySize != null) return (long)_ephemeralMemorySize;
                // Get GC type. see https://docs.microsoft.com/en-us/dotnet/core/runtime-config/garbage-collector
                var gc_type = Environment.GetEnvironmentVariable("DOTNET_gcServer") ??
                              Environment.GetEnvironmentVariable("COMPlus_gcServer") ?? "0";
                var arch = Architecture.Server32Bit;
                if (Environment.Is64BitProcess & gc_type == "0")
                    arch = Architecture.Workstation64Bit;
                else if (!Environment.Is64BitProcess & gc_type == "0")
                    arch = Architecture.Workstation32Bit;
                else if (Environment.Is64BitProcess)
                    arch = Architecture.Server64Bit;
                _ephemeralMemorySize = EphemeralMemorySizeTable(arch);
                PluginLog.LogDebug("Detectec GC architecture: " + arch);
                return (long)_ephemeralMemorySize;
            }
        }

        private byte[]? _data;
        private TextureWrap? _texture;

        public TextureWrap Texture
        {
            get
            {
                if (_texture != null) return _texture;
                throw new Exception("await LoadImage() before accessing the texture");
            }
        }


        public NekoImage(byte[] data)
        {
            _data = data;
            // This somehow doesnt work, so its done with NoGCRegion
            // GC.KeepAlive(_data);
        }

        ~NekoImage()
        {
            Dispose();
        }

        public void Dispose()
        {
            _texture?.Dispose();
            _texture = null;
#if DEBUG
            PluginLog.LogDebug("Disposed {0} image", Helper.SizeSuffix(_data?.Length ?? 0));
#endif
            _data = null;
        }

        public async Task<TextureWrap> LoadImage()
        {
            if (_data == null || _data.Length == 0)
            {
                throw new Exception("No Image data provided");
            }

            // Pause GC
            try
            {
                // try correct size
                System.GC.TryStartNoGCRegion(EphemeralMemorySize, true);
            }
            catch (ArgumentOutOfRangeException)
            {
                // This will catch, if:
                // - EphemeralMemorySize is to big: Fallback to 32bit GC size
                // - Already in NoGCRegion
                PluginLog.Log("Fallback to smaller GC bufffer " + Helper.SizeSuffix(EphemeralMemorySize, 3));
                System.GC.TryStartNoGCRegion(EphemeralMemorySizeTable(Architecture.Workstation32Bit), true);
            }
            catch (InvalidOperationException)
            {
                // This will catch, if already in NoGCRegion
            }

            try
            {
                // Load Image to GPU
                // This will cause a System.AccessViolationException if a GC colletion happens
                _texture = await Plugin.PluginInterface.UiBuilder.LoadImageAsync(_data);

                if (_texture == null) // This should never happen
                    throw new Exception("Image null");
            }
            catch (System.Exception ex)
            {
                // This will catch, if:
                // - the GC is collected while the image is loading
                // - there is not enough ram avalible
                // - there is not enough vram avalible
                // - the NoGCRegion totalSize was too small and a GC collection happend
                // - the image data is corupt
                throw new Exception("Could not load image", ex);
            }

            // Restart GC
            try { System.GC.EndNoGCRegion(); }
            catch (System.Exception)
            {
                // This will catch, if:
                // - NoGCRegion totalSize was too small
                PluginLog.LogDebug("Could not end NoGCRegion");
            }

#if !DEBUG
            // we can delete the original image data to clear up ram, 
            // since the uncompressed image is now stored in vram
            _data = null;
#endif

            return _texture;
        }

        static private NekoImage? defaultNekoImage; // uses 7.3 MB vram
        static public TextureWrap DefaultNekoTexture
        {
            get
            {
                if (defaultNekoImage != null) return defaultNekoImage.Texture;
                throw new Exception("Default image not yet loaded");
            }
        }

        static public bool DefaultNekoReady { get; private set; }

        static public async Task<NekoImage> DefaultNeko()
        {
            // Only load texture if it was never loaded. 
            if (defaultNekoImage != null) return defaultNekoImage;

            // Load embedded error icon as a fallback
            if (DefaultNekoReady)
                PluginLog.LogWarning("Reloading default Neko image");
            var assembly = Assembly.GetExecutingAssembly();
            var resourceName = "Neko.resources.error.jpg";

            try
            {
                using Stream? stream = assembly.GetManifestResourceStream(resourceName);
                using var memoryStream = new MemoryStream();
                stream?.CopyTo(memoryStream);
                var bytes = memoryStream.ToArray();
                var img = new NekoImage(bytes);
                await img.LoadImage();
                defaultNekoImage = img;
            }
            catch (System.Exception)
            {
                PluginLog.LogFatal("Could not load default image");
                throw;
            }

            DefaultNekoReady = true;
            return defaultNekoImage;
        }
    }
}