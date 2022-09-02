using System;

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


    public static void RandomThrow(double chance = 0.3, string message = "")
    {

#if RANDOM_THROW
        if (ThrowRandom.NextDouble() <= chance) {
            var text = "\n"
                +  "--------------------------------\n"
                +  "|         Random Throw          \n"
                + $"|         Chance: " + chance * 100 + "%\n"
                +  "--------------------------------";
            if (!string.IsNullOrEmpty(message))
                text+= "\n"
                + $"|  " + message + "\n"
                +  "--------------------------------\n";

            throw new Exception(text);
        }
#endif
    }
}

