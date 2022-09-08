using Dalamud.Game.Command;
using Dalamud.IoC;
using Dalamud.Plugin;
using Neko.Gui;

namespace Neko;

#pragma warning disable CA1816 // Dispose warining

public class Plugin : IDalamudPlugin
{

    [PluginService] public static CommandManager CommandManager { get; private set; } = null!;
    [PluginService] public static DalamudPluginInterface PluginInterface { get; private set; } = null!;
    [PluginService] public static Dalamud.Game.ClientState.Keys.KeyState KeyState { get; private set; } = null!;

    public string Name => "Neko Fans";

    public static Configuration Config { get; private set; } = null!;
    public static MainWindow? GuiMain { get; private set; } = null!;
    public static ConfigWindow? GuiConfig { get; private set; } = null!;
    public static Sources.CombinedSource ImageSource { get; private set; } = null!;

    private const string CommandMain = "/neko";
    private const string CommandConfig = "/nekocfg";


    public Plugin()
    {
        // Setup commands
        CommandManager.AddHandler(CommandConfig, new CommandInfo(OnCommand)
        {
            HelpMessage = "Configuration window"
        });

        CommandManager.AddHandler(CommandMain, new CommandInfo(OnCommand)
        {
            HelpMessage = "Display the main window, containing the image."
        });

        Config = Configuration.Load(); // Load Configuration
        ImageSource = Config.LoadSources(); // Load ImageSources from config
        // Load Embedded Images
        var _imageError = NekoImage.Embedded.ImageError.Load();
        var _ImageLoading = NekoImage.Embedded.ImageLoading.Load();

        PluginInterface.UiBuilder.OpenConfigUi += ToggleConfigGui;
        PluginInterface.UiBuilder.Draw += DrawUI;

        // Open Main Window
        if (Config.GuiMainVisible)
            ShowMainGui();

#if DEBUG
        // always open all windows in Debug
        ShowMainGui();
        ShowConfigGui();
#endif
    }

    public void Dispose()
    {
        CommandManager.RemoveHandler(CommandConfig);
        CommandManager.RemoveHandler(CommandMain);
    }

    public static void UpdateImageSource() => ImageSource.UpdateFrom(Config.LoadSources());

    /// <summary>
    ///  This will clear all downloaded images and start downloading new ones.
    /// </summary>
    public static void ReloadSources() => ImageSource = Config.LoadSources();

    private void OnCommand(string command, string args)
    {
        var input = command + args;

        if (input.Contains("cfg", System.StringComparison.CurrentCultureIgnoreCase)
         || input.Contains("config", System.StringComparison.CurrentCultureIgnoreCase))
        {
            ToggleConfigGui();
        }
        else
        {
            ToggleMainGui();
        }
    }

    private void DrawUI()
    {
        if (GuiMain != null)
            GuiMain.Draw();

        if (GuiConfig != null)
            GuiConfig.Draw();
    }

    public static void ToggleMainGui()
    {
        if (GuiMain == null)
            GuiMain = new();
        GuiMain.Visible = !GuiMain.Visible;
    }

    public static void ToggleConfigGui()
    {
        if (GuiConfig == null)
            GuiConfig = new();
        GuiConfig.Visible = !GuiConfig.Visible;
    }

    public static void ShowMainGui()
    {
        if (GuiMain == null)
            GuiMain = new();
        GuiMain.Visible = true;
    }
    public static void ShowConfigGui()
    {
        if (GuiConfig == null)
            GuiConfig = new();
        GuiConfig.Visible = true;
    }
}

