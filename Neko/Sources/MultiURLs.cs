using System;
using System.Collections.Concurrent;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Dalamud.Logging;


namespace Neko.Sources;


/// <summary>
/// Stores a List of URLs, which are provided from an API.
/// This is used when the API returns a list of many URLs to images
/// </summary>
/// <typeparam name="TJson">Json to parse into</typeparam>
public class MultiURLs<T> : MultiURLsGeneric<T, string>
    where T : IJsonToList<string>
{
    public MultiURLs(string url, int maxCount = 25) : base(url, maxCount)
    {
    }

    public MultiURLs(Func<HttpRequestMessage> requestGen, int maxCount = 25) : base(requestGen, maxCount)
    {
    }
}


/// <summary>
/// Stores a List of <typeparamref name="TQueueElement"/>, which are provided from an API.
/// This is used when the API returns a list of many <typeparamref name="TQueueElement"/>s
/// </summary>
/// <typeparam name="TJson">Json to parse into</typeparam>
/// <typeparam name="TQueueElement">Result of the API</typeparam>
public class MultiURLsGeneric<TJson, TQueueElement>
    where TJson : IJsonToList<TQueueElement>
{
    public int URLCount => _urlCount;
    protected const int URLThreshold = 25;
    protected Task<TJson> getNewURLs;
    protected readonly ConcurrentQueue<TQueueElement> URLs = new();
    protected readonly Func<Task<TJson>> parseJson;
    protected readonly int maxCount;
    protected int taskRunning;
    protected int _urlCount;


    public MultiURLsGeneric(string url, int maxCount = URLThreshold)
    {
        this.maxCount = maxCount;
        parseJson = () => Common.ParseJson<TJson>(url);
        getNewURLs = StartTask();
    }

    public MultiURLsGeneric(Func<HttpRequestMessage> requestGen, int maxCount = URLThreshold)
    {
        this.maxCount = maxCount;
        parseJson = () => Common.ParseJson<TJson>(ModifyRequest(requestGen()));
        getNewURLs = StartTask();
    }

    public virtual async Task<TQueueElement> GetURL()
    {
        // Load more
        if (_urlCount <= maxCount
            && getNewURLs.IsCompletedSuccessfully
            && 0 == Interlocked.Exchange(ref taskRunning, 1))
        {
            getNewURLs = StartTask();
        }

        await getNewURLs;
        Interlocked.Decrement(ref _urlCount);
        URLs.TryDequeue(out var res);

        return res == null || getNewURLs.IsFaulted ? throw new Exception("Could not get URLs to images") : res;
    }

    private async Task<TJson> StartTask()
    {
        var tsk = parseJson();
        await tsk.ContinueWith(OnTaskComplete);
        return await tsk;
    }

    private void OnTaskComplete(Task<TJson> task)
    {
        foreach (var ex in task.Exception?.Flatten().InnerExceptions ?? new(Array.Empty<Exception>()))
        {
            _ = ex;
        }

        if (task.IsCompletedSuccessfully)
        {
            try
            {
                OnTaskSuccessfull(task.Result);
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
    }

    protected virtual void OnTaskSuccessfull(TJson result)
    {
        var list = result.ToList();
        foreach (var item in list)
        {
            Interlocked.Increment(ref _urlCount);
            URLs.Enqueue(item);
        }
    }



    protected virtual HttpRequestMessage ModifyRequest(HttpRequestMessage response) => response;

    public override string ToString() => $"URLs: {_urlCount}{(maxCount != URLThreshold ? $" TargetUrlCount: {maxCount}" : "")}";
}
