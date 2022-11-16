using System.Collections.Generic;
using System.Threading;
using Neko.Drawing;

namespace Neko;

#pragma warning disable CA1711 // The Class is a Queue type

/// <summary>
/// Loads the next images, which will be displayed. This is all done async.
/// The amount of images loaded depends on the Queue length and if the Queue is stopped.
/// Use Pop() to get the next image.
/// </summary>
public class NekoQueue
{
    private readonly List<NekoImage> queue;
    private CancellationTokenSource tokenSource;
    public bool StopQueue;

    public NekoQueue()
    {
        tokenSource = new();
        queue = new();

        FillQueue();
        LoadImages();
    }

    private int TargetDownloadCount => StopQueue || !Plugin.ImageSource.ContainsNonFaulted() ? 0 : Plugin.Config.QueueDownloadCount;

    private int TargetPreloadCount => StopQueue || !Plugin.ImageSource.ContainsNonFaulted() ? 0 : Plugin.Config.QueuePreloadCount;

    ~NekoQueue()
    {
        tokenSource.Cancel();
    }

    public override string ToString()
    {
        var res = $"Queue length: {TargetDownloadCount}   preloaded: {TargetPreloadCount}{(StopQueue ? "   Queue Stopped" : "")}";
        for (var i = 0; i < queue.Count; i++)
        {
            var text = queue[i].ToString();
            var lines = text.Split("\n");
            res += $"\n{i,2} {lines[0]}";
            for (var l = 1; l < lines.Length - 1; l++)
            {
                res += $"\n├─ {lines[l]}";
            }
            if (lines.Length > 1)
            {
                res += $"\n└─ {lines[^1]}";
            }
        }
        return res;
    }

    public long RAMUsage()
    {
        var res = 0L;
        foreach (var item in queue)
        {
            res += item.RAMUsage;
        }
        return res;
    }

    public long VRAMUsage()
    {
        var res = 0L;
        foreach (var item in queue)
        {
            res += item.VRAMUsage;
        }
        return res;
    }

    public NekoImage? Pop()
    {
        NekoImage popped;
        // If the Queue is empty load new images, unless the queue is stopped
        if (queue.Count == 0)
        {
            if (StopQueue)
                return null;
            // This happens when you restart the plugin with all ImageSources disabled
            if (!Plugin.ImageSource.ContainsNonFaulted())
                return null;
            FillQueue();
            LoadImages();

            // If after trying to fill the queue there are no new images
            // This happens when all ImageSources are faulted 
            if (queue.Count == 0)
                return null;
        }

        // Check for NSFW mode (check for changes)
        var _ = NSFW.AllowNSFW;

        // Check if there are faulted images in the preloaded Queue
        // If there are: just use the first image.
        // If there are none:
        //      If there are images in VRAM use the latest
        //      else use the first
        var index = 0;
        for (var i = 0; i < TargetPreloadCount && i < queue.Count; i++)
        {
            var item = queue[i];
            if (item.CurrentState == NekoImage.State.Error)
            {
                break;
            }
            else if (item.CurrentState == NekoImage.State.LoadedGPU)
            {
                index = i;
                break;
            }
            else if (item.CurrentState is NekoImage.State.Decoded or NekoImage.State.Downloaded)
            {
                index = i;
            }
        }
        // Remove from queue
        popped = queue[index];
        queue.RemoveAt(index);

        // Refill Queue
        UpdateQueueLength();

        // Started Loading
        if (popped.CurrentState != NekoImage.State.LoadedGPU)
        {
            popped.DecodeAndLoadGPUAsync();
        }

        return popped;
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

        var next = Plugin.ImageSource.Next(tokenSource.Token);
        queue.Add(next);

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

    /*
        private void HandleTaskExceptions<T>(Task<T> task, QueueItem item)
        {
            foreach (var ex in task.Exception?.Flatten().InnerExceptions ?? new(Array.Empty<Exception>()))
            {
                PluginLog.LogWarning(ex, "Loading of an image failed");
            }

            // TODO: remove
            failCountGlobal--;
            if (failCountGlobal < 0)
                StopQueue = true;

            // Remove from queue
            lock (queue)
            {
                if (!queue.Remove(item))
                    PluginLog.LogDebug("Could not remove item from queue");
                // Refill Queue
                FillQueue();
                LoadImages();
            }
        }
    */
    private void LoadImages()
    {
        for (var i = 0; i < TargetPreloadCount && i < queue.Count; i++)
        {
            queue[i].RequestLoadGPU();
        }
    }
}
