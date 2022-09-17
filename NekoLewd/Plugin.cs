using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Plugin.Ipc;

namespace NekoLewd;

#pragma warning disable CA1816 // Dispose warining
#pragma warning disable RCS1170 // Use read-only auto-implemented property.

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

    [PluginService] public static DalamudPluginInterface PluginInterface { get; } = null!;

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