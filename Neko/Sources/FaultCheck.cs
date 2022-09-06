using System;
using System.Threading;
using System.Threading.Tasks;
using Dalamud.Logging;

namespace Neko.Sources;

public class FaultCheck : IImageSource
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

    private int FaultCount;
    private readonly IImageSource Source;
    private const int MaxFaultCount = 5;
    public bool HasFaulted => FaultCount >= MaxFaultCount;

    private FaultCheck(IImageSource source) => Source = source;

    public async Task<NekoImage> Next(CancellationToken ct = default)
    {
        if (HasFaulted || Source.Faulted)
        {
            PluginLog.LogWarning("Image Task faulted to many times and is disabled");
            return await NekoImage.Embedded.ImageError.Load();
        }

        try
        {
            return await Source.Next(ct); ;
        }
        catch (Exception ex)
        {
            Interlocked.Increment(ref FaultCount);
            throw new Exception($"Image Task faulted. Fault Count increased to {FaultCount}", ex);
        }
    }

    public void FaultLimitReached()
    {
        PluginLog.LogWarning("Fault limit reached for " + Source.ToString());
        Plugin.ImageSource.RemoveSource(Source);
        Source.Faulted = true;
    }

    public void ResetFaultCount() => Interlocked.Exchange(ref FaultCount, 0);

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

    public bool Equals(IImageSource? other) => other != null && Source.Equals(other);
}
