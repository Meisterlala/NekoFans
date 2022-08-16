using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Dalamud.Logging;
using ImGuiScene;

namespace Neko;

#pragma warning disable CA1711 // The Class is a Queue type

/// <summary>
/// Loads the next images, which will be displayed. This is all done async.
/// The amount of images loaded depends on the Queue length and if the Queue is stopped.
/// Use Pop() to get the next image.
/// </summary>
public class NekoQueue
{
    private class QueueItem
    {
        public Task<NekoImage>? downloadTask;
        public Task<TextureWrap>? imageTask;
        public bool imageShouldLoad;
    }

    private readonly List<QueueItem> queue;
    private CancellationTokenSource tokenSource;
    public bool StopQueue;

    public NekoQueue()
    {
        tokenSource = new();
        queue = new();

        FillQueue();
        LoadImages();
    }

    private int TargetDownloadCount => StopQueue ? 0 : Plugin.Config.QueueDownloadCount;

    private int TargetPreloadCount => StopQueue ? 0 : Plugin.Config.QueuePreloadCount;

    ~NekoQueue()
    {
        tokenSource.Cancel();
    }

    public override string ToString()
    {
        var res = $"Queue length: {TargetDownloadCount}   preloaded: {TargetPreloadCount}{(StopQueue ? "   Queue Stopped" : "")}";
        for (var i = 0; i < queue.Count; i++)
        {
            var item = queue[i];
            if (item.downloadTask == null)
                res += $"\n{i} [ Download not started ]";
            else if (!item.downloadTask.IsCompleted)
                res += $"\n{i} [ Downloading ]";
            else if (!item.downloadTask.IsCompletedSuccessfully)
                res += $"\n{i} [ Could not Download ]";
            else if (item.imageTask?.IsCompletedSuccessfully ?? false)
                res += $"\n{i} [  Loaded  ] {item.downloadTask.Result}";
            else if (!item.imageTask?.IsCompleted ?? false)
                res += $"\n{i} [ Loading  ] {item.downloadTask.Result}";
            else if (item.imageTask?.IsFaulted ?? false)
                res += $"\n{i} [  Error  ] {item.downloadTask.Result}";
            else if (item.imageShouldLoad)
                res += $"\n{i} [ Scheduled ] {item.downloadTask.Result}";
            else if (item.downloadTask.IsCompleted)
                res += $"\n{i} [ Downloaded ] {item.downloadTask.Result}";
            else
                res += $"\n{i} [ Unknown State ]";
        }
        return res;
    }

    public async Task<NekoImage> Pop()
    {
        // If the Queue is empty load new images, unless the queue is stopped
        if (queue.Count == 0)
        {
            if (StopQueue)
                return new NekoImage();
            FillQueue();
            LoadImages();
        }

        // Check if there are faulted images in the preloaded Queue
        // If there are: just use the first image.
        // If there are none:
        //      If there are images in VRAM use the latest
        //      else use the first
        var index = 0;
        for (var i = 0; i < TargetPreloadCount && i < queue.Count; i++)
        {
            var item = queue[i];
            if ((item.downloadTask?.IsFaulted ?? true)
                || (item.imageTask != null && item.imageTask.IsFaulted))
            {
                break;
            }
            else if ((item.downloadTask?.IsCompletedSuccessfully ?? false)
                       && (item.imageTask?.IsCompletedSuccessfully ?? false))
            {
                index = i;
                break;
            }
        }

        // Remove from queue
        var popped = queue[index];
        queue.RemoveAt(index);

        // Refill Queue
        FillQueue();
        LoadImages();

        if (popped.downloadTask == null)
            throw new Exception("Image was never downloaded");

        // Return image if it has loaded (and has errors)
        if (popped.imageTask?.IsCompleted ?? false)
            return await popped.downloadTask;

        // Wait for download
        if (!popped.downloadTask.IsCompleted)
            await popped.downloadTask;

        // Return if download didnt succeed
        if (popped.downloadTask.IsFaulted)
            return new NekoImage();

        // Start Loading to VRAM
        popped.imageShouldLoad = true;
        LoadImage(popped);
        if (popped.imageTask == null)
            throw new Exception("Imagetask not started");

        // Wait for load to VRAM
        await popped.imageTask;

        // Return if image load didnt succeed 
        return popped.imageTask.IsFaulted ? new NekoImage() : await popped.downloadTask;
    }

    public void UpdateQueueLength()
    {
        // Currently the image queue will only grow and never shrink
        FillQueue();
        LoadImages();
    }

    private void FillQueue()
    {
        if (queue.Count >= TargetDownloadCount) return; // Base case

        var item = new QueueItem();

        var download = Plugin.ImageSource.Next(tokenSource.Token);

        download.ContinueWith((task) =>
        {
            if (task.IsFaulted)
                HandleTaskExceptions(task);
            else
                LoadImages();
        });

        queue.Add(new QueueItem
        {
            downloadTask = download,
        });

        FillQueue(); // Recursivly fill
    }

    public void Refresh()
    {
        tokenSource.Cancel();
        tokenSource = new CancellationTokenSource();
        queue.Clear();
        FillQueue();
        LoadImages();
    }

    private static void HandleTaskExceptions<T>(Task<T> task)
    {
        foreach (var ex in task.Exception?.Flatten().InnerExceptions ?? new(Array.Empty<Exception>()))
        {
            PluginLog.LogError(ex, "Loading of a image failed");
        }
    }

    private void LoadImages()
    {
        for (var i = 0; i < TargetPreloadCount && i < queue.Count; i++)
        {
            queue[i].imageShouldLoad = true;
            LoadImage(queue[i]);
        }
    }

    private static void LoadImage(QueueItem item)
    {
        if (item.imageTask != null                          // If already loading
            || !item.imageShouldLoad                        // If it should not load
            || item.downloadTask == null                    // If never downloaded
            || !item.downloadTask.IsCompletedSuccessfully)  // If couldnt download
        {
            return;
        }

        var img = item.downloadTask.Result;
        item.imageTask = img.LoadImage();
        item.imageTask.ContinueWith(HandleTaskExceptions, TaskContinuationOptions.OnlyOnFaulted);
    }
}
