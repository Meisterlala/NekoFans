using System;
using System.Numerics;
using System.Threading.Tasks;
using Dalamud.Logging;
using ImGuiNET;
using ImGuiScene;

namespace Neko
{
    public class NekoWindow
    {
        private bool visible = false;
        public bool Visible
        {
            get => visible;
            set => visible = value;
        }

        private float alpha = 0.0f;
        public float Alpha
        {
            get => alpha;
            set => alpha = value;
        }

        private bool imageGrayed = false;

        private Task<NekoImage>? nekoTaskCurrent;
        private Task<NekoImage>? nekoTaskNext;

        public NekoWindow()
        {
            try
            {
                // Load config
                // var configs = Plugin.Configuration;

                // Load neko asnync
                nekoTaskCurrent = GetNeko.NextNeko();
            }
            catch (Exception ex)
            {
                PluginLog.Error(ex, "Failed to create Neko Fans window");
            }
        }

        public void Draw()
        {
            try { DrawNeko(); }
            catch { }
        }

        public void AsnyncNextNeko()
        {
            if (nekoTaskNext != null && nekoTaskNext.Status == TaskStatus.Running) return;

            if (nekoTaskNext != null)
                nekoTaskNext.Dispose();

            nekoTaskNext = GetNeko.NextNeko();

            nekoTaskNext.ContinueWith((task) =>
            {
                foreach (var ex in task.Exception?.Flatten().InnerExceptions ?? new(Array.Empty<Exception>()))
                {
                    PluginLog.LogError(ex, "Task exited with error");
                }
                if (nekoTaskCurrent != null)
                    nekoTaskCurrent.Dispose();
                nekoTaskCurrent = task;
                imageGrayed = false;
            });
        }

        public void DrawNeko()
        {
            if (!visible) return;
            if (!NekoImage.DefaultNekoReady) return;

            var fontScale = ImGui.GetIO().FontGlobalScale;
            var size = new Vector2(100 * fontScale, 100 * fontScale);

            ImGui.SetNextWindowSize(size, ImGuiCond.FirstUseEver);
            ImGui.SetNextWindowSizeConstraints(size, size * 20);
            ImGui.PushStyleColor(ImGuiCol.ResizeGrip, 0);
            ImGui.SetNextWindowBgAlpha(alpha);

            if (ImGui.Begin("Neko", ref visible))
            {
                TextureWrap? currentNeko;
                if (nekoTaskCurrent != null && nekoTaskCurrent.Status == TaskStatus.RanToCompletion)
                    currentNeko = nekoTaskCurrent.Result.Texture;
                else
                    currentNeko = NekoImage.DefaultNekoTexture;

                var imageRatio = (float)currentNeko.Height / currentNeko.Width;
                var imageStart = ImGui.GetCursorScreenPos();
                var windowSize = ImGui.GetWindowSize() - new Vector2(15f, 40f);

                // Fix aspect ratio
                Vector2 imageSize;
                if (windowSize.Y / windowSize.X > imageRatio)
                {
                    imageSize = new Vector2(windowSize.X, windowSize.X * imageRatio);
                }
                else
                {
                    imageSize = new Vector2(windowSize.Y / imageRatio, windowSize.Y);
                }

                // Center
                imageStart += (windowSize - imageSize) / 2;
                ImGui.SetCursorScreenPos(imageStart);

                // Transparancy
                ImGui.PushStyleColor(ImGuiCol.Button, Vector4.Zero);
                ImGui.PushStyleColor(ImGuiCol.ButtonActive, Vector4.Zero);
                ImGui.PushStyleColor(ImGuiCol.ButtonHovered, Vector4.Zero);

                if (ImGui.ImageButton(currentNeko.ImGuiHandle,
                    imageSize,
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
            ImGui.PopStyleColor();
            ImGui.End();
        }
    }
}