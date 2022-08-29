using System;
using System.Timers;

namespace Neko.Sources;

public class Slideshow
{
    private readonly Timer timer;

    public const double MININTERVAL = 1;

    public Slideshow(Action onTick)
    {
        timer = new();
        timer.Elapsed += new ElapsedEventHandler((_, _) => onTick());
        timer.Stop();
        UpdateFromConfig();
    }

    ~Slideshow()
    {
        Dalamud.Logging.PluginLog.Log("Dispose ");
        timer.Stop();
        timer.Dispose();
    }

    public void UpdateFromConfig()
    {
        // Check for miminimum interval
        if (Plugin.Config.SlideshowIntervalSeconds < MININTERVAL)
            Plugin.Config.SlideshowIntervalSeconds = MININTERVAL;
        timer.Interval = Plugin.Config.SlideshowIntervalSeconds * 1000;
        timer.Enabled = Plugin.Config.SlideshowEnabled;
        Dalamud.Logging.PluginLog.Log("Setting time to: " + timer.Enabled);
    }

    public void Start() => timer.Start();
    public void Stop() => timer.Stop();
    public void UpdateInterval(double intervalMS) => timer.Interval = intervalMS;
    public void Restart()
    {
        if (timer.Enabled)
        {
            timer.Stop();
            timer.Start();
        }
    }

    public override string ToString() => $"Slideshow {(timer.Enabled ? "" : "not")} enabled.\t Inverval: {Helper.SecondsToString(timer.Interval / 1000)}";
}