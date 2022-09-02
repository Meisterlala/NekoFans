using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Dalamud.Logging;

namespace Neko.Sources;

public class FaultCheck : IImageSource
{
    private int FaultCount;
    private readonly IImageSource Source;
    private const int MaxFaultCount = 999;
    public bool HasFaulted => FaultCount >= MaxFaultCount;

    private FaultCheck(IImageSource source) => Source = source;

    public async Task<NekoImage> Next(CancellationToken ct = default)
    {
        if (HasFaulted)
        {
            PluginLog.LogWarning("Task Faulted and is disabled");
            return await NekoImage.Embedded.ImageError.Load();
        }

        try
        {
            return await Source.Next(ct);
        }
        catch (Exception ex)
        {
            Interlocked.Increment(ref FaultCount);
            PluginLog.LogWarning(ex, "Image Task faulted");
            return await NekoImage.Embedded.ImageError.Load();
        }
    }

    public override string ToString()
    {
        var status = HasFaulted
            ? "ERROR"
            : FaultCount > 0
            ? FaultCount.ToString()
            : "OK";

        return $"({status}) {Source.ToString() ?? "Fault Check"}";
    }
    public bool IsFaulted() => FaultCount >= MaxFaultCount;

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
            if (fc.Source == source)
            {
                Interlocked.Increment(ref fc.FaultCount);
                return;
            }
        }
        PluginLog.LogDebug("Could not increase FaultCount for {0}", source);
    }


}