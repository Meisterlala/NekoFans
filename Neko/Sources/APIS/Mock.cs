using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Dalamud.Logging;

namespace Neko.Sources.APIS;

public class Mock : IImageSource
{
    public static readonly List<MockImage> MockImages = new()
    {
        new("/home/ninja/Pictures/neko/n1.jpg"),
        new("/home/ninja/Pictures/neko/n2.jpg"),
        new("/home/ninja/Pictures/neko/n3.jpg"),
        new("/home/ninja/Pictures/neko/n4.jpg"),
    };

#pragma warning disable CA2211 // Non-constant fields should not be visible
#if DEBUG
    public static bool Enabled = true;
#else
    public static bool Enabled;
#endif

    public static bool CheckedEnabled;
#pragma warning restore CA2211

    public bool Faulted { get; set; }

    private readonly byte[] Data;
    private readonly string FileName;

    private static volatile int Index;
    private static readonly object IndexLock = new();

    public Mock(byte[] data, string fileName)
    {
        Data = data;
        FileName = fileName;
    }

    public Task<NekoImage> Next(CancellationToken ct = default)
    {
        var image = new NekoImage(Data, FileName);
        lock (IndexLock)
        {
            Index++;
            if (Index >= 5)
                DebugHelper.RandomThrow(message: $"Mock Image {Index}");
            image.DebugInfo = $"Mock Image {Index}";
            image.Description = $"Mock Image\nFile: {FileName}\nImage Index {Index}";
        }
        return Task.FromResult(image);
    }

    public static IImageSource? CreateCombinedSource()
    {
        if (!Enabled) return null;

        var images = MockImage.LoadList(MockImages);
        var cs = new CombinedSource();
        foreach (var (imageData, name) in images)
        {
            cs.AddSource(new Mock(imageData, name));
        }
        return cs;
    }

    public override string ToString() => $"Mock from {FileName}";

    public class MockImage
    {
        public string Path;
        public byte[]? Data;

        public MockImage(string path) => Path = path;

        public void LoadData()
        {
            if (Data != null) return;
            try
            {
                PluginLog.LogWarning("Loading mock image (this should only happen while debugging): {0}", Path);
                Data = File.ReadAllBytes(System.IO.Path.GetFullPath(Path));
            }
            catch (Exception ex)
            {
                PluginLog.LogError(ex, "Could not find image at path {0}", Path);
            }
        }

        public static List<(byte[], string)> LoadList(List<MockImage> mocks)
        {
            var images = new List<(byte[], string)>();
            foreach (var mock in mocks)
            {
                mock.LoadData();
                if (mock.Data != null)
                    images.Add((mock.Data, System.IO.Path.GetFileName(mock.Path)));
            }
            return images;
        }
    }
}