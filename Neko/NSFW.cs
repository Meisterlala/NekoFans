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
    private enum NSFW_MODE
    {
        Enabled, Disabled, Unknown
    }

    private static NSFW_MODE mode = NSFW_MODE.Unknown!;
    public static bool AllowNSFW
    {
        get
        {
            var check = CheckIPC();
            var newMode = check ? NSFW_MODE.Enabled : NSFW_MODE.Disabled;
            if (newMode != mode && mode != NSFW_MODE.Unknown)
            {
                mode = newMode;
                ChangeDetected(mode);
            }
            else
            {
                mode = newMode;
            }
            return mode == NSFW_MODE.Enabled;
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

    private static void ChangeDetected(NSFW_MODE mode)
    {
        if (mode == NSFW_MODE.Unknown)
            return;
        else if (mode == NSFW_MODE.Enabled)
            PluginLog.Log("Detected NSFW Plugin. NSFW Images are now avalible.");
        else
            PluginLog.Log("NSFW Plugin was disabled. NSFW Images are unavalible");

        // Reload Config
        Plugin.ReloadImageSources();
        // Refresh Image Queue
        Plugin.GuiMain?.Queue.Refresh();
    }
}
