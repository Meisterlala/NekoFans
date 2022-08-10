using System;
using System.Numerics;
using System.Threading.Tasks;
using Dalamud.Logging;
using ImGuiNET;
using ImGuiScene;

namespace Neko.Gui
{
    public class NekoWindow
    {
        private bool visible = false;
        public bool Visible
        {
            get => visible;
            set => visible = value;
        }

        private bool imageGrayed = false;

        private Task<NekoImage>? nekoTaskCurrent;
        private Task<NekoImage>? nekoTaskNext;
        public readonly NekoQueue queue;

        public NekoWindow()
        {
            // Load config
            // var configs = Plugin.Configuration;
            queue = new(); // Start loading images
            AsnyncNextNeko();
        }

        public void Draw()
        {
            if (!visible) return;
            try
            {
                DrawNeko();
            }
            finally
            {
                ImGui.End();
            }
        }


        public void DrawNeko()
        {
            if (!NekoImage.DefaultNekoReady) return;

            var fontScale = ImGui.GetIO().FontGlobalScale;
            var size = new Vector2(100 * fontScale, 100 * fontScale);

            ImGui.SetNextWindowSize(size, ImGuiCond.FirstUseEver);
            ImGui.SetNextWindowSizeConstraints(size, size * 20);
            ImGui.SetNextWindowBgAlpha(Plugin.Config.GuiMainOpacity);
            var flags = ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse;
            if (!Plugin.Config.GuiMainShowResize)
                ImGui.PushStyleColor(ImGuiCol.ResizeGrip, 0);

            if (ImGui.Begin("Neko", ref visible, flags))
            {
                TextureWrap? currentNeko;
                if (nekoTaskCurrent != null
                    && nekoTaskCurrent.IsCompleted
                    && nekoTaskCurrent.Result.ImageStatus == ImageStatus.Successfull)
                    currentNeko = nekoTaskCurrent.Result.Texture;
                else
                    currentNeko = NekoImage.DefaultNekoTexture;

                // Align Image
                var windowSize = ImGui.GetWindowSize() - new Vector2(10f, 27f);
                var (startPos, endPos) = Common.AlignImage(new Vector2(currentNeko.Height, currentNeko.Width), windowSize, Plugin.Config.Alignment);
                ImGui.SetCursorPos(startPos + new Vector2(5f, 23f));

                // Transparancy
                ImGui.PushStyleColor(ImGuiCol.Button, Vector4.Zero);
                ImGui.PushStyleColor(ImGuiCol.ButtonActive, Vector4.Zero);
                ImGui.PushStyleColor(ImGuiCol.ButtonHovered, Vector4.Zero);

                if (ImGui.ImageButton(currentNeko.ImGuiHandle,
                    endPos - startPos,
                    Vector2.Zero,
                    Vector2.One,
                    0,
                    Vector4.Zero,
                    imageGrayed ? new Vector4(.5f, .5f, .5f, 1f) : Vector4.One))
                {
                    imageGrayed = true;
                    // Load next Neko if Image is pressed
                    AsnyncNextNeko();
                }

                ImGui.PopStyleColor(3);
            }
            if (!Plugin.Config.GuiMainShowResize)
                ImGui.PopStyleColor();
        }


        private void AsnyncNextNeko()
        {
            if (nekoTaskNext != null && !nekoTaskNext.IsCompleted) return;
            if (nekoTaskNext != null && nekoTaskNext.IsCompleted) nekoTaskNext.Dispose();

            // Get next image from Queue
            nekoTaskNext = queue.Pop();

            var processResult = (Task<NekoImage> task) =>
            {
                var _ = task.Exception?.Flatten();  // This is done to prevent System.AggregateException
                if (nekoTaskCurrent != null)
                    nekoTaskCurrent.Dispose();
                nekoTaskCurrent = task;
                imageGrayed = false;
            };

            // Update nekoTaskCurrent now
            if (nekoTaskNext.IsCompleted)
                processResult(nekoTaskNext);
            else // Update later
                nekoTaskNext.ContinueWith(processResult);
        }

    }
}