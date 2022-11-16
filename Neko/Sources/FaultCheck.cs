using System;
using System.Threading;
using System.Threading.Tasks;
using Dalamud.Logging;
using Neko.Drawing;

namespace Neko.Sources;

public sealed class FaultCheck : IImageSource
{
    public bool Faulted
    {
        get => HasFaulted;
        set
        {
            PluginLog.LogWarning("Trying to set a FaultCheck to Faulted");
            FaultCount = MaxFaultCount;
        }
    }

#if RANDOM_THROW // Set a higher limit for testing
    private const int MaxFaultCount = 25;
#else
    private const int MaxFaultCount = 5;
#endif

    private int FaultCount;
    private readonly IImageSource Source;

    public bool HasFaulted => FaultCount >= MaxFaultCount || Source.Faulted;
    private readonly object FaultLock = new();
    private CancellationTokenSource cts = new();

    public string Name => Source.Name;

    private FaultCheck(IImageSource source) => Source = source;

    public NekoImage Next(CancellationToken ct = default)
    {
        if (HasFaulted || Source.Faulted)
        {
            PluginLog.LogWarning("Image Task faulted to many times and is disabled");
            return Embedded.ImageError.Image!;
        }

        // If either the caller passed a token, which is cancelled, or the token of this class is cancelled
        // this childToken will be cancelled
        var childToken = CancellationTokenSource.CreateLinkedTokenSource(ct, cts.Token).Token;

        try
        {
            return Source.Next(childToken);
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex)
        {
            Interlocked.Increment(ref FaultCount);
            if (HasFaulted)
            {
                lock (FaultLock)
                {
                    if (!cts.IsCancellationRequested)
                        FaultLimitReached();
                }
            }
            throw new Exception($"Image Task faulted. Fault Count increased to {FaultCount}", ex);
        }
    }

    public void FaultLimitReached()
    {
        PluginLog.LogError($"Fault limit reached for {Source.Name}. This API will be disabled");
        Source.Faulted = true;
        cts.Cancel();
    }

    public void ResetFaultCount()
    {
        Interlocked.Exchange(ref FaultCount, 0);
        Source.Faulted = false;
        cts = new();
    }

    public override string ToString()
    {
        var status = HasFaulted
            ? "ERROR"
            : FaultCount > 0
            ? $" {FaultCount} "
            : "OK";

        return $"<{status}> {Source.ToString() ?? "Fault Check"}";
    }

    public static FaultCheck Wrap(IImageSource source)
        => source is FaultCheck faultCheck
            ? faultCheck
            : new(source);
    public IImageSource UnWrap() => Source;

    public static void IncreaseFaultCount(IImageSource source)
    {
        var fcs = Plugin.ImageSource.GetAll<FaultCheck>();
        foreach (var fc in fcs)
        {
            if (fc.Source.Equals(source))
            {
                Interlocked.Increment(ref fc.FaultCount);
                if (fc.HasFaulted)
                    fc.FaultLimitReached();
                return;
            }
        }
        PluginLog.LogDebug("Could not increase FaultCount for {0}", source);
    }

    public bool Equals(IImageSource? other) => other?.Equals(Source) == true;
}
