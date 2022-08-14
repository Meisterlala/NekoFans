using System;
using System.Collections.Generic;
using System.Numerics;
using Dalamud.Logging;
using ImGuiNET;
using Neko.Sources;

namespace Neko.Gui;


public class ImageSourcesGUI
{
    const bool NSFW_ENABELD = false;

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
                typeof(Waifuim), Plugin.Config.Sources.Waifuim),
            new ImageSourceConfig("Waifu.pics", "Anime Waifus","https://waifu.pics/",
                typeof(WaifuPics), Plugin.Config.Sources.WaifuPics),
            new ImageSourceConfig("Pic.re", "High resolution Anime Images","https://pic.re/",
                typeof(PicRe), Plugin.Config.Sources.PicRe)
        };

    private const float INDENT = 32f;

    public void Draw()
    {
        //  ------------ nekos.life --------------
        SourceCheckbox(SourceList[0], ref Plugin.Config.Sources.NekosLife.enabled);
        //  ------------ shibe.online --------------
        SourceCheckbox(SourceList[1], ref Plugin.Config.Sources.ShibeOnline.enabled);
        //  ------------ Catboys --------------
        SourceCheckbox(SourceList[2], ref Plugin.Config.Sources.Catboys.enabled);
        //  ------------ waifu.im --------------
        SourceCheckbox(SourceList[3], ref Plugin.Config.Sources.Waifuim.enabled);
        if (Plugin.Config.Sources.Waifuim.enabled && NSFW_ENABELD)
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
        //  ------------ Waifu.pics --------------
        SourceCheckbox(SourceList[4], ref Plugin.Config.Sources.WaifuPics.enabled);
        if (Plugin.Config.Sources.WaifuPics.enabled)
        {
            ImGui.Indent(INDENT);
            var wp = Plugin.Config.Sources.WaifuPics;
            var preview = "";
            foreach (var f in Helper.GetFlags(wp.sfwCategories))
            {
                preview += (Enum.GetName(typeof(WaifuPics.CategoriesSFW), f) ?? "unknown") + ", ";
            }
            foreach (var f in Helper.GetFlags(wp.nsfwCategories))
            {
                preview += "NSFW " + (Enum.GetName(typeof(WaifuPics.CategoriesNSFW), f) ?? "unknown") + ", ";
            }
            if (preview.Length > 3)
                preview = preview[..^2];
            else
                preview = "No categories selected";

            if (ImGui.BeginCombo("Categories", preview))
            {
                EnumSelectable(SourceList[4], "Waifu", WaifuPics.CategoriesSFW.Waifu, ref wp.sfwCategories);
                EnumSelectable(SourceList[4], "Neko", WaifuPics.CategoriesSFW.Neko, ref wp.sfwCategories);
                EnumSelectable(SourceList[4], "Shinobi", WaifuPics.CategoriesSFW.Shinobu, ref wp.sfwCategories);
                EnumSelectable(SourceList[4], "Megumin", WaifuPics.CategoriesSFW.Megumin, ref wp.sfwCategories);
                EnumSelectable(SourceList[4], "Awoo", WaifuPics.CategoriesSFW.Awoo, ref wp.sfwCategories);
                if (NSFW_ENABELD)
                {
                    EnumSelectable(SourceList[4], "NSFW Waifu", WaifuPics.CategoriesNSFW.Waifu, ref wp.nsfwCategories);
                    EnumSelectable(SourceList[4], "NSFW Neko", WaifuPics.CategoriesNSFW.Neko, ref wp.nsfwCategories);
                    EnumSelectable(SourceList[4], "NSFW Trap", WaifuPics.CategoriesNSFW.Trap, ref wp.nsfwCategories);
                }
                ImGui.EndCombo();
            }
            if (preview.Length > 35)
                Common.ToolTip(preview);

            if (wp.sfwCategories == WaifuPics.CategoriesSFW.None && wp.nsfwCategories == WaifuPics.CategoriesNSFW.None)
            {
                ImGui.TextColored(new Vector4(1f, 0f, 0f, 1f), "WARNING:"); ImGui.SameLine();
                ImGui.TextWrapped("No categories selected. Please select at least one image category,");
            }
            ImGui.Unindent(INDENT);
        }
        //  ------------ Pic.re --------------
        SourceCheckbox(SourceList[5], ref Plugin.Config.Sources.PicRe.enabled);
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

    private static void EnumSelectable<T>(ImageSourceConfig source, string name, T single, ref T combined) where T : Enum
    {
        if (ImGui.Selectable(name, combined.HasFlag(single), ImGuiSelectableFlags.DontClosePopups))
        {
            int comb = Convert.ToInt32(combined);
            int sing = Convert.ToInt32(single);
            if (combined.HasFlag(single))
                comb &= ~sing;
            else
                comb |= sing;
            combined = (T)Enum.ToObject(typeof(T), comb);
            Plugin.ImageSource.RemoveAll(source.Type);
            Plugin.ImageSource.AddSource(source.Config.LoadConfig());
            Plugin.Config.Save();
        }
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
