using Dalamud.Configuration;
using Dalamud.Logging;
using Neko.Sources;
using Neko.Sources.APIS;

namespace Neko;

public class Configuration : IPluginConfiguration
{

    public class SourceConfig
    {
        public NekosLife.Config NekosLife = new();
        public ShibeOnline.Config ShibeOnline = new();
        public Catboys.Config Catboys = new();
        public Waifuim.Config Waifuim = new();
        public WaifuPics.Config WaifuPics = new();
        public PicRe.Config PicRe = new();
        public DogCEO.Config DogCEO = new();
        public TheCatAPI.Config TheCatAPI = new();
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

    public int QueueDownloadCount = 5;
    public int QueuePreloadCount = 2;

    public ImageAlignment Alignment = ImageAlignment.Center;

    public void Save() => Plugin.PluginInterface.SavePluginConfig(this);

    public CombinedSource LoadSources()
    {
        CombinedSource combined = new();
        combined.AddSource(Sources.NekosLife.LoadConfig());
        combined.AddSource(Sources.ShibeOnline.LoadConfig());
        combined.AddSource(Sources.Catboys.LoadConfig());
        combined.AddSource(Sources.Waifuim.LoadConfig());
        combined.AddSource(Sources.WaifuPics.LoadConfig());
        combined.AddSource(Sources.PicRe.LoadConfig());
        combined.AddSource(Sources.DogCEO.LoadConfig());
        combined.AddSource(Sources.TheCatAPI.LoadConfig());
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
}


