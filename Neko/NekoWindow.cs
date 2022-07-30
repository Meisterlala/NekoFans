using Dalamud.Interface.Components;
using Dalamud.Logging;
using ImGuiNET;
using Lumina.Excel.GeneratedSheets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using ImGuiScene;
using System.Threading.Tasks;

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
        private TextureWrap? currentNeko;
        private Task<TextureWrap>? nekoTask;

        public NekoWindow()
        {
            try
            {
                // Load config
                // var configs = Plugin.Configuration;

                // Load neko asnync
                asnyncNextNeko();
            }
            catch (Exception ex)
            {
                PluginLog.Error(ex, "Failed to create Neko Life window");
            }
        }

        public void Draw()
        {
            try
            {
                DrawNeko();
            }
            catch
            {
            }
        }

        private void asnyncNextNeko()
        {
            if (nekoTask == null || nekoTask.IsCompleted)
            {
                // Load New Neko
                nekoTask = GetNeko.nextNeko();

                // if Task success
                nekoTask.ContinueWith((task) =>
                {
                    imageGrayed = false;
                    currentNeko = task.Result;
                }, TaskContinuationOptions.OnlyOnRanToCompletion);

                // if Task failed
                nekoTask.ContinueWith((task) =>
                {
                    imageGrayed = false;
                    currentNeko = GetNeko.defaultNeko();

                    // useless code, but the task scheduler will throw an exception if every exception
                    // is not used. task.Exception is not null
                    var whyisthisneeded = task.Exception.Flatten();
                }, TaskContinuationOptions.OnlyOnFaulted);
            }
        }

        public void DrawNeko()
        {
            if (!visible) return;

            var fontScale = ImGui.GetIO().FontGlobalScale;
            var size = new Vector2(100 * fontScale, 100 * fontScale);


            ImGui.SetNextWindowSize(size, ImGuiCond.FirstUseEver);
            ImGui.SetNextWindowSizeConstraints(size, size * 20);
            ImGui.PushStyleColor(ImGuiCol.ResizeGrip, 0);
            ImGui.SetNextWindowBgAlpha(alpha);


            if (ImGui.Begin("Neko", ref visible))
            {
                if (currentNeko != null)
                {
                    var imageRatio = (float)currentNeko.Height / currentNeko.Width;
                    var imageStart = ImGui.GetCursorScreenPos();
                    var windowSize = ImGui.GetWindowSize() - new Vector2(15f, 40f);

                    // Fix aspect ration
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
                        asnyncNextNeko();
                    }

                    ImGui.PopStyleColor(3);
                }
            }

            ImGui.PopStyleColor();
            ImGui.End();
        }
    }
}
