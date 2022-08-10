using System.Collections.Generic;
using Dalamud.Configuration;

namespace Neko
{
    public class Configuration : IPluginConfiguration
    {
        public enum ImageAlignment
        {
            TopLeft, Top, TopRight, Left, Center, Right, BottomLeft, Bottom, BottomRight
        }

        public int Version { get; set; } = 1;

        public float GuiMainOpacity = 0f;
        public bool GuiMainShowResize = false;

        public ImageAlignment Alignment = ImageAlignment.Center;

        public void Save()
        {
            Plugin.PluginInterface.SavePluginConfig(this);
        }

        public static Configuration Load()
        {
            return Plugin.PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
        }
    }
}