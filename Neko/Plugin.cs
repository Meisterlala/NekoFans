using Dalamud.Game.Command;
using Dalamud.IoC;
using Dalamud.Logging;
using Dalamud.Plugin;
using Neko.Gui;

namespace Neko
{
    public class Plugin : IDalamudPlugin
    {

        [PluginService] public static CommandManager CommandManager { get; private set; } = null!;
        [PluginService] public static DalamudPluginInterface PluginInterface { get; private set; } = null!;

        public string Name => "Neko Fans";

        public static Configuration Config { get; private set; } = null!;
        public static NekoWindow? GuiMain { get; private set; } = null!;
        public static ConfigWindow? GuiConfig { get; private set; } = null!;


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
            _ = NekoImage.DefaultNeko(); // Load Default image to memory

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

#pragma warning disable CA1816
        public void Dispose()
        {
            if (GuiMain != null)
                GuiMain.Visible = false;
            if (GuiConfig != null)
                GuiConfig.Visible = false;

            CommandManager.RemoveHandler(CommandConfig);
            CommandManager.RemoveHandler(CommandMain);
        }
#pragma warning restore

        private void OnCommand(string command, string args)
        {
            var input = command + args;
            if (input.Contains("cfg") || input.Contains("config"))
                ToggleConfigGui();
            else
                ToggleMainGui();
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
}
