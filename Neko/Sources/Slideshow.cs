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
        UpdateFromConfig();
    }

    public void UpdateFromConfig()
    {
        // Check for miminimum interval
        if (Plugin.Config.SlideshowIntervalSeconds < MININTERVAL)
            Plugin.Config.SlideshowIntervalSeconds = MININTERVAL;
        timer.Interval = Plugin.Config.SlideshowIntervalSeconds * 1000;
        timer.Enabled = Plugin.Config.SlideshowEnabled;
    }

    public void Start() => timer.Start();
    public void Stop() => timer.Stop();
    public void UpdateInterval(double intervalMS) => timer.Interval = intervalMS;
    public void Restart()
    {
        timer.Stop();
        timer.Start();
    }

}