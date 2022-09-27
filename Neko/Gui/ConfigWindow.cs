using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Dalamud.Interface;
using ImGuiNET;

namespace Neko.Gui;

/// <summary>
/// The Configuration GUI (/nekocfg)
/// </summary>
public class ConfigWindow
{
    public bool Visible;

    public static readonly Vector4 RedColor = new(0.38f, 0.1f, 0.1f, 0.55f);

    private readonly ImageSourcesGUI imageSourcesGUI = new();
    private readonly HeaderImage.Total headerImage = new();

    private int QueueDonwloadCount;
    private int QueuePreloadCount;

    public ConfigWindow()
    {
        QueueDonwloadCount = Plugin.Config.QueueDownloadCount;
        QueuePreloadCount = Plugin.Config.QueuePreloadCount;
    }

    public void Draw()
    {
        if (!Visible) return;
        try
        {
            var fontScale = ImGui.GetIO().FontGlobalScale;
            var size = new Vector2(450 * fontScale, 300 * fontScale);

            ImGui.SetNextWindowSize(size * 2, ImGuiCond.FirstUseEver);
            ImGui.SetNextWindowSizeConstraints(size, size * 20);

            if (!ImGui.Begin("Neko Fans Configuration", ref Visible)) return;

            // The Tab Bar
            if (ImGui.BeginTabBar("##tabBar"))
            {
                if (ImGui.BeginTabItem("Look & Feel"))
                {
                    DrawLook();
                    ImGui.EndTabItem();
                }
                if (ImGui.BeginTabItem("Image sources"))
                {
                    imageSourcesGUI.Draw();
                    ImGui.EndTabItem();
                }
                if (ImGui.BeginTabItem("Advanced"))
                {
                    DrawAdvanced();
                    ImGui.EndTabItem();
                }
                if (Plugin.PluginInterface.IsDevMenuOpen && ImGui.BeginTabItem("Dev"))
                {
                    DrawDev();
                    ImGui.EndTabItem();
                }
                if (ImGui.TabItemButton(Plugin.GuiMain?.Visible ?? false ? "Hide Neko window" : "Show Neko window"))
                    Plugin.ToggleMainGui();
                Common.ToolTip("type /neko in the chat to view the main window");
            }
            ImGui.EndTabBar();
        }
        finally
        {
            ImGui.End();
        }
    }

