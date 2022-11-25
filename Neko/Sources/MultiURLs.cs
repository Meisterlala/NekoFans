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
/// <typeparam name="T">Json to parse into</typeparam>
public class MultiURLs<T> : MultiURLsGeneric<T, string>
    where T : IJsonToList<string>
{
    public MultiURLs(string url, ImageSource caller, int maxCount = 25) : base(url, caller, maxCount)
    {
    }

    public MultiURLs(Func<HttpRequestMessage> requestGen, ImageSource caller, int maxCount = 25) : base(requestGen, caller, maxCount)
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
    protected ImageSource caller;

    protected readonly CancellationTokenSource cts = new();

    public MultiURLsGeneric(string url, ImageSource caller, int maxCount = URLThreshold)
    {
        this.maxCount = maxCount;
        this.caller = caller;
        parseJson = () => Download.ParseJson<TJson>(url, cts.Token);
    }

    public MultiURLsGeneric(Func<HttpRequestMessage> requestGen, ImageSource caller, int maxCount = URLThreshold)
    {
        this.maxCount = maxCount;
        this.caller = caller;
        parseJson = () => Download.ParseJson<TJson>(ModifyRequest(requestGen()), cts.Token);
    }

    ~MultiURLsGeneric()
    {
        cts.Cancel();
    }

    public virtual async Task<TQueueElement> GetURL(CancellationToken ct = default)
    {
        DebugHelper.RandomThrow(DebugHelper.ThrowChance.GetURL);
        await DebugHelper.RandomDelay(DebugHelper.Delay.GetURL, ct).ConfigureAwait(false);

        TQueueElement? element;
        do
        {
            // Cancel if needed
            if (caller.Faulted || ct.IsCancellationRequested)
                throw new OperationCanceledException();

            // Load more if needed
            if (_urlCount <= maxCount
                && Interlocked.Exchange(ref taskRunning, 1) == 0)
            {
                getNewURLs = StartTask();
            }

            // Wait for more
            if (getNewURLs != null && _urlCount <= 0)
            {
                await getNewURLs.WaitAsync(ct).ConfigureAwait(false);
            }

            // Try to get a URL from Queue
        } while (!URLs.TryDequeue(out element));

        Interlocked.Decrement(ref _urlCount);
        return element;
    }

    private Task StartTask() => Task.Run(async () => await parseJson().ContinueWith(OnTaskComplete, cts.Token).ConfigureAwait(false), cts.Token);

    private void OnTaskComplete(Task<TJson> task)
    {
        try
        {
            DebugHelper.RandomDelay(DebugHelper.Delay.MultiURL, cts.Token);
            OnTaskSuccessfull(task.Result);
        }
        catch (AggregateException ex)
        {
            throw new Exception("Could not get more URLs to images", ex.InnerException);
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

    public override string ToString()
    => initilized ? $"URLs: {_urlCount}{(maxCount != URLThreshold ? $" TargetUrlCount: {maxCount}" : "")}{(taskRunning == 1 ? "\tLoading more ..." : "")}" : "URLs not initialized";
}
