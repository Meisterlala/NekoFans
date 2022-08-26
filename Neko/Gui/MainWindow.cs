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

    private bool imageGrayed;

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

        ImGui.SetNextWindowSize(size * 4, ImGuiCond.FirstUseEver);
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

        // Locking
        if (Plugin.Config.GuiMainLocked)
            flags |= ImGuiWindowFlags.NoMove;

        if (ImGui.Begin("Neko", ref visible, flags))
        {
            // Save visible State
            Visible = visible;

            // Load Neko or fallback to default
            var currentNeko = nekoTaskCurrent != null
                && nekoTaskCurrent.IsCompletedSuccessfully
                && nekoTaskCurrent.Result.ImageStatus == ImageStatus.Successfull
                ? nekoTaskCurrent.Result.Texture
                : NekoImage.DefaultNekoTexture;

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
            if (ImGui.IsMouseDragging(ImGuiMouseButton.Right)
            && (ImGui.IsWindowHovered()
                || (ImGui.IsWindowFocused()
                && ImGui.IsMouseDown(ImGuiMouseButton.Right))))
            {
                ImGui.SetWindowFocus(); // This is needed, if you drag to fast and the window cant keep up
                ImGui.SetWindowPos(ImGui.GetIO().MouseDelta + ImGui.GetWindowPos());
            }

            // Allow close with middle mouse button
            if (!Plugin.Config.GuiMainShowTitleBar
            && ImGui.IsMouseDragging(ImGuiMouseButton.Middle)
            && ImGui.IsWindowFocused())
            {
                Visible = false;
            }

            // Copy to clipboard with c
            if (Helper.KeyPressed(Dalamud.Game.ClientState.Keys.VirtualKey.C)
            && (ImGui.IsWindowFocused() || ImGui.IsWindowHovered())
            && nekoTaskCurrent != null
            && nekoTaskCurrent.IsCompletedSuccessfully)
            {
                Helper.CopyToClipboard(nekoTaskCurrent?.Result.URL ?? "");
            }

            // Open in Browser with b
            if (Helper.KeyPressed(Dalamud.Game.ClientState.Keys.VirtualKey.B)
            && (ImGui.IsWindowFocused() || ImGui.IsWindowHovered())
            && nekoTaskCurrent != null
            && nekoTaskCurrent.IsCompletedSuccessfully)
            {
                Helper.OpenInBrowser(nekoTaskCurrent?.Result.URL ?? "");
            }

            ImGui.PopStyleColor(3);
            ImGui.EndChild();
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