    private void DrawLook()
    {
        // Draw Header
        if (Plugin.Config.ShowHeaders)
        {
            headerImage.DrawFullWidth();
            var text = "The total amount of images displayed by all Neko Fans users.";
            if (!Plugin.Config.EnableTelemetry)
                text += "\nYou are not included in the total count because you disabled \"Contribute to public image count\".";
            Common.ToolTip(text);
        }

        ImGui.PushItemWidth(-200 * ImGui.GetIO().FontGlobalScale);

        // Background opacity slider
        if (ImGui.SliderFloat("Background opacity", ref Plugin.Config.GuiMainOpacity, 0, 1))
            Plugin.Config.Save();
        ImGui.SameLine(); Common.HelpMarker("CTRL+click to input value.");

        // Allow resizing
        if (ImGui.Checkbox("Allow resizing", ref Plugin.Config.GuiMainAllowResize))
            Plugin.Config.Save();
        ImGui.SameLine(); Common.HelpMarker("Show arrows near the edges of the window to resize it.");

        if (Plugin.Config.GuiMainAllowResize)
        {
            // Show resize
            if (ImGui.Checkbox("Show resize handle", ref Plugin.Config.GuiMainShowResize))
                Plugin.Config.Save();
            ImGui.SameLine(); Common.HelpMarker("Show or hide the grey triangle in the bottom right corner of the window.");
        }

        // Lock Window
        if (ImGui.Checkbox("Lock position", ref Plugin.Config.GuiMainLocked))
            Plugin.Config.Save();
        ImGui.SameLine(); Common.HelpMarker("Lock the position of the window, not allowing it to be moved.\nYou can always move the window by holdng down the right mouse button and dragging.");

        // Show Title Bar
        if (ImGui.Checkbox("Show window title bar", ref Plugin.Config.GuiMainShowTitleBar))
            Plugin.Config.Save();
        ImGui.SameLine(); Common.HelpMarker("Show or hide the bar on top of the image");

        ImGui.Separator();

        // Show / Hide Header
        if (ImGui.Checkbox("Show header image", ref Plugin.Config.ShowHeaders))
            Plugin.Config.Save();
        ImGui.SameLine(); Common.HelpMarker("Show or hide the image at the top of the window. It shows the total amount of images downloaded by all Neko Fans users.\n" +
                                            "The image in the 'Image sources' Tab shows the amount of images you downloaded.");

        ImGui.Separator();

        // Slideshow Enable / Disable
        if (ImGui.Checkbox("Slideshow", ref Plugin.Config.SlideshowEnabled))
        {
            Plugin.Config.Save();
            Plugin.GuiMain?.Slideshow.UpdateFromConfig();
        }
        ImGui.SameLine(); Common.HelpMarker("Automatically display a new image after the specified interval.");

        // Slideshow Interval
        if (Plugin.Config.SlideshowEnabled)
        {
            if (ImGui.InputDouble("Interval", ref Plugin.Config.SlideshowIntervalSeconds, 1, 60, Helper.SecondsToString(Plugin.Config.SlideshowIntervalSeconds)))
            {
                // Check for miminimum interval
                if (Plugin.Config.SlideshowIntervalSeconds < Sources.Slideshow.MININTERVAL)
                    Plugin.Config.SlideshowIntervalSeconds = Sources.Slideshow.MININTERVAL;
                Plugin.Config.Save();
                Plugin.GuiMain?.Slideshow.UpdateFromConfig();
            }
            Common.ToolTip("Input the interval length in seconds.\nHolding Control while pressing the + or - button changes the inverval by 1 minute.");
            ImGui.SameLine(); Common.HelpMarker("How long to wait before displaying a new image.");
        }

        ImGui.Separator();

        // Image Alignment Submenu
        if (ImGui.CollapsingHeader("Image alignment"))
            DrawAlign();

        // List Hotkeys
        if (ImGui.CollapsingHeader("Hotkeys"))
        {
            var keybinds = new List<(Hotkey, string)>() {
                (Plugin.Config.Hotkeys.NextImage, "Show the next image. You can always click on the image to show the next one."),
                (Plugin.Config.Hotkeys.ToggleWindow, "Open or close the Neko window."),
                (Plugin.Config.Hotkeys.MoveWindow,"Move the Neko window."),
                (Plugin.Config.Hotkeys.OpenInBrowser, "Open the current image in your default browser."),
                (Plugin.Config.Hotkeys.CopyToClipboard, "Copy the link of the current image to the clipboard") };

            DrawKeybinds(keybinds);
        }

        ImGui.PopItemWidth();
    }

