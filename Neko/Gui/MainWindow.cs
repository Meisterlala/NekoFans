using System;
using System.Numerics;
using System.Threading.Tasks;
using Dalamud.Logging;
using ImGuiNET;
using ImGuiScene;

namespace Neko.Gui;

/// <summary>
/// The Main GUI of the Plugin (/neko). It displays the image.
/// </summary>
public class MainWindow
{
    private bool visible = false;

    public bool Visible
    {
        get => visible;
        set
        {
            if (Plugin.Config.GuiMainVisible != value)
            {
                Plugin.Config.GuiMainVisible = value;
                Plugin.Config.Save();
            }
            visible = value;
        }
    }

    public readonly NekoQueue Queue;

    private bool imageGrayed = false;

    private Task<NekoImage>? nekoTaskCurrent;
    private Task<NekoImage>? nekoTaskNext;

    public MainWindow()
    {
        Queue = new(); // Start loading images
        AsnyncNextNeko();
    }

    public void Draw()
    {
        if (!Visible) return;
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

        // Remove resize triangle
        if (!Plugin.Config.GuiMainShowResize)
            ImGui.PushStyleColor(ImGuiCol.ResizeGrip, 0);

        // Remove Title Bar
        if (!Plugin.Config.GuiMainShowTitleBar)
            flags |= ImGuiWindowFlags.NoTitleBar;

        // No Resize
        if (!Plugin.Config.GuiMainAllowResize)
            flags |= ImGuiWindowFlags.NoResize;

        if (ImGui.Begin("Neko", ref visible, flags))
        {
            // Load Neko or fallback to default
            TextureWrap? currentNeko;
            if (nekoTaskCurrent != null
                && nekoTaskCurrent.IsCompletedSuccessfully
                && nekoTaskCurrent.Result.ImageStatus == ImageStatus.Successfull)
                currentNeko = nekoTaskCurrent.Result.Texture;
            else
                currentNeko = NekoImage.DefaultNekoTexture;

            // Get Window Size
            var windowSize = ImGui.GetWindowSize();
            if (Plugin.Config.GuiMainShowTitleBar)
                windowSize -= new Vector2(10f, 27f);
            else
                windowSize -= new Vector2(10f, 10f);

            // Align Image
            var (startPos, endPos) = Common.AlignImage(new Vector2(currentNeko.Height, currentNeko.Width), windowSize, Plugin.Config.Alignment);

            // Set image start position
            if (Plugin.Config.GuiMainShowTitleBar)
                ImGui.SetCursorPos(startPos + new Vector2(5f, 23f));
            else
                ImGui.SetCursorPos(startPos + new Vector2(5f, 5f));

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

            // Allow move with right mouse button
            if (ImGui.IsMouseDragging(ImGuiMouseButton.Right))
                ImGui.SetWindowPos(ImGui.GetIO().MouseDelta + ImGui.GetWindowPos());

            // Allow close with middle mouse button
            if (!Plugin.Config.GuiMainShowTitleBar && ImGui.IsMouseClicked(ImGuiMouseButton.Middle))
                Visible = false;

            ImGui.PopStyleColor(3);
            ImGui.EndChild();
            //  ImGui.PopStyleColor();
        }
        if (!Plugin.Config.GuiMainShowResize)
            ImGui.PopStyleColor();
    }


    private void AsnyncNextNeko()
    {
        if (nekoTaskNext != null && !nekoTaskNext.IsCompleted) return;
        if (nekoTaskNext != null && nekoTaskNext.IsCompleted) nekoTaskNext.Dispose();

        // Get next image from Queue
        nekoTaskNext = Queue.Pop();

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
