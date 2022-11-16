using System;
using System.Numerics;
using System.Threading.Tasks;
using ImGuiNET;
using Neko.Drawing;

namespace Neko.Gui;

/// <summary>
/// The Main GUI of the Plugin (/neko). It displays the image.
/// </summary>
public class MainWindow
{
    private bool visible = Plugin.Config.GuiMainVisible;

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
    public readonly Sources.Slideshow Slideshow;

    public NekoImage? ImageCurrent { get; private set; }
    public NekoImage? ImageNext { get; private set; }

    private DateTime displayTime;
    private bool imageGrayed;

    public MainWindow()
    {
        Queue = new(); // Start loading images
        Slideshow = new(() =>
        {
            // Dont load image, if the window is not visible
            if (!Visible) return;

            NextNeko();
        });
        NextNeko();
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
        if (!Embedded.ImageLoading.Ready) return;
        // Debug.Assert(nekoCurrent != null, "There is no image loaded");

        var fontScale = ImGui.GetIO().FontGlobalScale;
        var size = new Vector2(100 * fontScale, 100 * fontScale);

        ImGui.SetNextWindowSize(size * 5, ImGuiCond.FirstUseEver);
        ImGui.SetNextWindowSizeConstraints(size, size * 50);
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

        // Locking
        if (Plugin.Config.GuiMainLocked)
            flags |= ImGuiWindowFlags.NoMove;

        if (ImGui.Begin("Neko", ref visible, flags))
        {
            // Save visible State
            Visible = visible;

            // Load Neko or fallback to Error
            var displayedNeko = ImageCurrent?.CurrentState == NekoImage.State.LoadedGPU
                 ? ImageCurrent
                 : ImageCurrent?.CurrentState == NekoImage.State.Error
                 ? Embedded.ImageError
                 : Embedded.ImageLoading;

            // Get Window Size
            var windowSize = ImGui.GetWindowSize();
            if (Plugin.Config.GuiMainShowTitleBar)
                windowSize -= new Vector2(10f, 10f) + (new Vector2(0, 15f) * fontScale);
            else
                windowSize -= new Vector2(10f, 10f);

            // Align Image
            DebugHelper.Assert(displayedNeko.Width.HasValue && displayedNeko.Height.HasValue, "Image has no Width or Height");
            var (startPos, endPos) = Common.AlignImage(new Vector2(displayedNeko.Width!.Value, displayedNeko.Height!.Value), windowSize, Plugin.Config.Alignment);

            // Calculate image start position
            var cursorPos = Plugin.Config.GuiMainShowTitleBar
                ? startPos + new Vector2(5f, 5f) + (new Vector2(0f, 15f) * fontScale)
                : startPos + new Vector2(5f, 5f);

            // Transparancy
            ImGui.PushStyleColor(ImGuiCol.Button, Vector4.Zero);
            ImGui.PushStyleColor(ImGuiCol.ButtonActive, Vector4.Zero);
            ImGui.PushStyleColor(ImGuiCol.ButtonHovered, Vector4.Zero);

            // Advance timer
            var elapedTime = (DateTime.Now - displayTime).TotalMilliseconds;

            // Draw Image
            ImGui.SetCursorPos(cursorPos);
            ImGui.Image(displayedNeko.GetTexture(elapedTime).ImGuiHandle,
                endPos - startPos,
                Vector2.Zero,
                Vector2.One,
                imageGrayed ? new Vector4(.5f, .5f, .5f, 1f) : Vector4.One);

            // Invisible Button above Image
            ImGui.SetCursorPos(cursorPos);
            if (ImGui.InvisibleButton("##NekoImageMainButton", endPos - startPos)
                || Plugin.Config.Hotkeys.NextImage.IsPressed())
            {
                NextNeko();
            }

            // Show Image description
            if (ImGui.IsItemHovered()
            && !string.IsNullOrWhiteSpace(displayedNeko.Description))
            {
                Common.ToolTip(displayedNeko.Description);
            }

            // Allow move with right mouse button
            if (Plugin.Config.Hotkeys.MoveWindow.IsHeld())
            {
                ImGui.SetWindowFocus(); // This is needed, if you drag to fast and the window cant keep up
                ImGui.SetWindowPos(ImGui.GetIO().MouseDelta + ImGui.GetWindowPos());
            }

            // Allow open/close with middle mouse button
            if (Plugin.Config.Hotkeys.ToggleWindow.IsPressed())
            {
                Visible = !Visible;
                Plugin.Config.Save();
            }

            // Copy to clipboard with c
            if (Plugin.Config.Hotkeys.CopyToClipboard.IsPressed())
                Helper.CopyToClipboard(displayedNeko.URLDownloadWebsite ?? "");

            // Open in Browser with b
            if (Plugin.Config.Hotkeys.OpenInBrowser.IsPressed())
                Helper.OpenInBrowser(displayedNeko.URLOpenOnClick ?? "");

            ImGui.PopStyleColor(3);
            ImGui.EndChild();
        }
        if (!Plugin.Config.GuiMainShowResize)
            ImGui.PopStyleColor();
    }

    private void NextNeko()
    {
        // Restart the timer for the slideshow
        Slideshow.Restart();

        // Skip if there is already an image loading
        if (ImageNext != null) return;

        // Get next image from Queue
        ImageNext = Queue.Pop();

        // No image found, load error image
        if (ImageNext == null)
        {
            ImageCurrent = Embedded.ImageError;
            ImageNext = null;
            return;
        }

        // If ready to display, set as current
        if (ImageNext.CurrentState == NekoImage.State.LoadedGPU)
        {
            ImageCurrent = ImageNext;
            ImageNext = null;
            displayTime = DateTime.Now;
            return;
        }

        // If not ready, load it async
        imageGrayed = true;
        Task.Run(async () =>
        {
            await ImageNext.Await(NekoImage.State.LoadedGPU);
            ImageCurrent = ImageNext;
            imageGrayed = false;
            ImageNext = null;
        });
    }
}
