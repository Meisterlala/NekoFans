using System;
using System.Numerics;
using System.Threading.Tasks;
using Dalamud.Logging;
using ImGuiNET;
using ImGuiScene;

namespace Neko.Gui
{
    public class ConfigWindow
    {
        private bool visible = false;
        public bool Visible
        {
            get => visible;
            set => visible = value;
        }
        private int QueueDonwloadCount;
        private int QueuePreloadCount;

        public ConfigWindow()
        {
            QueueDonwloadCount = Plugin.Config.QueueDonwloadCount;
            QueuePreloadCount = Plugin.Config.QueuePreloadCount;
        }

        public void Draw()
        {
            if (!visible) return;
            try
            {
                var fontScale = ImGui.GetIO().FontGlobalScale;
                var size = new Vector2(400 * fontScale, 250 * fontScale);

                ImGui.SetNextWindowSize(size, ImGuiCond.FirstUseEver);
                ImGui.SetNextWindowSizeConstraints(size, size * 20);

                if (!ImGui.Begin("Neko Fans Configuration", ref visible)) return;

                if (ImGui.BeginTabBar("##tabBar"))
                {
                    if (ImGui.BeginTabItem("Look & Feel"))
                    {
                        DrawLook();
                        ImGui.EndTabItem();
                    }
                    if (ImGui.BeginTabItem("Image sources"))
                    {
                        ImGui.EndTabItem();
                    }
                    if (ImGui.BeginTabItem("Advanced"))
                    {
                        DrawAdvanced();
                        ImGui.EndTabItem();
                    }
#if DEBUG
                    if (ImGui.BeginTabItem("Dev"))
                    {
                        DrawDev();
                        ImGui.EndTabItem();
                    }
#endif
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

            // Show Title Bar
            if (ImGui.Checkbox("Show window title bar", ref Plugin.Config.GuiMainShowTitleBar))
                Plugin.Config.Save();
            ImGui.SameLine(); Common.HelpMarker("Show or hide the bar on top of the image.\nHold down the right mouse button to move the window when no title bar is displayed.");

            // Image Alignment Submenu
            if (ImGui.CollapsingHeader("Image alignment"))
                DrawAlign();

            ImGui.PopItemWidth();
        }

        private void DrawAdvanced()
        {
            ImGui.PushItemWidth(-200);

            // Image Queue System
            ImGui.Text("Image preloading system");
            ImGui.SameLine(); Common.HelpMarker("Images are loaded in the background, to make displaying the next image faster.");

            // Int Downloaded
            if (ImGui.InputInt("Downloaded", ref QueueDonwloadCount, 1))
            {
                if (QueueDonwloadCount < 1 || QueueDonwloadCount > 50 || QueuePreloadCount > QueueDonwloadCount)
                    QueueDonwloadCount = Plugin.Config.QueueDonwloadCount;
                Plugin.Config.QueueDonwloadCount = QueueDonwloadCount;
                Plugin.Config.Save();
                Plugin.GuiMain.queue.UpdateQueueLength();
            }
            ImGui.SameLine(); Common.HelpMarker("The amount of images which are downloaded from the internet.\nIncreasing this will result in higher RAM usage. Recomended: 5");

            // Int in VRAM
            if (ImGui.InputInt("in VRAM", ref QueuePreloadCount, 1))
            {
                if (QueuePreloadCount < 1 || QueuePreloadCount > 25 || QueuePreloadCount > QueueDonwloadCount)
                    QueuePreloadCount = Plugin.Config.QueuePreloadCount;
                Plugin.Config.QueuePreloadCount = QueuePreloadCount;
                Plugin.Config.Save();
                Plugin.GuiMain.queue.UpdateQueueLength();
            }
            ImGui.SameLine(); Common.HelpMarker("The amount of images which are decoded and loaded into the GPU.\nIncreasing this will result in higher VRAM usage. Recomended: 2");
            ImGui.PopItemWidth();
        }

        private void DrawAlign()
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

            for (int y = 0; y < 3; y++)
            {
                for (int x = 0; x < 3; x++)
                {
                    var alignment = new Vector2(x / 2f, y / 2f);
                    var isSelected = Plugin.Config.Alignment == alignmentents[y * 3 + x];

                    if (x > 0)
                        ImGui.SameLine();
                    ImGui.PushStyleVar(ImGuiStyleVar.SelectableTextAlign, alignment);
                    if (ImGui.Selectable(names[y * 3 + x], isSelected, ImGuiSelectableFlags.None, buttonSize))
                    {
                        Plugin.Config.Alignment = alignmentents[y * 3 + x];
                        Plugin.Config.Save();
                    }
                    ImGui.PopStyleVar();
                }
            }

            ImGui.EndChild();
            ImGui.PopStyleColor();
        }


        private void DrawDev()
        {
            if (ImGui.CollapsingHeader("Image Queue"))
                ImGui.Text(Plugin.GuiMain.queue.ToString());

        }
    }
}