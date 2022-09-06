using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using ImGuiNET;
using Neko.Sources;
using Neko.Sources.APIS;

namespace Neko.Gui;

/// <summary>
/// The "Image Sources" tab in the Config Menu
/// </summary>
public class ImageSourcesGUI
{
    private class ImageSourceConfig
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
            new ImageSourceConfig("Nekos.life", "Anime Catgirls", "https://nekos.life/",
                typeof(NekosLife), Plugin.Config.Sources.NekosLife),
            new ImageSourceConfig("shibe.online", "Shiba Inu Dogs", "https://shibe.online/",
                typeof(ShibeOnline), Plugin.Config.Sources.ShibeOnline),
            new ImageSourceConfig("Catboys", "Anime Catboys","https://catboys.com/",
                typeof(Catboys), Plugin.Config.Sources.Catboys),
            new ImageSourceConfig("WAIFU.IM", "Anime Waifus","https://waifu.im/",
                typeof(Waifuim), Plugin.Config.Sources.Waifuim),
            new ImageSourceConfig("Waifu.pics", "Anime Waifus","https://waifu.pics/",
                typeof(WaifuPics), Plugin.Config.Sources.WaifuPics),
            new ImageSourceConfig("Pic.re", "High resolution Anime Images","https://pic.re/",
                typeof(PicRe), Plugin.Config.Sources.PicRe),
            new ImageSourceConfig("Dog CEO", "Dogs","https://dog.ceo/",
                typeof(DogCEO), Plugin.Config.Sources.DogCEO),
            new ImageSourceConfig("The Cat API", "Cats","https://thecatapi.com/",
                typeof(TheCatAPI), Plugin.Config.Sources.TheCatAPI),
            new ImageSourceConfig("Twitter", "Twitter","https://twitter.com/",
                typeof(Twitter), Plugin.Config.Sources.Twitter),
        };

    private readonly Vector4 TwitterDark = new(0.0549f, 0.29411f, 0.4431372f, 1f);
    private readonly Vector4 TwitterLight = new(0.11372549f, 0.6313725f, 0.94901960f, 0.8f);
    private readonly Vector4 TableTextBG = new(0.29019607f, 0.29019607f, 0.29019607f, 0.823529f);
    private readonly Vector4 TableTextRed = new(0.38823529f, 0.1098039f, 0.1098039f, 1f);

    private const float INDENT = 32f;
    private static (TheCatAPI.Breed[], string[])? TheCatAPIBreedNames;
    private static (DogCEO.Breed[], string[])? DogCEOBreedNames;

    public void Draw()
    {
        // ------------ Mock Images for debugging --------------
        if (Plugin.PluginInterface.IsDevMenuOpen)
            DrawMock();
        //  ------------ nekos.life --------------
        SourceCheckbox(SourceList[0], ref Plugin.Config.Sources.NekosLife.enabled);
        //  ------------ shibe.online --------------
        SourceCheckbox(SourceList[1], ref Plugin.Config.Sources.ShibeOnline.enabled);
        //  ------------ Catboys --------------
        SourceCheckbox(SourceList[2], ref Plugin.Config.Sources.Catboys.enabled);
        //  ------------ waifu.im --------------
        SourceCheckbox(SourceList[3], ref Plugin.Config.Sources.Waifuim.enabled);
        if (Plugin.Config.Sources.Waifuim.enabled && NSFW.AllowNSFW) // NSFW Check
            DrawWaifuim();
        //  ------------ Waifu.pics --------------
        SourceCheckbox(SourceList[4], ref Plugin.Config.Sources.WaifuPics.enabled);
        if (Plugin.Config.Sources.WaifuPics.enabled)
            DrawWaifuPics(SourceList[4]);
        //  ------------ Pic.re --------------
        SourceCheckbox(SourceList[5], ref Plugin.Config.Sources.PicRe.enabled);
        //  ------------ Dog CEO --------------
        SourceCheckbox(SourceList[6], ref Plugin.Config.Sources.DogCEO.enabled);
        if (Plugin.Config.Sources.DogCEO.enabled)
            DrawDogCEO();
        //  ------------ TheCatAPI --------------
        SourceCheckbox(SourceList[7], ref Plugin.Config.Sources.TheCatAPI.enabled);
        if (Plugin.Config.Sources.TheCatAPI.enabled)
            DrawTheCatAPI();
        //  ------------ Twitter --------------
        SourceCheckbox(SourceList[8], ref Plugin.Config.Sources.Twitter.enabled);
        if (Plugin.Config.Sources.Twitter.enabled)
            DrawTwitter();

        CheckIfNoSource();
    }

    private static void DrawMock()
    {
#if DEBUG
        if (ImGui.Checkbox("Mock Imagages##Mock", ref Mock.Enabled))
            Plugin.UpdateImageSource();

        ImGui.SameLine();
        ImGui.TextDisabled("This should only be visible in debug mode");

        if (Mock.Enabled && ImGui.Button("Update Mock Images##Mock"))
        {
            Mock.UpdateImages();
            Plugin.UpdateImageSource();
        }
#endif
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

        preview = preview.Length > 3 ? preview[..^2] : "No categories selected";

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

    private static void DrawWaifuim()
    {
        ImGui.Indent(INDENT);
        if (ImGui.Combo("Content##Waifuim", ref Plugin.Config.Sources.Waifuim.ContentComboboxIndex, new string[] { "SFW", "NSFW", "Both" }, 3))
        {
            Plugin.Config.Sources.Waifuim.sfw = Plugin.Config.Sources.Waifuim.ContentComboboxIndex is 0 or 2;
            Plugin.Config.Sources.Waifuim.nsfw = Plugin.Config.Sources.Waifuim.ContentComboboxIndex is 1 or 2;
            Plugin.UpdateImageSource();
        }
        ImGui.Unindent(INDENT);
    }


    private static void DrawDogCEO()
    {
        if (DogCEOBreedNames == null) // Load names only once, then use cached
        {
            var b = (DogCEO.Breed[])Enum.GetValues(typeof(DogCEO.Breed));
            var n = new string[b.Length];
            for (var i = 0; i < b.Length; i++)
            {
                n[i] = b[i] == DogCEO.Breed.all ? "All" : DogCEO.BreedName(b[i]);
            }
            DogCEOBreedNames = (b, n);
        }

        ImGui.Indent(INDENT);
        var (breeds, names) = DogCEOBreedNames ?? default;

        if (ImGui.BeginCombo("Breed##DogCeo", names[Plugin.Config.Sources.DogCEO.selected], ImGuiComboFlags.HeightLarge))
        {
            for (var i = 0; i < names.Length; i++)
            {
                if (ImGui.Selectable(names[i] + "##" + i, i == Plugin.Config.Sources.DogCEO.selected))
                {
                    Plugin.Config.Sources.DogCEO.selected = i;
                    Plugin.Config.Sources.DogCEO.breed = breeds[i];
                    Plugin.Config.Save();
                    Plugin.UpdateImageSource();
                }
                if (ImGui.IsItemHovered()
                    && breeds[i] != DogCEO.Breed.all
                    && DogCEO.BreedDictionary.ContainsKey(breeds[i]))
                {
                    Common.ToolTip(DogCEO.BreedDictionary[breeds[i]].Description);
                }
            }

            ImGui.EndCombo();
        }
        ImGui.Unindent(INDENT);

    }

    private static void DrawTheCatAPI()
    {
        if (TheCatAPIBreedNames == null) // Load names only once, then use cached
        {
            var b = (TheCatAPI.Breed[])Enum.GetValues(typeof(TheCatAPI.Breed));
            var n = new string[b.Length];
            for (var i = 0; i < b.Length; i++)
            {
                n[i] = b[i] == TheCatAPI.Breed.All ? "All" : TheCatAPI.BreedDictionary[b[i]].Name;
            }
            TheCatAPIBreedNames = (b, n);
        }

        ImGui.Indent(INDENT);
        var (breeds, names) = TheCatAPIBreedNames ?? default;

        if (ImGui.BeginCombo("Breed##TheCatApi", names[Plugin.Config.Sources.TheCatAPI.selected], ImGuiComboFlags.HeightLarge))
        {
            for (var i = 0; i < names.Length; i++)
            {
                if (ImGui.Selectable(names[i] + "##" + i, i == Plugin.Config.Sources.TheCatAPI.selected))
                {
                    Plugin.Config.Sources.TheCatAPI.selected = i;
                    Plugin.Config.Sources.TheCatAPI.breed = breeds[i];
                    Plugin.Config.Save();
                    Plugin.UpdateImageSource();
                }
                if (ImGui.IsItemHovered()
                    && breeds[i] != TheCatAPI.Breed.All
                    && TheCatAPI.BreedDictionary.ContainsKey(breeds[i]))
                {
                    Common.ToolTip(TheCatAPI.BreedDictionary[breeds[i]].Description);
                }
            }
            ImGui.EndCombo();
        }
        ImGui.Unindent(INDENT);
    }


    private static List<TwitterTableEntry>? TwitterTableEntries;

    private class TwitterTableEntry
    {
        public Twitter.Config.Query Query;

        public Twitter? ImageSource;
        public bool IsDirty;

        public TwitterTableEntry(Twitter.Config.Query query, Twitter? imageSource, bool isDirty)
        {
            Query = query;
            ImageSource = imageSource;
            IsDirty = isDirty;
        }
    }

    private int selectedTwitterEntry = -1;
    private bool showTwitterHelp;


    private void DrawTwitter()
    {
        // Create Table if there is none
        if (TwitterTableEntries == null)
        {
            TwitterTableEntries = new();
            var imageSources = Plugin.ImageSource.GetAll<Twitter>();
            foreach (var query in Plugin.Config.Sources.Twitter.queries)
            {
                var source = imageSources.Find((s) => s.ConfigQuery == query);
                TwitterTableEntries.Add(new(query, source, false));
            }
        }

        // Add a default entry if the Table is empty
        if (TwitterTableEntries.Count == 0)
            TwitterTableEntries.Add(new(new(), null, false));

        // Status of the Tweet (message, helptext?)
        static (string, string?) TweetStatus(TwitterTableEntry? entry)
            => entry == null
                ? (" ", null)
                : entry.ImageSource == null
                ? ("?", null)
                : entry.ImageSource.Faulted
                ? ("ERROR", null)
                : entry?.ImageSource?.TweetStatus() ?? ("?", null);

        // Find max width needed of TweetCount Column or use default
        var tweetCountColumWidth = TwitterTableEntries.Count > 0
            ? TwitterTableEntries.Max((e) => ImGui.CalcTextSize(TweetStatus(e).Item1).X)
            : ImGui.CalcTextSize(TweetStatus(null).Item1).X;

        // It should be bigger than the header
        if (tweetCountColumWidth < ImGui.CalcTextSize("Count").X)
            tweetCountColumWidth = ImGui.CalcTextSize("Count").X;

        ImGui.Indent(INDENT);
        ImGui.BeginTable("TwitterConfig##Twitter", 3, ImGuiTableFlags.PadOuterX | ImGuiTableFlags.RowBg);

        ImGui.TableSetupColumn("Enabled##Twitter", ImGuiTableColumnFlags.WidthFixed | ImGuiTableColumnFlags.NoResize | ImGuiTableColumnFlags.NoSort);
        ImGui.TableSetupColumn("Search Text##Twitter", ImGuiTableColumnFlags.WidthStretch, 100f - ImGui.GetColumnWidth(0) - tweetCountColumWidth);
        ImGui.TableSetupColumn("Status##Twitter", ImGuiTableColumnFlags.WidthFixed, tweetCountColumWidth);
        ImGui.TableHeadersRow();

        // Color of Selectable
        ImGui.PushStyleColor(ImGuiCol.Header, TwitterDark);
        ImGui.PushStyleColor(ImGuiCol.HeaderHovered, TwitterLight);

        // Frame Background Color
        ImGui.PushStyleColor(ImGuiCol.FrameBg, TableTextBG);

        for (var i = 0; i < TwitterTableEntries.Count; i++)
        {
            ImGui.TableNextColumn();
            var entry = TwitterTableEntries[i];

            // Enabled Checkbox
            var checkboxSize = ImGui.GetFontSize() + (ImGui.GetStyle().FramePadding.X * 2);
            var checkboxX = (ImGui.GetColumnWidth() / 2) - (checkboxSize / 2) + ImGui.GetCursorPosX();
            ImGui.SetCursorPosX(checkboxX);
            if (ImGui.Checkbox($"##TwitterTableEntryEnabled_{i}", ref entry.Query.enabled) && entry.Query.searchText != "")
                entry.IsDirty = true;

            // Search Text
            ImGui.TableNextColumn();
            ImGui.PushItemWidth(-1);  // Remove Label
            if (entry.ImageSource?.Faulted ?? false)
                ImGui.PushStyleColor(ImGuiCol.FrameBg, TableTextRed);
            if (ImGui.InputText($"##TwitterTableEntrySearchText_{i}", ref entry.Query.searchText, 256))
                entry.IsDirty = true;
            if (entry.ImageSource?.Faulted ?? false)
                ImGui.PopStyleColor();
            ImGui.PopItemWidth();

            // Status
            ImGui.TableNextColumn();
            var (text, tooltip) = TweetStatus(entry);
            ImGui.Text(text);
            if (!string.IsNullOrEmpty(tooltip))
                Common.ToolTip(tooltip);

            // Selectabel Row
            ImGui.SameLine();
            if (ImGui.Selectable("##TwitterTableEntrySelectable_" + i, selectedTwitterEntry == i, ImGuiSelectableFlags.SpanAllColumns))
                selectedTwitterEntry = i;

        }

        // Pop Style Colors
        ImGui.PopStyleColor(3);

        ImGui.EndTable();

        // Add Button
        if (ImGui.Button("Add ##Twitter"))
        {
            Twitter.Config.Query query = new();
            Plugin.Config.Sources.Twitter.queries.Add(query);
            TwitterTableEntries.Add(new(query, null, false));
            Plugin.Config.Save();
        }

        string GetHelpText() => showTwitterHelp ? "Hide Help" : "Show Help";

        // Save button only when there are changes to save
        if (TwitterTableEntries.Find((e) => e.IsDirty) != null)
        {
            var lengthSave = ImGui.CalcTextSize("Save Changes").X + (ImGui.GetStyle().FramePadding.X * 2);
            var lengthHelp = ImGui.CalcTextSize(GetHelpText()).X + (ImGui.GetStyle().FramePadding.X * 2);
            ImGui.SameLine(((ImGui.GetWindowContentRegionMax().X - ImGui.GetWindowContentRegionMin().X - INDENT) / 2) - ((lengthSave + lengthHelp + ImGui.GetStyle().ItemSpacing.X) / 2) + INDENT);
            if (ImGui.Button("Save Changes##Twitter"))
            {
                // Remove all changed entries
                foreach (var entry in TwitterTableEntries)
                {
                    if (entry.IsDirty)
                        entry.ImageSource = null;
                }

                Plugin.Config.Save();
                Plugin.UpdateImageSource();

                // Update ImageSource references and reset dirty flag
                foreach (var entry in TwitterTableEntries)
                {
                    if (entry.IsDirty || entry.ImageSource == null)
                    {
                        entry.ImageSource = Plugin.ImageSource.GetAll<Twitter>().Find((s) => s.ConfigQuery.Equals(entry.Query));
                        entry.IsDirty = false;
                    }
                }
            }
        }

        // Help Button
        {
            var lengthSave = ImGui.CalcTextSize("Save Changes").X + (ImGui.GetStyle().FramePadding.X * 2);
            var lengthHelp = ImGui.CalcTextSize(GetHelpText()).X + (ImGui.GetStyle().FramePadding.X * 2);
            // If there is a Save button, align it to the right
            if (TwitterTableEntries.Find((e) => e.IsDirty) != null)
                ImGui.SameLine(((ImGui.GetWindowContentRegionMax().X - ImGui.GetWindowContentRegionMin().X - INDENT) / 2) - ((lengthSave + lengthHelp + ImGui.GetStyle().ItemSpacing.X) / 2) + INDENT + (lengthSave + ImGui.GetStyle().ItemSpacing.X));
            else
                ImGui.SameLine(((ImGui.GetWindowContentRegionMax().X - ImGui.GetWindowContentRegionMin().X - INDENT) / 2) - (lengthHelp / 2) + INDENT);

            if (ImGui.Button($"{GetHelpText()}##Twitter"))
                ImGui.OpenPopup("TwitterHelp");
        }


        // Remove Button (Right align)
        if (selectedTwitterEntry >= 0)
        {
            var length = ImGui.CalcTextSize("Remove").X;
            ImGui.SameLine(ImGui.GetWindowContentRegionMax().X - ImGui.GetWindowContentRegionMin().X - length);
            if (ImGui.Button("Remove##Twitter") && selectedTwitterEntry >= 0)
            {
                Plugin.Config.Sources.Twitter.queries.RemoveAt(selectedTwitterEntry);
                TwitterTableEntries.RemoveAt(selectedTwitterEntry);
                Plugin.Config.Save();
                Plugin.UpdateImageSource();
                selectedTwitterEntry = selectedTwitterEntry < 0
                ? -1
                : selectedTwitterEntry > TwitterTableEntries.Count - 1
                ? TwitterTableEntries.Count - 1
                : selectedTwitterEntry;
            }
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
            var comb = Convert.ToInt32(combined);
            var sing = Convert.ToInt32(single);
            if (combined.HasFlag(single))
                comb &= ~sing;
            else
                comb |= sing;
            combined = (T)Enum.ToObject(typeof(T), comb);
            Plugin.Config.Save();
            Plugin.UpdateImageSource();
        }
    }

    private static void SourceCheckbox(ImageSourceConfig source, ref bool enabled)
    {
        if (ImGui.Checkbox(source.Name, ref enabled))
        {
            Plugin.Config.Save();
            Plugin.UpdateImageSource();
        }
        ImGui.SameLine();
        ImGui.TextDisabled(source.Description);
        ImGui.SameLine();
        Common.HelpMarker(source.Help);
    }

}
