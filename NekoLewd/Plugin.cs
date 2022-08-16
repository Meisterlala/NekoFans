using System;
using Dalamud.IoC;
using Dalamud.Logging;
using Dalamud.Plugin;
using Dalamud.Plugin.Ipc;

namespace NekoLewd;

/// <summary>
/// This Plugin returns true via IPC.
/// The offical repository doesnt allow some content. This Plugin is only hosted on third party repositorys.
/// By Checking if this plugin is installed and checking with IPC if the plugin is enabled we can allow
/// another Plugin to give access to additional features when detected.
/// Feel free to copy/paste and use in your Projects
/// </summary>
public class Plugin : IDalamudPlugin
{
    public string Name => "Neko Fans NSFW 18+ Patch";

    [PluginService] public static DalamudPluginInterface PluginInterface { get; private set; } = null!;

    private readonly ICallGateProvider<bool> IPCProvider;

    public Plugin()
    {
        IPCProvider = PluginInterface.GetIpcProvider<bool>("NSFW Check");
        IPCProvider.RegisterFunc(() => true);
    }

    public void Dispose()
    {
        IPCProvider.UnregisterFunc();
    }
}