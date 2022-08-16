using System;
using Dalamud.Logging;

namespace Neko;

/// <summary>
/// This Class checks if NFSW images should be displayed.
/// They are only displayed if NekoLewd is installed and enabled.
/// This check is done with IPC.
/// </summary>
public static class NSFW
{
    private static bool allowNSFW = false;
    public static bool AllowNSFW
    {
        get
        {
            var check = CheckIPC();
            if (allowNSFW != check & check)
                PluginLog.Log("Detected NSFW Plugin. NSFW Images are now avalible.");
            else if (allowNSFW != check & !check)
                PluginLog.Log("NSFW Plugin was disabled. NSFW Images are unavalible");
            allowNSFW = check;
            return allowNSFW;
        }
    }

    private static bool CheckIPC()
    {
        if (!Plugin.PluginInterface.PluginInternalNames.Contains("NekoLewd"))
            return false;

        bool ipcCheck;
        try
        {
            var sub = Plugin.PluginInterface.GetIpcSubscriber<bool>("NSFW Check");
            ipcCheck = sub.InvokeFunc();
        }
        catch (Exception)
        {
            // Could not use IPC to Check for NSFW mode
            return false;
        }
        return ipcCheck;
    }
}