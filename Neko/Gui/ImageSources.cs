using System;
using System.Collections.Generic;
using System.Numerics;
using Dalamud.Logging;
using ImGuiNET;
using Neko.Sources;

namespace Neko.Gui;

/// <summary>
/// The "Image Sources" tab in the Config Menu
/// </summary>
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

    private readonly ImageSourceConfig[] SourceList = {
            new ImageSourceConfig("nekos.life", "Anime Catgirls", "https://nekos.life/",
                typeof(NekosLife), Plugin.Config.Sources.NekosLife),
            new ImageSourceConfig("shibe.online", "Shiba Inu Dogs", "https://shibe.online/",
                typeof(ShibeOnline), Plugin.Config.Sources.ShibeOnline),
            new ImageSourceConfig("Catboys", "Anime Catboys","https://catboys.com/",
                typeof(Catboys), Plugin.Config.Sources.Catboys),
            new ImageSourceConfig("waifu.im", "Anime Waifus","https://waifu.im/",
                typeof(Waifuim), Plugin.Config.Sources.Waifuim),
            new ImageSourceConfig("Waifu.pics", "Anime Waifus","https://waifu.pics/",
                typeof(WaifuPics), Plugin.Config.Sources.WaifuPics),
            new ImageSourceConfig("Pic.re", "High resolution Anime Images","https://pic.re/",
                typeof(PicRe), Plugin.Config.Sources.PicRe),
            new ImageSourceConfig("Dog CEO", "Dogs","https://dog.ceo/",
                typeof(DogCEO), Plugin.Config.Sources.DogCEO),
            new ImageSourceConfig("The Cat API", "Cats","https://thecatapi.com/",
                typeof(TheCatAPI), Plugin.Config.Sources.TheCatAPI)
        };

    private const float INDENT = 32f;
    private static (TheCatAPI.Breed[], string[])? TheCatAPIBreedNames;
    private static (DogCEO.Breed[], string[])? DogCEOBreedNames;

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
        if (Plugin.Config.Sources.Waifuim.enabled && NSFW.AllowNSFW) // NSFW Check
            DrawWaifuim(SourceList[3]);
        //  ------------ Waifu.pics --------------
        SourceCheckbox(SourceList[4], ref Plugin.Config.Sources.WaifuPics.enabled);
        if (Plugin.Config.Sources.WaifuPics.enabled)
            DrawWaifuPics(SourceList[4]);
        //  ------------ Pic.re --------------
        SourceCheckbox(SourceList[5], ref Plugin.Config.Sources.PicRe.enabled);
        //  ------------ Dog CEO --------------
        SourceCheckbox(SourceList[6], ref Plugin.Config.Sources.DogCEO.enabled);
        if (Plugin.Config.Sources.DogCEO.enabled)
            DrawDogCEO(SourceList[6]);
        //  ------------ TheCatAPI --------------
        SourceCheckbox(SourceList[7], ref Plugin.Config.Sources.TheCatAPI.enabled);
        if (Plugin.Config.Sources.TheCatAPI.enabled)
            DrawTheCatAPI(SourceList[7]);
        CheckIfNoSource();
    }


    private static void DrawWaifuPics(ImageSourceConfig source)
    {
        ImGui.Indent(INDENT);
        var wp = Plugin.Config.Sources.WaifuPics;
        var preview = "";
        foreach (var f in Helper.GetFlags(wp.sfwCategories))
        {
            preview += (Enum.GetName(typeof(WaifuPics.CategoriesSFW), f) ?? "unknown") + ", ";
        }
        if (NSFW.AllowNSFW) // NSFW Check
        {
            foreach (var f in Helper.GetFlags(wp.nsfwCategories))
            {
                preview += "NSFW " + (Enum.GetName(typeof(WaifuPics.CategoriesNSFW), f) ?? "unknown") + ", ";
            }
        }
        if (preview.Length > 3)
            preview = preview[..^2];
        else
            preview = "No categories selected";

        if (ImGui.BeginCombo("Categories##WaifuPics", preview))
        {
            EnumSelectable(source, "Waifu##WaifuPics", WaifuPics.CategoriesSFW.Waifu, ref wp.sfwCategories);
            EnumSelectable(source, "Neko##WaifuPics", WaifuPics.CategoriesSFW.Neko, ref wp.sfwCategories);
            EnumSelectable(source, "Shinobi##WaifuPics", WaifuPics.CategoriesSFW.Shinobu, ref wp.sfwCategories);
            EnumSelectable(source, "Megumin##WaifuPics", WaifuPics.CategoriesSFW.Megumin, ref wp.sfwCategories);
            EnumSelectable(source, "Awoo##WaifuPics", WaifuPics.CategoriesSFW.Awoo, ref wp.sfwCategories);
            if (NSFW.AllowNSFW) // NSFW Check
            {
                EnumSelectable(source, "NSFW Waifu##WaifuPics", WaifuPics.CategoriesNSFW.Waifu, ref wp.nsfwCategories);
                EnumSelectable(source, "NSFW Neko##WaifuPics", WaifuPics.CategoriesNSFW.Neko, ref wp.nsfwCategories);
                EnumSelectable(source, "NSFW Trap##WaifuPics", WaifuPics.CategoriesNSFW.Trap, ref wp.nsfwCategories);
            }
            ImGui.EndCombo();
        }
        if (preview.Length > 35)
            Common.ToolTip(preview);

        if (wp.sfwCategories == WaifuPics.CategoriesSFW.None && (wp.nsfwCategories == WaifuPics.CategoriesNSFW.None || !NSFW.AllowNSFW))
        {
            ImGui.TextColored(new Vector4(1f, 0f, 0f, 1f), "WARNING:"); ImGui.SameLine();
            ImGui.TextWrapped("No categories selected. Please select at least one image category,");
        }
        ImGui.Unindent(INDENT);
    }

    private static void DrawWaifuim(ImageSourceConfig source)
    {
        ImGui.Indent(INDENT);
        if (ImGui.Combo("Content##Waifuim", ref Plugin.Config.Sources.Waifuim.ContentComboboxIndex, new string[] { "SFW", "NSFW", "Both" }, 3))
        {
            Plugin.Config.Sources.Waifuim.sfw = Plugin.Config.Sources.Waifuim.ContentComboboxIndex == 0
                                             || Plugin.Config.Sources.Waifuim.ContentComboboxIndex == 2;
            Plugin.Config.Sources.Waifuim.nsfw = Plugin.Config.Sources.Waifuim.ContentComboboxIndex == 1
                                              || Plugin.Config.Sources.Waifuim.ContentComboboxIndex == 2;
            UpdateImageSource(source, true);
        }
        ImGui.Unindent(INDENT);
    }


    private static void DrawDogCEO(ImageSourceConfig source)
    {
        if (DogCEOBreedNames == null) // Load names only once, then use cached
        {
            var b = (DogCEO.Breed[])Enum.GetValues(typeof(DogCEO.Breed));
            var n = new string[b.Length];
            for (int i = 0; i < b.Length; i++)
            {
                if (b[i] == DogCEO.Breed.all)
                    n[i] = "All";
                else
                    n[i] = DogCEO.BreedName(b[i]);
            }
            DogCEOBreedNames = (b, n);
        }

        ImGui.Indent(INDENT);
        var (breeds, names) = DogCEOBreedNames ?? default;

        if (ImGui.BeginCombo("Breed##DogCeo", names[Plugin.Config.Sources.DogCEO.selected], ImGuiComboFlags.HeightLarge))
        {
            for (int i = 0; i < names.Length; i++)
            {
                if (ImGui.Selectable(names[i] + "##" + i, i == Plugin.Config.Sources.DogCEO.selected))
                {
                    Plugin.Config.Sources.DogCEO.selected = i;
                    Plugin.Config.Sources.DogCEO.breed = breeds[i];
                    Plugin.ImageSource.RemoveAll(source.Type);
                    Plugin.ImageSource.AddSource(source.Config.LoadConfig());
                    Plugin.Config.Save();
                }
                if (ImGui.IsItemHovered()
                    && breeds[i] != DogCEO.Breed.all
                    && DogCEO.BreedDictionary.ContainsKey(breeds[i]))
                    Common.ToolTip(DogCEO.BreedDictionary[breeds[i]].Description);
            }

            ImGui.EndCombo();
        }
        ImGui.Unindent(INDENT);

    }

    private static void DrawTheCatAPI(ImageSourceConfig source)
    {
        if (TheCatAPIBreedNames == null) // Load names only once, then use cached
        {
            var b = (TheCatAPI.Breed[])Enum.GetValues(typeof(TheCatAPI.Breed));
            var n = new string[b.Length];
            for (int i = 0; i < b.Length; i++)
            {
                if (b[i] == TheCatAPI.Breed.All)
                    n[i] = "All";
                else
                    n[i] = TheCatAPI.BreedDictionary[b[i]].Name;
            }
            TheCatAPIBreedNames = (b, n);
        }

        ImGui.Indent(INDENT);
        var (breeds, names) = TheCatAPIBreedNames ?? default;

        if (ImGui.BeginCombo("Breed##TheCatApi", names[Plugin.Config.Sources.TheCatAPI.selected], ImGuiComboFlags.HeightLarge))
        {
            for (int i = 0; i < names.Length; i++)
            {
                if (ImGui.Selectable(names[i] + "##" + i, i == Plugin.Config.Sources.TheCatAPI.selected))
                {
                    Plugin.Config.Sources.TheCatAPI.selected = i;
                    Plugin.Config.Sources.TheCatAPI.breed = breeds[i];
                    Plugin.ImageSource.RemoveAll(source.Type);
                    Plugin.ImageSource.AddSource(source.Config.LoadConfig());
                    Plugin.Config.Save();
                }
                if (ImGui.IsItemHovered()
                    && breeds[i] != TheCatAPI.Breed.All
                    && TheCatAPI.BreedDictionary.ContainsKey(breeds[i]))
                    Common.ToolTip(TheCatAPI.BreedDictionary[breeds[i]].Description);
            }
            ImGui.EndCombo();
        }
        ImGui.Unindent(INDENT);
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
        if (ImGui.Selectable(name + "##" + source.Name, combined.HasFlag(single), ImGuiSelectableFlags.DontClosePopups))
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
