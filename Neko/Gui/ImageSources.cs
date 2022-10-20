using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Dalamud.Logging;
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
            new ImageSourceConfig("nekos.best", "Anime Catgirls", "https://nekos.best/",
                typeof(NekosBest), Plugin.Config.Sources.NekosBest),
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

    private static readonly Vector4 TwitterDark = new(0.0549f, 0.29411f, 0.4431372f, 1f);
    private static readonly Vector4 TwitterLight = new(0.11372549f, 0.6313725f, 0.94901960f, 0.8f);
    private static readonly Vector4 TableTextBG = new(0.29019607f, 0.29019607f, 0.29019607f, 0.823529f);
    private static readonly Vector4 TableTextRed = new(0.38823529f, 0.1098039f, 0.1098039f, 1f);

    private const float INDENT = 32f;
    private static (TheCatAPI.Breed[], string[])? TheCatAPIBreedNames;
    private static (DogCEO.Breed[], string[])? DogCEOBreedNames;

    private readonly HeaderImage.Individual Header = new();

    public void Draw()
    {
        // ------------ Header --------------
        if (Plugin.Config.ShowHeaders)
            DrawHeader();
        // ------------ Mock Images for debugging --------------
        if (Plugin.PluginInterface.IsDevMenuOpen)
            DrawMock();
        //  ------------ nekos.life --------------
        SourceCheckbox(SourceList[0], ref Plugin.Config.Sources.NekosLife.enabled);
        //  ------------ nekos.best --------------
        SourceCheckbox(SourceList[1], ref Plugin.Config.Sources.NekosBest.enabled);
        if (Plugin.Config.Sources.NekosBest.enabled)
            DrawNekosBest(SourceList[1]);
        //  ------------ shibe.online --------------
        SourceCheckbox(SourceList[2], ref Plugin.Config.Sources.ShibeOnline.enabled);
        //  ------------ Catboys --------------
        SourceCheckbox(SourceList[3], ref Plugin.Config.Sources.Catboys.enabled);
        //  ------------ waifu.im --------------
        SourceCheckbox(SourceList[4], ref Plugin.Config.Sources.Waifuim.enabled);
        if (Plugin.Config.Sources.Waifuim.enabled && NSFW.AllowNSFW) // NSFW Check
            DrawWaifuim();
        //  ------------ Waifu.pics --------------
        SourceCheckbox(SourceList[5], ref Plugin.Config.Sources.WaifuPics.enabled);
        if (Plugin.Config.Sources.WaifuPics.enabled)
            DrawWaifuPics(SourceList[5]);
        //  ------------ Pic.re --------------
        SourceCheckbox(SourceList[6], ref Plugin.Config.Sources.PicRe.enabled);
        //  ------------ Dog CEO --------------
        SourceCheckbox(SourceList[7], ref Plugin.Config.Sources.DogCEO.enabled);
        if (Plugin.Config.Sources.DogCEO.enabled)
            DrawDogCEO();
        //  ------------ TheCatAPI --------------
        SourceCheckbox(SourceList[8], ref Plugin.Config.Sources.TheCatAPI.enabled);
        if (Plugin.Config.Sources.TheCatAPI.enabled)
            DrawTheCatAPI();
        //  ------------ Twitter --------------
        SourceCheckbox(SourceList[9], ref Plugin.Config.Sources.Twitter.enabled);
        if (Plugin.Config.Sources.Twitter.enabled)
            DrawTwitter();

        CheckIfNoSource();
    }

    private void DrawHeader()
    {
        var imgSize = Header.TryGetSize();
        if (imgSize == null)
            return;

        var regionMax = ImGui.GetWindowContentRegionMax();
        var regionMin = ImGui.GetWindowContentRegionMin();
        var height = (regionMax.Y - regionMin.Y) * 0.25f;
        var width = regionMax.X - regionMin.X - (2 * ImGui.GetStyle().WindowPadding.X);

        var (start, end) = Common.AlignImage(imgSize.Value, new Vector2(width, height), Configuration.ImageAlignment.Top);
        var cursorPos = ImGui.GetCursorPos();
        start += new Vector2(cursorPos.X + ImGui.GetStyle().WindowPadding.X, cursorPos.Y);
        end += cursorPos;

        Header.Draw((start, end));
        Common.ToolTip($"The amount of images you downloaded with Neko Fans is {Plugin.Config.LocalDownloadCount}");
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
            ImGui.TextWrapped("No categories selected. Please select at least one image category.");
        }
        ImGui.Unindent(INDENT);
    }

    private static void DrawNekosBest(ImageSourceConfig source)
    {
        ImGui.Indent(INDENT);
        var nb = Plugin.Config.Sources.NekosBest;
        var preview = "";
        foreach (var f in Helper.GetFlags(nb.categories))
        {
            preview += (Enum.GetName(typeof(NekosBest.Config.Category), f) ?? "unknown") + ", ";
        }

        preview = preview.Length > 3 ? preview[..^2] : "No categories selected";

        if (ImGui.BeginCombo("Categories##NekosBest", preview))
        {
            EnumSelectable(source, "Waifu##NekosBest", NekosBest.Config.Category.Waifu, ref nb.categories);
            EnumSelectable(source, "Neko##NekosBest", NekosBest.Config.Category.Neko, ref nb.categories);
            EnumSelectable(source, "Kitsune##NekosBest", NekosBest.Config.Category.Kitsune, ref nb.categories);
            EnumSelectable(source, "Husbando##NekosBest", NekosBest.Config.Category.Husbando, ref nb.categories);
            ImGui.EndCombo();
        }
        if (preview.Length > 35)
            Common.ToolTip(preview);

        if (nb.categories == NekosBest.Config.Category.None)
        {
            ImGui.TextColored(new Vector4(1f, 0f, 0f, 1f), "WARNING:"); ImGui.SameLine();
            ImGui.TextWrapped("No categories selected. Please select at least one image category.");
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
        public Twitter.Config.Query QueryDirty;

        public Twitter? ImageSource;
        public bool IsDirty;

        public TwitterTableEntry(Twitter.Config.Query query, Twitter? imageSource, bool isDirty)
        {
            Query = query;
            QueryDirty = query.Clone(); // Make a copy
            ImageSource = imageSource;
            IsDirty = isDirty;
        }
    }

    private int selectedTwitterEntry = -1;
    private bool twitterHelpOpen;

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
        {
            Twitter.Config.Query query = new();
            Plugin.Config.Sources.Twitter.queries.Add(query);
            TwitterTableEntries.Add(new(query, null, false));
            Plugin.Config.Save();
        }

        // Status of the Tweet (message, helptext?)
        static (string, string?) TweetStatus(TwitterTableEntry? entry)
            => entry == null
                ? (" ", null)
                : entry.ImageSource == null
                ? ("?", null)
                : entry?.ImageSource?.TweetStatus() ?? ("?", null);

        // Find max width needed of TweetCount Column or use default
        var tweetStatusColumWidth = TwitterTableEntries.Count > 0
            ? TwitterTableEntries.Max((e) => ImGui.CalcTextSize(TweetStatus(e).Item1).X + 5)
            : ImGui.CalcTextSize(TweetStatus(null).Item1).X;

        // It should be bigger than the header
        var statusWidth = ImGui.CalcTextSize("Status").X;
        if (tweetStatusColumWidth < statusWidth)
            tweetStatusColumWidth = statusWidth;

        ImGui.Indent(INDENT);
        ImGui.BeginTable("TwitterConfig##Twitter", 3, ImGuiTableFlags.PadOuterX | ImGuiTableFlags.RowBg);

        ImGui.TableSetupColumn("Enabled##Twitter", ImGuiTableColumnFlags.WidthFixed | ImGuiTableColumnFlags.NoResize | ImGuiTableColumnFlags.NoSort);
        ImGui.TableSetupColumn("Search Text##Twitter", ImGuiTableColumnFlags.WidthStretch, 100f - ImGui.GetColumnWidth(0) - tweetStatusColumWidth);
        ImGui.TableSetupColumn("Status##Twitter", ImGuiTableColumnFlags.WidthFixed, tweetStatusColumWidth);
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
            if (ImGui.Checkbox($"##TwitterTableEntryEnabled_{i}", ref entry.QueryDirty.enabled) && entry.QueryDirty.searchText != "")
                entry.IsDirty = true;

            // Search Text
            ImGui.TableNextColumn();
            ImGui.PushItemWidth(-1);  // Remove Label
            if (entry.ImageSource?.Faulted ?? false)
                ImGui.PushStyleColor(ImGuiCol.FrameBg, TableTextRed);
            if (ImGui.InputText($"##TwitterTableEntrySearchText_{i}", ref entry.QueryDirty.searchText, 450))
                entry.IsDirty = true;
            if (ImGui.IsItemClicked())
                selectedTwitterEntry = i;
            if (entry.ImageSource?.Faulted ?? false)
                ImGui.PopStyleColor();

            ImGui.PopItemWidth();

            // Status
            ImGui.TableNextColumn();
            var (text, tooltip) = TweetStatus(entry);
            var tooltipPos = ImGui.GetCursorScreenPos();
            ImGui.Text(text);
            if (!string.IsNullOrEmpty(tooltip))
            {
                var height = ImGui.GetFrameHeight();
                var end = tooltipPos + new Vector2(ImGui.GetColumnWidth(), height);
                Common.ToolTip(tooltip, tooltipPos, end);
            }

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

        static string GetHelpText() => ImGui.IsPopupOpen("Twitter Help##Twitter") ? "Hide Help" : "Show Help";

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
                    if (!entry.IsDirty)
                        continue;

                    PluginLog.LogVerbose("Changin Twitter Search text from: \"" + entry.Query.searchText + "\" to: \"" + entry.QueryDirty.searchText + "\"");

                    // Remove the old source
                    if (entry.ImageSource != null)
                        Plugin.ImageSource.RemoveSource(entry.ImageSource);
                    entry.ImageSource = null;

                    // Eemove the old query and add the new one
                    var query = entry.QueryDirty.Clone();
                    var oldIndex = Plugin.Config.Sources.Twitter.queries.FindIndex((q) => q == entry.Query);
                    if (oldIndex != -1)
                        Plugin.Config.Sources.Twitter.queries.RemoveAt(oldIndex);
                    Plugin.Config.Sources.Twitter.queries.Insert(oldIndex, query);
                    entry.Query = query;
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
                twitterHelpOpen = !twitterHelpOpen;
        }
        // Draw the Help
        if (twitterHelpOpen)
            DrawTwitterHelp();

        // Remove Button (Right align)
        if (selectedTwitterEntry >= 0)
        {
            var length = ImGui.CalcTextSize("Remove").X;
            ImGui.SameLine(ImGui.GetWindowContentRegionMax().X - ImGui.GetWindowContentRegionMin().X - length);
            if (ImGui.Button("Remove##Twitter") && selectedTwitterEntry >= 0)
            {
                Plugin.Config.Sources.Twitter.queries.RemoveAll(q => q == TwitterTableEntries[selectedTwitterEntry].Query);
                TwitterTableEntries.RemoveAt(selectedTwitterEntry);
                if (TwitterTableEntries.Count == 0)
                {
                    Twitter.Config.Query query = new();
                    Plugin.Config.Sources.Twitter.queries.Add(query);
                    TwitterTableEntries.Add(new(query, null, false));
                }
                Plugin.Config.Save();
                Plugin.UpdateImageSource();
                selectedTwitterEntry = selectedTwitterEntry < 0
                ? -1
                : selectedTwitterEntry > TwitterTableEntries.Count - 1
                ? TwitterTableEntries.Count - 1
                : selectedTwitterEntry;
                // Update ImageSource references
                foreach (var entry in TwitterTableEntries)
                {
                    entry.ImageSource ??= Plugin.ImageSource.GetAll<Twitter>().Find((s) => s.ConfigQuery.Equals(entry.Query));
                }
            }
        }
    }

    private void DrawTwitterHelp()
    {
        var fontScale = ImGui.GetIO().FontGlobalScale;
        var minSize = new Vector2(400 * fontScale, 200 * fontScale);
        ImGui.SetNextWindowSize(minSize * 2, ImGuiCond.FirstUseEver);
        ImGui.SetNextWindowSizeConstraints(minSize, minSize * 20);

        // Begin Window
        if (!ImGui.Begin("Neko Fans Twitter Help##NekoTwitter", ref twitterHelpOpen, ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoCollapse)) return;

        // Close Button
        ImGui.SetCursorPosX(ImGui.GetWindowContentRegionMax().X - 20f - ImGui.CalcTextSize("X").X);
        if (Common.IconButton(Dalamud.Interface.FontAwesomeIcon.Times, "##twitterCloseButton"))
            twitterHelpOpen = false;
        ImGui.SetCursorPosY(ImGui.GetCursorPosY() - ImGui.GetTextLineHeightWithSpacing());
        ImGui.SetCursorPosX(ImGui.GetStyle().WindowPadding.X);

        // Ignore spacing inbeween Text, TextWrapped and TextColored
        var spacing = ImGui.GetStyle().ItemSpacing;
        ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new Vector2(0f, spacing.Y));

        // How to use the Table:
        ImGui.TextColored(TwitterLight, "How to use the Table:");
        ImGui.Separator();
        ImGui.TextWrapped("The table is used to add and remove Twitter searches. The status column shows the current status of the API request and displays how many matching Tweets were found. "
                        + "Make sure to enable each row you want to use and click on the \"Save Changes\" button to save your changes. \n"
                        + "New rows can be added by clicking on the \"Add\" button. Rows can be removed by selecting them and clicking on the \"Remove\" button.\n"
                        + "If the search text is invalid, the input field will be colored red.");

        // Search Text
        ImGui.Spacing(); ImGui.Spacing();
        ImGui.TextColored(TwitterLight, "How to select the Tweets you want to see:");
        ImGui.Separator();

        // Button to Twitter Advanced Search
        ImGui.Spacing();
        if (ImGui.Button("Open Twitter Advanced Search", new Vector2(ImGui.GetWindowContentRegionMax().X - ImGui.GetStyle().WindowPadding.X, 25f * fontScale)))
            Helper.OpenInBrowser("https://twitter.com/search-advanced");

        ImGui.TextWrapped("There are 2 modes. You can either view Tweets from a specific user or all Tweets that match a query. The status column will show \"OK\" if you are viewing tweets from a specific user or the amount of tweets mathcing a query.");

        // By User  
        ImGui.Spacing();
        ImGui.TextColored(TwitterLight, "Search by Username:");
        Common.TextWithColorsWrapped(new Common.Segment[]{
            new("The last 600 Tweets from a specific user can be viewed by entering a Twitter username. If the user exists, then the status column will show the text \"OK\""),
        });
        ImGui.Spacing();
        Common.TextWithColorsWrapped(new Common.Segment[]{
            new("@username",  Dalamud.Interface.Colors.ImGuiColors.DalamudGrey),
            new(" (e.g. "),
            new("@nasa",      Dalamud.Interface.Colors.ImGuiColors.DalamudGrey),
            new(") will show you the last 600 Tweets from Nasa."),
        });

        // By Query  
        ImGui.Spacing();
        ImGui.TextColored(TwitterLight, "Search by Query:");
        Common.TextWithColorsWrapped(new Common.Segment[]{
            new("You can combine multiple search terms. Only Tweets that were posted in the last 7 days will be shown. The status column will show the amount of matching Tweets."),
        });
        ImGui.Spacing();
        Common.TextWithColorsWrapped(new Common.Segment[]{
            new("#hashtag",         Dalamud.Interface.Colors.ImGuiColors.DalamudGrey),
            new(" (e.g. "),
            new("#gposers",         Dalamud.Interface.Colors.ImGuiColors.DalamudGrey),
            new(") Matches any Tweet containing the hashtag #gposers\n"),
            new("keyword",          Dalamud.Interface.Colors.ImGuiColors.DalamudGrey),
            new(" (e.g. "),
            new("neko",             Dalamud.Interface.Colors.ImGuiColors.DalamudGrey),
            new(") Matches any Tweet that contains the word \"neko\"\n"),
            new("@username",        Dalamud.Interface.Colors.ImGuiColors.DalamudGrey),
            new(" (e.g. "),
            new("@ff_xiv_en",       Dalamud.Interface.Colors.ImGuiColors.DalamudGrey),
            new(") Matches any Tweet that mentions the user @ff_xiv_en\n"),
            new("lang:language",    Dalamud.Interface.Colors.ImGuiColors.DalamudGrey),
            new(" (e.g. "),
            new("lang:en",          Dalamud.Interface.Colors.ImGuiColors.DalamudGrey),
            new(") Matches any Tweet which is classified as English\n"),
            new("a OR b",           Dalamud.Interface.Colors.ImGuiColors.DalamudGrey),
            new(" (e.g. "),
            new("Miqo'te OR Viera", Dalamud.Interface.Colors.ImGuiColors.DalamudGrey),
            new(") Matches any Tweet containing the word \"Miqo'te\" or \"Viera\"\n"),
            new("-a",               Dalamud.Interface.Colors.ImGuiColors.DalamudGrey),
            new(" (e.g. "),
            new("-Lalafell",        Dalamud.Interface.Colors.ImGuiColors.DalamudGrey),
            new(") Matches any Tweet which doesn't contain the word \"Lalafell\""),
        });
        ImGui.Spacing();
        ImGui.Spacing();
        Common.TextWithColorsWrapped(new Common.Segment[]{
            new("Here is an example of what a query could look like:\n"),
            new("lang:en #ffxiv #gposers -#miqote -#aura -#lala -#lalafell -(#meme OR funny)", Dalamud.Interface.Colors.ImGuiColors.DalamudGrey),
        });
        ImGui.Spacing();
        Common.TextWithColorsWrapped(new Common.Segment[]{
            new("There are many more options. For more information, please visit the "),
        }); ImGui.SameLine();
        // Clickable Link
        Common.ClickLinkWrapped("Twitter API Documentation.", () => Helper.OpenInBrowser("https://developer.twitter.com/en/docs/twitter-api/tweets/search/integrate/build-a-query"));

        // Itemspacing
        ImGui.PopStyleVar();

        ImGui.End();
        ImGui.Unindent(INDENT);
    }

    private static void CheckIfNoSource()
    {
        var hasSome = Plugin.ImageSource.Count() > 0;
        var hasNoneFaulted = Plugin.ImageSource.ContainsNonFaulted();
        // If any are enabled, enable the queue again
        if (hasSome && hasNoneFaulted)
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

        if (hasSome && !hasNoneFaulted)
        {
            ImGui.TextWrapped("All image sources are currently faulted. You can disable and enable them again to restart them.");
            return;
        }

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
        var hasFaulted = false;
        if (enabled)
        {
            var all = Plugin.ImageSource.GetAll(s => source.Type.IsAssignableFrom(s.GetType()));
            hasFaulted = all.Count != 0 && all.All(s => s.Faulted);
        }

        if (hasFaulted)
        {
            ImGui.PushStyleColor(ImGuiCol.FrameBg, ConfigWindow.RedColor);
            ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(1f, 0f, 0f, 1f));
        }

        if (ImGui.Checkbox(source.Name, ref enabled))
        {
            Plugin.Config.Save();
            Plugin.UpdateImageSource();
        }

        if (hasFaulted)
            ImGui.PopStyleColor(2);

        ImGui.SameLine();
        ImGui.TextDisabled(source.Description);
        ImGui.SameLine();
        Common.HelpMarker(source.Help);
        if (ImGui.IsItemClicked())
            Helper.OpenInBrowser(source.Help);
    }
}
