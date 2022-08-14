using System;
using System.Collections.Generic;
using System.Numerics;
using Dalamud.Logging;
using ImGuiNET;
using Neko.Sources;

namespace Neko.Gui
{
    public class ImageSourcesGUI
    {
        class ImageSourceConfig
        {
            public string Name;
            public string Description;
            public Type Type;
            public IImageConfig Config;

            public ImageSourceConfig(string name, string description, Type type, IImageConfig config)
            {
                Name = name;
                Description = description;
                Type = type;
                Config = config;
            }
        }

        private readonly List<ImageSourceConfig> SourceList = new() {
            new ImageSourceConfig("nekos.life", "Neko images", typeof(NekosLife), Plugin.Config.Sources.NekosLife),
            new ImageSourceConfig("shibe.online", "Shibe images", typeof(ShibeOnline), Plugin.Config.Sources.ShibeOnline),
            new ImageSourceConfig("Catboys", "catboys", typeof(Catboys), Plugin.Config.Sources.Catboys)
        };

        public void Draw()
        {
            SourceCheckbox(SourceList[0], ref Plugin.Config.Sources.NekosLife.enabled);
            SourceCheckbox(SourceList[1], ref Plugin.Config.Sources.ShibeOnline.enabled);
            SourceCheckbox(SourceList[2], ref Plugin.Config.Sources.Catboys.enabled);

            CheckIfNoSource();
        }

        private static void CheckIfNoSource()
        {
            // If any are enabled, enable the queue again
            if (Plugin.ImageSource.Count() > 0)
            {
                if (Plugin.GuiMain != null)
                    Plugin.GuiMain.Queue.StopQueue = false;
                return;
            }

            // Stop queue new images if there are no image sources
            if (Plugin.GuiMain != null)
                Plugin.GuiMain.Queue.StopQueue = true;
            ImGui.TextColored(new Vector4(1f, 0f, 0f, 1f), "WARNING:");
            ImGui.SameLine();
            ImGui.TextWrapped("No Image source is selected. This makes loading new images impossible.");
        }

        private static void SourceCheckbox(ImageSourceConfig source, ref bool enabled)
        {
            if (ImGui.Checkbox(source.Name, ref enabled))
            {
                Plugin.ImageSource.RemoveAll(source.Type);
                if (enabled)
                    Plugin.ImageSource.AddSource(source.Config.LoadConfig());
                Plugin.Config.Save();
            }
        }
    }
}