    private void DrawAdvanced()
    {
        ImGui.PushItemWidth(-200);

        ImGui.PushItemWidth(150 * ImGui.GetIO().FontGlobalScale);
        // Image Queue System
        ImGui.Text("Image preloading system");
        ImGui.SameLine(); Common.HelpMarker("Images are loaded in the background, to make displaying the next image faster.");

        // Int Downloaded
        if (ImGui.InputInt("Downloaded##Advanced", ref QueueDonwloadCount, 1))
        {
            if (QueueDonwloadCount < 1 || QueueDonwloadCount > 50 || QueuePreloadCount > QueueDonwloadCount)
                QueueDonwloadCount = Plugin.Config.QueueDownloadCount;
            Plugin.Config.QueueDownloadCount = QueueDonwloadCount;
            Plugin.Config.Save();
            if (Plugin.GuiMain != null)
                Plugin.GuiMain.Queue.UpdateQueueLength();
        }
        ImGui.SameLine(); Common.HelpMarker("The amount of images which are downloaded from the internet.\n" +
                                            "Increasing this will result in higher RAM usage. Recomended: 5");
        if (Plugin.GuiMain != null)
        {
            ImGui.SameLine(); ImGui.TextDisabled(Helper.SizeSuffix(Plugin.GuiMain.Queue.RAMUsage()));
        }

        // Int in VRAM
        if (ImGui.InputInt("in VRAM##Advanced", ref QueuePreloadCount, 1))
        {
            if (QueuePreloadCount < 1 || QueuePreloadCount > 25 || QueuePreloadCount > QueueDonwloadCount)
                QueuePreloadCount = Plugin.Config.QueuePreloadCount;
            Plugin.Config.QueuePreloadCount = QueuePreloadCount;
            Plugin.Config.Save();
            if (Plugin.GuiMain != null)
                Plugin.GuiMain.Queue.UpdateQueueLength();
        }
        ImGui.SameLine(); Common.HelpMarker("The amount of images which are decoded and loaded into the GPU.\n" +
                                            "Increasing this will result in higher VRAM usage. Recomended: 2");
        if (Plugin.GuiMain != null)
        {
            ImGui.SameLine(); ImGui.TextDisabled(Helper.SizeSuffix(Plugin.GuiMain.Queue.VRAMUsage()));
        }
        ImGui.PopItemWidth();

        ImGui.Separator();

        // Telemetry
        if (ImGui.Checkbox("Contribute to public image count", ref Plugin.Config.EnableTelemetry))
            Plugin.Config.Save();
        ImGui.SameLine(); Common.HelpMarker("Contribute to the public image count by sending the amount of images you downloaded to the Neko Fans server.\n" +
                                            "The Image Source name and the downloaded image count will be sent.\n" +
                                            "The Twitter seach text will not be sent. And no personal information will be sent.");

        ImGui.Separator();

        // Clear Image queue
        if (ImGui.Button("Clear all downloaded images##Advanced"))
        {
            if (Plugin.GuiMain != null)
                Plugin.GuiMain.Queue.Refresh();
        }
        ImGui.SameLine(); Common.HelpMarker("This will force all images to be downloaded again.");
        ImGui.PopItemWidth();

        // Clear Image queue
        if (ImGui.Button("Rest all faulty Image Sources##Advanced"))
            Plugin.ImageSource.ResetFaultySources();
        ImGui.SameLine(); Common.HelpMarker("If an API has a problem, it will be disabled. It the name of the API is red, it is disabled.\n" +
                                            "Clicking this Button will reset all disabled APIs.\n You can also disable and enable APIs in the 'Image sources' menu to reset them.");

        // Reload from Config
        if (ImGui.Button("Reload Image Sources from config##Advanced"))
            Plugin.ReloadSources();
        ImGui.SameLine(); Common.HelpMarker("This will reload all Image Sources from the state saved in the configuration file.");

        ImGui.PopItemWidth();
    }

    private static void DrawAlign()
    {
        // Center Child
        var windowWidth = ImGui.GetWindowWidth();
        var childSize = new Vector2(180, 175);
        ImGui.SetCursorPosX((windowWidth - childSize.X) / 2);

        ImGui.PushStyleColor(ImGuiCol.ChildBg, RedColor);
        ImGui.BeginChild("Align", childSize, true);
        ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, 0);

        var buttonSize = new Vector2(50, 50);
        string[] names = {
                "Top\nLeft",
                "Top",
                "Top\nRight",
                "Left",
                "Center",
                "Right",
                "Bottom\nLeft",
                "Bottom",
                "Bottom\nRight" };
        Configuration.ImageAlignment[] alignmentents = {
                Configuration.ImageAlignment.TopLeft,
                Configuration.ImageAlignment.Top,
                Configuration.ImageAlignment.TopRight,
                Configuration.ImageAlignment.Left,
                Configuration.ImageAlignment.Center,
                Configuration.ImageAlignment.Right,
                Configuration.ImageAlignment.BottomLeft,
                Configuration.ImageAlignment.Bottom,
                Configuration.ImageAlignment.BottomRight };

