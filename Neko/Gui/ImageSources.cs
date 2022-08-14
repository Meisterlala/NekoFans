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
            public string Help;
            public Type Type;
            public IImageConfig Config;

            public ImageSourceConfig(string name, string description, string help, Type type, IImageConfig config)
            {
                Name = name;
                Description = description;
                Help = help;
                Type = type;
                Config = config;
            }
        }

        private readonly List<ImageSourceConfig> SourceList = new() {
            new ImageSourceConfig("nekos.life", "Anime Catgirls", "https://nekos.life/",
                typeof(NekosLife), Plugin.Config.Sources.NekosLife),
            new ImageSourceConfig("shibe.online", "Shiba Inu dogs", "https://shibe.online/",
                typeof(ShibeOnline), Plugin.Config.Sources.ShibeOnline),
            new ImageSourceConfig("Catboys", "Anime Catboys","https://catboys.com/",
                typeof(Catboys), Plugin.Config.Sources.Catboys),
            new ImageSourceConfig("waifu.im", "Anime Waifus","https://waifu.im/",
                typeof(Waifuim), Plugin.Config.Sources.Waifuim)
        };

        private const float INDENT = 32f;

        public void Draw()
        {
            SourceCheckbox(SourceList[0], ref Plugin.Config.Sources.NekosLife.enabled);
            SourceCheckbox(SourceList[1], ref Plugin.Config.Sources.ShibeOnline.enabled);
            SourceCheckbox(SourceList[2], ref Plugin.Config.Sources.Catboys.enabled);
            SourceCheckbox(SourceList[3], ref Plugin.Config.Sources.Waifuim.enabled);
            if (Plugin.Config.Sources.Waifuim.enabled)
            {
                ImGui.Indent(INDENT);
                if (ImGui.Combo("Content", ref Plugin.Config.Sources.Waifuim.ContentComboboxIndex, new string[] { "SFW", "NSFW", "Both" }, 3))
                {
                    Plugin.Config.Sources.Waifuim.sfw = Plugin.Config.Sources.Waifuim.ContentComboboxIndex == 0
                                                     || Plugin.Config.Sources.Waifuim.ContentComboboxIndex == 2;
                    Plugin.Config.Sources.Waifuim.nsfw = Plugin.Config.Sources.Waifuim.ContentComboboxIndex == 1
                                                      || Plugin.Config.Sources.Waifuim.ContentComboboxIndex == 2;
                    UpdateImageSource(SourceList[3], true);
                }
                ImGui.Unindent(INDENT);
            }

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
                UpdateImageSource(source, enabled);
            ImGui.SameLine();
            ImGui.TextDisabled(source.Description);
            ImGui.SameLine();
            Common.HelpMarker(source.Help);
        }

        private static void UpdateImageSource(ImageSourceConfig source, bool enabled)
        {
            Plugin.ImageSource.RemoveAll(source.Type);
            if (enabled)
                Plugin.ImageSource.AddSource(source.Config.LoadConfig());
            Plugin.Config.Save();
        }

    }
}