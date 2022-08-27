using System;
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

    private int QueueDonwloadCount;
    private int QueuePreloadCount;

    private readonly ImageSourcesGUI imageSourcesGUI = new();

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
            var size = new Vector2(400 * fontScale, 250 * fontScale);

            ImGui.SetNextWindowSize(size, ImGuiCond.FirstUseEver);
            ImGui.SetNextWindowSizeConstraints(size, size * 20);

            if (!ImGui.Begin("Neko Fans Configuration", ref Visible)) return;

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


    private static void DrawLook()
    {
        ImGui.PushItemWidth(-200);

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
        ImGui.SameLine(); Common.HelpMarker("Show or hide the bar on top of the image.\n" +
                                            "Hold down the right mouse button to move the window.\n" +
                                            "Press the middle mouse button to close the window when no title bar is displayed.");

        // Image Alignment Submenu
        if (ImGui.CollapsingHeader("Image alignment"))
            DrawAlign();

        // List Hotkeys
        if (ImGui.CollapsingHeader("Hotkeys"))
        {
            ImGui.TextWrapped("The following Hotkeys are only active when the main window is active.");
            (string, string, bool)[] keybind = {
                ("left mouse button", "next image", true),
                ("middle mouse botton", "close window", !Plugin.Config.GuiMainShowTitleBar),
                ("right mouse button", "move window", true),
                ("B", "open image in web browser", true),
                ("C", "copy image link to clipboard", true)};

            DrawKeybinds(keybind);
        }

        ImGui.PopItemWidth();
    }

    private void DrawAdvanced()
    {
        ImGui.PushItemWidth(-200);

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
            var span = TimeSpan.FromSeconds(Plugin.Config.SlideshowIntervalSeconds);
            var interval = span.TotalHours >= 1
                ? $"{span.Hours}h {span.Minutes}m {span.Seconds}s"
                : span.TotalMinutes >= 1
                ? $"{span.Minutes}m {span.Seconds}s"
                : $"{span.Seconds}s";

            if (ImGui.InputDouble("Interval", ref Plugin.Config.SlideshowIntervalSeconds, 1, 60, interval))
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


        // Image Queue System
        ImGui.Text("Image preloading system");
        ImGui.SameLine(); Common.HelpMarker("Images are loaded in the background, to make displaying the next image faster.");

        // Int Downloaded
        if (ImGui.InputInt("Downloaded", ref QueueDonwloadCount, 1))
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

        // Int in VRAM
        if (ImGui.InputInt("in VRAM", ref QueuePreloadCount, 1))
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

        // Clear Image queue
        if (ImGui.Button("Clear all downloaded images"))
        {
            if (Plugin.GuiMain != null)
                Plugin.GuiMain.Queue.Refresh();
        }
        ImGui.SameLine(); Common.HelpMarker("This will force all images to be downloaded again.");
        ImGui.PopItemWidth();
    }

    private static void DrawAlign()
    {
        // Center Child
        var windowWidth = ImGui.GetWindowWidth();
        var childSize = new Vector2(180, 175);
        ImGui.SetCursorPosX((windowWidth - childSize.X) / 2);

        ImGui.PushStyleColor(ImGuiCol.ChildBg, new Vector4(0.38f, 0.1f, 0.1f, 0.55f));
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
            ImGui.Text(Plugin.GuiMain?.Queue.ToString() ?? "GuiMain not loaded");
        if (ImGui.CollapsingHeader("Image Sources"))
            ImGui.Text(Plugin.ImageSource.ToString());
    }

    private static void DrawKeybinds((string, string, bool)[] keybinds)
    {
        ImGui.BeginTable("Keybinds##ConfigWindow", 3);

        ImGui.TableSetupColumn("Key", ImGuiTableColumnFlags.WidthFixed);
        ImGui.TableSetupColumn("Arrow", ImGuiTableColumnFlags.WidthFixed, 20);
        ImGui.TableSetupColumn("Description", ImGuiTableColumnFlags.WidthStretch);

        foreach (var keybind in keybinds)
        {
            // Skip if bool is false
            if (!keybind.Item3)
                continue;

            // Right Align the first column (name of key)
            ImGui.TableNextColumn();
            var posX = ImGui.GetCursorPosX() + ImGui.GetColumnWidth() - ImGui.CalcTextSize(keybind.Item1).X - ImGui.GetScrollX();
            if (posX > ImGui.GetCursorPosX())
                ImGui.SetCursorPosX(posX);
            ImGui.Text(keybind.Item1);
            // Arrow ->
            ImGui.TableNextColumn();
            Common.FontAwesomeIcon(FontAwesomeIcon.LongArrowAltRight);
            // Description
            ImGui.TableNextColumn();
            ImGui.TextWrapped(keybind.Item2);
            ImGui.TableNextRow();
        }


        ImGui.EndTable();
    }
}
