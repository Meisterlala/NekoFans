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

        public ConfigWindow()
        {

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

                if (!ImGui.Begin("Neko Fans config", ref visible)) return;

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
            ImGui.Text("TestText");

            // Background opacity slider
            if (ImGui.SliderFloat("Background opacity", ref Plugin.Config.GuiMainOpacity, 0, 1))
                Plugin.Config.Save();
            ImGui.SameLine(); Common.HelpMarker("CTRL+click to input value.");

            // Show resize
            if (ImGui.Checkbox("Show resize handle", ref Plugin.Config.GuiMainShowResize))
                Plugin.Config.Save();
            ImGui.SameLine(); Common.HelpMarker("Show or hide the grey triangle in the bottom right corner of the window");

            // Image Alignment Submenu
            if (ImGui.CollapsingHeader("Image alignment"))
                DrawAlign();
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