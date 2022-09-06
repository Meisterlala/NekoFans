using System.Text.Json;
using Dalamud.Configuration;
using Dalamud.Logging;
using Neko.Sources;
using Neko.Sources.APIS;

namespace Neko;

public class Configuration : IPluginConfiguration
{

    public class SourceConfig
    {
        public Catboys.Config Catboys = new();
        public DogCEO.Config DogCEO = new();
        public NekosLife.Config NekosLife = new();
        public PicRe.Config PicRe = new();
        public ShibeOnline.Config ShibeOnline = new();
        public TheCatAPI.Config TheCatAPI = new();
        public Twitter.Config Twitter = new();
        public Waifuim.Config Waifuim = new();
        public WaifuPics.Config WaifuPics = new();
    }

    public enum ImageAlignment
    {
        TopLeft, Top, TopRight, Left, Center, Right, BottomLeft, Bottom, BottomRight
    }

    public int Version { get; set; } = 1;

    public SourceConfig Sources = new();
    public float GuiMainOpacity;
    public bool GuiMainShowResize;
    public bool GuiMainShowTitleBar = true;
    public bool GuiMainAllowResize = true;
    public bool GuiMainVisible;
    public bool GuiMainLocked;

    public bool SlideshowEnabled;
    public double SlideshowIntervalSeconds = 60 * 5; // 5 minutes

    public int QueueDownloadCount = 5;
    public int QueuePreloadCount = 2;

    public ImageAlignment Alignment = ImageAlignment.Center;

    public void Save() => Plugin.PluginInterface.SavePluginConfig(this);

    public CombinedSource LoadSources()
    {
        CombinedSource combined = new();
        combined.AddSource(Sources.Catboys.LoadConfig());
        combined.AddSource(Sources.DogCEO.LoadConfig());
        combined.AddSource(Sources.NekosLife.LoadConfig());
        combined.AddSource(Sources.PicRe.LoadConfig());
        combined.AddSource(Sources.ShibeOnline.LoadConfig());
        combined.AddSource(Sources.TheCatAPI.LoadConfig());
        combined.AddSource(Sources.Twitter.LoadConfig());
        combined.AddSource(Sources.Waifuim.LoadConfig());
        combined.AddSource(Sources.WaifuPics.LoadConfig());
#if DEBUG // Load the test source in debug mode
        combined.AddSource(Mock.LoadSources());
#endif
        return combined;
    }


    public static Configuration Load()
    {
        try
        {
            return Plugin.PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
        }
        catch (System.Exception ex)
        {
            PluginLog.LogWarning(ex, "Could not load Neko Fans config");
            return new Configuration();
        }
    }

    public override string ToString() =>
        JsonSerializer.Serialize(this, typeof(Configuration),
            new JsonSerializerOptions() { WriteIndented = true, IncludeFields = true });
}


