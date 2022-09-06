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
    public MultiURLs(string url, IImageSource caller, int maxCount = 25) : base(url, caller, maxCount)
    {
    }

    public MultiURLs(Func<HttpRequestMessage> requestGen, IImageSource caller, int maxCount = 25) : base(requestGen, caller, maxCount)
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
    protected Task? getNewURLs;
    protected readonly ConcurrentQueue<TQueueElement> URLs = new();
    protected readonly Func<Task<TJson>> parseJson;
    protected readonly int maxCount;
    protected int taskRunning;
    protected int _urlCount;
    protected bool initilized;
    protected IImageSource caller;

    private readonly CancellationTokenSource cts = new();

    public MultiURLsGeneric(string url, IImageSource caller, int maxCount = URLThreshold)
    {
        this.maxCount = maxCount;
        this.caller = caller;
        parseJson = () => Common.ParseJson<TJson>(url, cts.Token);
    }

    public MultiURLsGeneric(Func<HttpRequestMessage> requestGen, IImageSource caller, int maxCount = URLThreshold)
    {
        this.maxCount = maxCount;
        this.caller = caller;
        parseJson = () => Common.ParseJson<TJson>(ModifyRequest(requestGen()), cts.Token);
    }

    ~MultiURLsGeneric()
    {
        cts.Cancel();
    }

    public virtual async Task<TQueueElement> GetURL()
    {
        DebugHelper.RandomThrow(DebugHelper.ThrowChance.GetURL);
        await DebugHelper.RandomDelay(DebugHelper.Delay.GetURL);

        // Try to get a URL from Queue
        TQueueElement? element;
        while (!URLs.TryDequeue(out element))
        {
            // Load more if needed
            if (_urlCount <= maxCount
                && (getNewURLs == null || getNewURLs.IsCompleted)
                && !cts.IsCancellationRequested
                && 0 == Interlocked.Exchange(ref taskRunning, 1))
            {
                getNewURLs = StartTask();
            }

            // Wait for more
            if (getNewURLs != null)
            {
                await getNewURLs;
            }
        }

        Interlocked.Decrement(ref _urlCount);
        return element;
    }

    private Task StartTask() => Task.Run(async () => await parseJson().ContinueWith(OnTaskComplete));

    private void OnTaskComplete(Task<TJson> task)
    {
        try
        {
            DebugHelper.RandomDelay(DebugHelper.Delay.MultiURL);
            OnTaskSuccessfull(task.Result);
        }
        catch (AggregateException ex)
        {
            FaultCheck.IncreaseFaultCount(caller);
            PluginLog.LogError(ex.InnerExceptions[0], "Could not get more URLs to images");
        }
        finally
        {
            Interlocked.Exchange(ref taskRunning, 0);
        }

    }

    protected virtual void OnTaskSuccessfull(TJson result)
    {
        initilized = true;
        var list = result.ToList();
        foreach (var item in list)
        {
            Interlocked.Increment(ref _urlCount);
            URLs.Enqueue(item);
        }
    }



    protected virtual HttpRequestMessage ModifyRequest(HttpRequestMessage response) => response;

    public override string ToString() => initilized ? $"URLs: {_urlCount}{(maxCount != URLThreshold ? $" TargetUrlCount: {maxCount}" : "")}" : "URLs not initialized";
}
