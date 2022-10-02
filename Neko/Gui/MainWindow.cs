using System.Numerics;
using System.Threading.Tasks;
using ImGuiNET;

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

    private bool imageGrayed;

    private Task<NekoImage>? nekoTaskCurrent;
    private Task<NekoImage>? nekoTaskNext;

    public MainWindow()
    {
        Queue = new(); // Start loading images
        Slideshow = new(() =>
        {
            // Dont load image, if the window is not visible
            if (!Visible) return;

            AsnyncNextNeko();
        });
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
        if (!NekoImage.Embedded.ImageLoading.Ready) return;

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
            var currentNeko = nekoTaskCurrent?.IsCompletedSuccessfully == true
                && nekoTaskCurrent.Result.ImageStatus == ImageStatus.Successfull
                 ? nekoTaskCurrent.Result.Texture
                 : nekoTaskCurrent != null
                && (nekoTaskCurrent.IsFaulted || nekoTaskCurrent.IsCanceled)
                 ? NekoImage.Embedded.ImageError.Texture
                 : NekoImage.Embedded.ImageLoading.Texture;

            // Get Window Size
            var windowSize = ImGui.GetWindowSize();
            if (Plugin.Config.GuiMainShowTitleBar)
                windowSize -= new Vector2(10f, 10f) + (new Vector2(0, 15f) * fontScale);
            else
                windowSize -= new Vector2(10f, 10f);

            // Align Image
            var (startPos, endPos) = Common.AlignImage(new Vector2(currentNeko.Width, currentNeko.Height), windowSize, Plugin.Config.Alignment);

            // Set image start position
            if (Plugin.Config.GuiMainShowTitleBar)
                ImGui.SetCursorPos(startPos + new Vector2(5f, 5f) + (new Vector2(0f, 15f) * fontScale));
            else
                ImGui.SetCursorPos(startPos + new Vector2(5f, 5f));

            // Transparancy
            ImGui.PushStyleColor(ImGuiCol.Button, Vector4.Zero);
            ImGui.PushStyleColor(ImGuiCol.ButtonActive, Vector4.Zero);
            ImGui.PushStyleColor(ImGuiCol.ButtonHovered, Vector4.Zero);

            // Show Next image
            void advanceImage()
            {
                imageGrayed = true;
                AsnyncNextNeko();
            }

            // Draw Image
            if (ImGui.ImageButton(currentNeko.ImGuiHandle,
                endPos - startPos,
                Vector2.Zero,
                Vector2.One,
                0,
                Vector4.Zero,
                imageGrayed ? new Vector4(.5f, .5f, .5f, 1f) : Vector4.One)
                || Plugin.Config.Hotkeys.NextImage.IsPressed()
                )
            {
                advanceImage();
            }

            // Show Image description
            if (ImGui.IsItemHovered()
            && nekoTaskCurrent?.IsCompletedSuccessfully == true
            && !string.IsNullOrWhiteSpace(nekoTaskCurrent.Result.Description))
            {
                Common.ToolTip(nekoTaskCurrent.Result.Description);
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
            if (Plugin.Config.Hotkeys.CopyToClipboard.IsPressed()
            && nekoTaskCurrent?.IsCompletedSuccessfully == true)
            {
                Helper.CopyToClipboard(nekoTaskCurrent?.Result.URLDownloadWebsite ?? "");
            }

            // Open in Browser with b
            if (Plugin.Config.Hotkeys.OpenInBrowser.IsPressed()
            && nekoTaskCurrent?.IsCompletedSuccessfully == true)
            {
                Helper.OpenInBrowser(nekoTaskCurrent?.Result.URLOpenOnClick ?? "");
            }

            ImGui.PopStyleColor(3);
            ImGui.EndChild();
        }
        if (!Plugin.Config.GuiMainShowResize)
            ImGui.PopStyleColor();
    }

    private void AsnyncNextNeko()
    {
        // Restart the timer for the slideshow
        Slideshow.Restart();

        // Dont load next neko if the current one is loading
        if (nekoTaskNext?.IsCompleted == false) return;
        if (nekoTaskNext?.IsCompleted == true) nekoTaskNext.Dispose();

        // Get next image from Queue
        nekoTaskNext = Queue.Pop();

        var processResult = (Task<NekoImage> task) =>
        {
            var _ = task.Exception?.Flatten();  // This is done to prevent System.AggregateException
            nekoTaskCurrent?.Dispose();
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
