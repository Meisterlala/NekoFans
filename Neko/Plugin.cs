using System.Net.Http;
using System.Reflection;
using Dalamud.Game.Command;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using Neko.Drawing;
using Neko.Gui;

namespace Neko;

#pragma warning disable CA1816 // Dispose warining
#pragma warning disable RCS1170 // Use read-only auto-implemented property.

public class Plugin : IDalamudPlugin
{
    [PluginService] public static ICommandManager CommandManager { get; private set; } = null!;
    [PluginService] public static DalamudPluginInterface PluginInterface { get; private set; } = null!;
    [PluginService] public static IKeyState KeyState { get; private set; } = null!;
    [PluginService] public static IPluginLog Log { get; private set; } = null!;
    [PluginService] public static INotificationManager NotificationManager { get; private set; } = null!;
    [PluginService] public static ITextureProvider TextureProvider { get; private set; } = null!;



    public static string Name => "Neko Fans";

    public static Configuration Config { get; private set; } = null!;
    public static MainWindow? GuiMain { get; private set; }
    public static ConfigWindow? GuiConfig { get; private set; }
    public static Sources.CombinedSource ImageSource { get; private set; } = null!;

    public const string ControlServer = "https://api.nekofans.net";

    private const string CommandMain = "/neko";
    private const string CommandConfig = "/nekocfg";

    public static readonly HttpClient HttpClient = new(
        new HttpClientHandler()
        {
            AutomaticDecompression = System.Net.DecompressionMethods.All
        }
    )
    {
        DefaultRequestHeaders = {
            UserAgent =
             {
                new("NekoFans", Assembly.GetExecutingAssembly().GetName().Version?.ToString()),
                new("(a Plugin for Final Fantasy XIV)")
            },
        },
    };

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

        Embedded.LoadAll(); // Load all embedded resources

        PluginInterface.UiBuilder.OpenConfigUi += ToggleConfigGui;
        PluginInterface.UiBuilder.Draw += DrawUI;
        PluginInterface.UiBuilder.OpenMainUi += ToggleMainGui;

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

        // Stop loading images
        GuiMain?.Dispose();
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
        // Allow open/close with middle mouse button
        if (GuiMain?.Visible != true && Config.Hotkeys.ToggleWindow.IsPressed())
            ToggleMainGui();

        GuiMain?.Draw();
        GuiConfig?.Draw();
    }


    public static void ToggleMainGui()
    {
        GuiMain ??= new();
        GuiMain.Visible = !GuiMain.Visible;
        Config.Save();
    }

    public static void ToggleConfigGui()
    {
        GuiConfig ??= new();
        GuiConfig.Visible = !GuiConfig.Visible;
        // Config.Save();
    }

    public static void ShowMainGui()
    {
        GuiMain ??= new();
        GuiMain.Visible = true;
    }
    public static void ShowConfigGui()
    {
        GuiConfig ??= new();
        GuiConfig.Visible = true;
    }
}
