using System;
using System.Threading;
using System.Threading.Tasks;

namespace Neko;

public static class DebugHelper
{
    public static void LogNetwork(Func<string> message)
    {
#pragma warning disable IDE0022 // Use expression body for methods
#if NETWORK
        PluginLog.LogVerbose(message());
#endif
#pragma warning restore IDE0022
    }

#pragma warning disable IDE0052 // Remove unused parameter
    private static readonly Random ThrowRandom = new();
#pragma warning restore IDE0052


    public class ThrowChance
    {
        public const double DownloadImage = 0.3;
        public const double ParseJson = 0.3;
        public const double GetURL = 0.3;
        public const double Mock = 0.3;
    }

    public static void RandomThrow(double chance = 0.3)
    {

#if RANDOM_THROW
        if (ThrowRandom.NextDouble() <= chance) {
             throw new Exception("\n"
                +  "┌───────────────────┐\n"
                +  "│         Random Throw\n"
                + $"│        Chance: {chance:P}\n"
                +  "└───────────────────┘");
        }
#endif
    }


    public class Delay
    {
        public const int DownloadImage = 1000 * 1;
        public const int ParseJson = 1000 * 2;
        public const int GetURL = 1000 * 1;
        public const int Mock = 150;

        public const double mean = 1.0;
        public const double stdDev = 0.4;
    }

#pragma warning disable IDE0052 // Remove unused parameter
    private static readonly Random DelayRandom = new();
#pragma warning restore IDE0052


    public static Task RandomDelay(int delayMS, CancellationToken ct = default)
    {
        double u1 = 1.0 - DelayRandom.NextDouble(); //uniform(0,1] random doubles
        double u2 = 1.0 - DelayRandom.NextDouble();
        double randStdNormal = Math.Sqrt(-2.0 * Math.Log(u1)) *
                     Math.Sin(2.0 * Math.PI * u2); //random normal(0,1)
        double randNormal = Delay.mean + Delay.stdDev * randStdNormal; //random normal(mean,stdDev^2)

        return Task.Delay((int)(randNormal * delayMS), ct);
    }
}