        for (var y = 0; y < 3; y++)
        {
            for (var x = 0; x < 3; x++)
            {
                var alignment = new Vector2(x / 2f, y / 2f);
                var isSelected = Plugin.Config.Alignment == alignmentents[(y * 3) + x];

                if (x > 0)
                    ImGui.SameLine();
                ImGui.PushStyleVar(ImGuiStyleVar.SelectableTextAlign, alignment);
                if (ImGui.Selectable(names[(y * 3) + x], isSelected, ImGuiSelectableFlags.None, buttonSize))
                {
                    Plugin.Config.Alignment = alignmentents[(y * 3) + x];
                    Plugin.Config.Save();
                }
                ImGui.PopStyleVar();
            }
        }
        ImGui.EndChild();
        ImGui.PopStyleColor();
    }

    private static void DrawDev()
    {
        if (ImGui.CollapsingHeader("Image Queue"))
            ImGui.TextWrapped(Plugin.GuiMain?.Queue.ToString() ?? "GuiMain not loaded");
        if (ImGui.CollapsingHeader("Image Sources"))
            ImGui.TextWrapped(Plugin.ImageSource.ToString());
        if (ImGui.CollapsingHeader("Slideshow Status"))
            ImGui.TextWrapped(Plugin.GuiMain?.Slideshow.ToString() ?? "GuiMain not loaded");
        if (ImGui.CollapsingHeader("Plugin Config"))
            ImGui.TextWrapped(Plugin.Config.ToString());
    }

    private static Key[]? Keys;
    private static string[]? KeyNames;
    private static float? KeyLongestName;
    private static HotkeyCondition[]? Conditions;
    private static string[]? ConditionNames;
    private static float? ConditionLongestName;

    private static void DrawKeybinds(List<(Hotkey, string)> keybinds)
    {
        // Set static fields
        Keys ??= Hotkey.KeyNames.Keys.ToArray();
        KeyNames ??= Keys.Select(x => Hotkey.GetKeyName(x)).ToArray();
        KeyLongestName ??= KeyNames.Max(x => ImGui.CalcTextSize(x).X);
        Conditions ??= Hotkey.ConditionNames.Keys.ToArray();
        ConditionNames ??= Conditions.Select(x => Hotkey.ConditionNames[x]).ToArray();
        ConditionLongestName ??= ConditionNames.Max(x => ImGui.CalcTextSize(x).X);

        ImGui.BeginTable("Keybinds##ConfigWindow", 4, ImGuiTableFlags.RowBg | ImGuiTableFlags.SizingFixedFit);

        var ConditionColumnwidth = (ConditionLongestName.Value * ImGui.GetIO().FontGlobalScale * 1.5f) - 10;
        ImGui.TableSetupColumn("Condition##ConfigWindow", ImGuiTableColumnFlags.WidthFixed, ConditionColumnwidth);
        var KeyColumnwidth = (KeyLongestName.Value * ImGui.GetIO().FontGlobalScale * 1.5f) - 35;
        ImGui.TableSetupColumn("Key##ConfigWindow", ImGuiTableColumnFlags.WidthFixed, KeyColumnwidth);
        ImGui.TableSetupColumn("Action##ConfigWindowu", ImGuiTableColumnFlags.WidthStretch);
        ImGui.TableHeadersRow();

        for (var i = 0; i < keybinds.Count; i++)
        {
            var (hotkey, description) = keybinds[i];

            ImGui.TableNextRow();
            // Condition combo box
            ImGui.TableNextColumn();
            ImGui.SetNextItemWidth(ConditionColumnwidth);
            if (ImGui.BeginCombo($"##Keybinds_Condition_{i}", hotkey.ConditionName))
            {
                for (var j = 0; j < ConditionNames.Length; j++)
                {
                    var name = ConditionNames[j];
                    if (ImGui.Selectable($"{name}##Keybinds_Condition_{i}_{j}", Conditions[j] == hotkey.Condition))
                    {
                        hotkey.Condition = Conditions[j];
                        Dalamud.Logging.PluginLog.Log($"Changed keybind {i} condition to {name}");
                        Plugin.Config.Save();
                    }
                }
                ImGui.EndCombo();
            }

            // Key combo box
            ImGui.TableNextColumn();
            ImGui.SetNextItemWidth(KeyColumnwidth);
            if (ImGui.BeginCombo($"##Keybinds_Combo_{i}", Hotkey.GetKeyName(hotkey.Key)))
            {
                for (var j = 0; j < KeyNames.Length; j++)
                {
                    var name = KeyNames[j];
                    if (ImGui.Selectable($"{name}##Keybinds_Combo_{i}_{j}", hotkey.Key == Keys[j]))
                    {
                        hotkey.Key = Keys[j];
                        Plugin.Config.Save();
                    }
                }
                ImGui.EndCombo();
            }

            // Description
            ImGui.TableNextColumn();
            Common.FontAwesomeIcon(FontAwesomeIcon.LongArrowAltRight); ImGui.SameLine();
            ImGui.Text(hotkey.Name); ImGui.SameLine();
            Common.HelpMarker(description);
        }
        ImGui.EndTable();

        // Check for duplicate keybinds
        var containsDuplicate = false;
        foreach (var (key, desc) in keybinds)
        {
            if (keybinds.Count(x => x.Item1.Key == key.Key) > 1)
            {
                containsDuplicate = true;
                break;
            }
        }
        if (containsDuplicate)
        {
            ImGui.TextColored(new Vector4(1, 0, 0, 1), "WARNING: Duplicate keys detected!");
            Common.ToolTip("You have multiple actions set to the same key. This will cause issues.");
        }
    }
}
