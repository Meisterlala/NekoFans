using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Dalamud.Logging;
using Neko.Drawing;

namespace Neko.Sources.APIS;

public class Mock : ImageSource
{
    public static readonly List<MockImage> MockImages = new()
    {
   //     new("X:\\1.jpg"),
  //      new("X:\\2.png"),
 //       new("X:\\3.gif"),
 //       new("X:\\4.gif"),
 //       new("X:\\different_frames.gif"),
        new("X:\\slap_012.gif"),
        new("X:\\really_big.gif"),
        new("X:\\transparent.gif"),
    };

#pragma warning disable CA2211 // Non-constant fields should not be visible
#if DEBUG
    public static bool Enabled = true;
#else
    public static bool Enabled;
#endif
#pragma warning restore CA2211

    public override string Name => "Mock";

    private readonly byte[] Data;
    private readonly string FileName;

    private static volatile int Index;
    private static readonly object IndexLock = new();

    private static bool SourcesUpdated;
    private static List<(byte[], string)> MockSources = MockImage.LoadList(MockImages);

    public Mock(byte[] data, string fileName)
    {
        Data = data;
        FileName = fileName;
    }

    public override NekoImage Next(CancellationToken ct = default)
    {
#if !DEBUG
        throw new Exception("Mock is only available in debug builds");
#pragma warning disable CS0162
#endif
        // await DebugHelper.RandomDelay(DebugHelper.Delay.Mock, ct);
        var image = new NekoImage(Data, this);
        lock (IndexLock)
        {
            Index++;
            if (Index >= 10)
                DebugHelper.RandomThrow(DebugHelper.ThrowChance.Mock);
            image.DebugInfo = $"Mock Image {Index}";
            image.Description = $"Mock Image\nFile: {FileName}\nImage Index {Index}";
        }
        return image;
    }

    public static CombinedSource LoadSources()
    {
        if (!Enabled) return new();

        CombinedSource res = new();
        foreach (var (imageData, name) in MockSources)
        {
            res.AddSource(new Mock(imageData, name));
        }
        return res;
    }

    public static void UpdateImages()
    {
        MockSources = MockImage.LoadList(MockImages);
        SourcesUpdated = true;
    }

    public override string ToString() => $"Mock from {FileName}";

    public override bool SameAs(ImageSource other) => !SourcesUpdated;

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
