using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Dalamud.Logging;

namespace Neko.Sources;

/// <summary>
/// Stores a List of URLs, which are provided from an API.
/// This is used when the API returns a list of many URLs to images
/// </summary>
/// <typeparam name="T">Json to parse into</typeparam>
public class MultiURLs<T> where T : IJsonToList
{
    public int URLCount { get => urlCount; }

    private const int URLThreshold = 25;
    private Task<T> getNewURLs;
    private readonly string url;
    private readonly ConcurrentQueue<string> URLs = new();
    private int taskRunning = 0;
    private int urlCount = 0;

    public MultiURLs(string url)
    {
        this.url = url;
        getNewURLs = StartTask();
    }

    public async Task<string> GetURL()
    {
        // Load more
        if (urlCount <= URLThreshold
            && getNewURLs.IsCompletedSuccessfully
            && 0 == Interlocked.Exchange(ref taskRunning, 1))
            getNewURLs = StartTask();

        await getNewURLs;
        Interlocked.Decrement(ref urlCount);
        URLs.TryDequeue(out string? res);

        if (res == null || getNewURLs.IsFaulted)
            throw new Exception("Could not get URLs to images");

        return res;
    }

    private async Task<T> StartTask()
    {
        Task<T> tsk = Common.ParseJson<T>(url);
        await tsk.ContinueWith((task) =>
        {
            foreach (var ex in task.Exception?.Flatten().InnerExceptions ?? new(Array.Empty<Exception>()))
            {
                _ = ex;
            }

            if (task.IsCompletedSuccessfully)
            {
                try
                {
                    var list = task.Result.ToList();
                    foreach (var item in list)
                    {
                        Interlocked.Increment(ref urlCount);
                        URLs.Enqueue(item);
                    }
                }
                catch (Exception ex)
                {
                    PluginLog.LogError(ex, "Could not get more URLs to images");
                }
                finally
                {
                    Interlocked.Exchange(ref taskRunning, 0);
                }
            }
        });
        return await tsk;
    }
}
