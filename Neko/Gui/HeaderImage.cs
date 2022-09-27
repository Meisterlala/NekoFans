using System;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;
using Dalamud.Logging;
using ImGuiNET;
using Neko.Sources;

namespace Neko.Gui;

public abstract class HeaderImage
{
    protected abstract TimeSpan RetryTimer { get; }
    protected abstract TimeSpan UpdateTimer { get; }

    private readonly CancellationTokenSource cts = new();

    private DateTime lastUpdate = DateTime.MinValue;
    private DateTime lastFaulted = DateTime.MaxValue;

    private int error_count;
    private bool isUpdating;
    private Task<NekoImage>? updateTask;
    private NekoImage? image;

    protected abstract Task<NekoImage> DownloadHeader();

    ~HeaderImage()
    {
        cts.Cancel();
    }

    public void Draw(Vector2 size)
    {
        if (NotReady())
            return;

        // Transparancy
        ImGui.PushStyleColor(ImGuiCol.Button, Vector4.Zero);
        ImGui.PushStyleColor(ImGuiCol.ButtonActive, Vector4.Zero);
        ImGui.PushStyleColor(ImGuiCol.ButtonHovered, Vector4.Zero);

        ImGui.ImageButton(image!.Texture.ImGuiHandle,
            size,
            Vector2.Zero,
            Vector2.One,
            0,
            Vector4.Zero,
            Vector4.One);

        ImGui.PopStyleColor(3);

        // Update image
        UpdateHeader();
    }

    public void Draw((Vector2, Vector2) region)
    {
        // Display nothing if there are errors
        if (error_count >= 5)
            return;

        // Wait for the task to finish
        if (image == null)
        {
            WaitForImage();
            return;
        }

        ImGui.SetCursorPos(region.Item1);
        Draw(region.Item2 - region.Item1);
    }

    public Vector2? TryGetSize()
    {
        return NotReady()
            ? null
            : new Vector2(image!.Texture.Width, image!.Texture.Height);
    }

    public void DrawFullWidth()
    {
        if (NotReady())
            return;

        var width = ImGui.GetWindowSize().X - ImGui.GetWindowContentRegionMin().X - (ImGui.GetStyle().WindowPadding.X * 2);
        Draw(new Vector2(width, width / image!.Texture.Width * image!.Texture.Height));
    }

    private bool NotReady()
    {
        if (error_count >= 5)
            return true;

        if (image == null)
        {
            WaitForImage();
            return true;
        }
        return false;
    }

    protected virtual void UpdateHeader()
    {
        if (isUpdating || DateTime.Now - lastUpdate < UpdateTimer || DateTime.Now - lastFaulted > RetryTimer)
            return;

        if (updateTask?.IsCompleted == false)
            return;

        isUpdating = true;
        updateTask = DownloadHeader();
        updateTask.ContinueWith(OnTaskComplete);
    }

    private void WaitForImage()
    {
        // Start Image Task
        if (updateTask == null)
        {
            updateTask = DownloadHeader();
            updateTask.ContinueWith(OnTaskComplete);
        }

        // Wait for Image Task
        if (!updateTask.IsCompleted)
            return;

        // Restart if Faulted
        if (updateTask.IsFaulted && DateTime.Now - lastFaulted > RetryTimer)
        {
            lastFaulted = DateTime.Now;
            updateTask = null;
        }
    }

    private void OnTaskComplete(Task<NekoImage> task)
    {
        if (task.IsFaulted || task.IsCanceled)
        {
            foreach (var ex in task.Exception?.Flatten().InnerExceptions ?? new(Array.Empty<Exception>()))
            {
                PluginLog.LogWarning(ex, $"Error while downloading header image: {GetType().Name}. Fault count: {error_count}");
            }
            Interlocked.Increment(ref error_count);
            lastFaulted = DateTime.Now;
        }
        else
        {
            PluginLog.LogVerbose($"Updated header image: {GetType().Name}");
            image = task.Result;
            lastUpdate = DateTime.Now;
        }
        isUpdating = false;
    }

    public class Total : HeaderImage
    {
        protected override TimeSpan RetryTimer => TimeSpan.FromMinutes(2);
        protected override TimeSpan UpdateTimer => TimeSpan.FromMinutes(1);

        protected override async Task<NekoImage> DownloadHeader()
        {
            var img = await Download.DownloadImage(Plugin.ControlServer + "/count_total", ct: cts.Token);
            await img.LoadImage();
            return img;
        }
    }

    public class Individual : HeaderImage
    {
        protected override TimeSpan RetryTimer => TimeSpan.FromMinutes(1);
        protected override TimeSpan UpdateTimer => TimeSpan.FromSeconds(1);

        private int lastCount;

        protected override async Task<NekoImage> DownloadHeader()
        {
            lastCount = Plugin.Config.LocalDownloadCount;
            var img = await Download.DownloadImage($"{Plugin.ControlServer}/count/{Plugin.Config.LocalDownloadCount}", ct: cts.Token);
            await img.LoadImage();
            return img;
        }

        protected override void UpdateHeader()
        {
            if (lastCount == Plugin.Config.LocalDownloadCount)
                return;

            base.UpdateHeader();
        }
    }
}
