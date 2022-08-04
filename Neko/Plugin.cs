using System;
using System.Threading;
using Dalamud.Game.Command;
using Dalamud.IoC;
using Dalamud.Logging;
using Dalamud.Plugin;
using ImGuiScene;

namespace Neko
{
    public class Plugin : IDalamudPlugin
    {

        [PluginService] public static CommandManager CommandManager { get; private set; } = null!;
        [PluginService] public static DalamudPluginInterface PluginInterface { get; private set; } = null!;

        public string Name => "Neko Fans";

        private const string CommandMain = "/neko";
        // private const string CommandConfig = "/nekocfg";

        private readonly NekoWindow ui;

        public Plugin()
        {
            // Configuration = PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();

            /*
            CommandManager.AddHandler(CommandConfig, new CommandInfo(OnCommand)
            {
                HelpMessage = "TODO: Config Window"
            });
            */
            CommandManager.AddHandler(CommandMain, new CommandInfo(OnCommand)
            {
                HelpMessage = "Display the main window, containing the image."
            });

            ui = new();  // Create UI
            _ = NekoImage.DefaultNeko(); // Load Default image to memory


            PluginInterface.UiBuilder.OpenConfigUi += OpenConfig;
            PluginInterface.UiBuilder.Draw += DrawUI;
        }

#pragma warning disable CA1816
        public void Dispose()
        {
            ui.Visible = false;
            // CommandManager.RemoveHandler(CommandConfig);
            CommandManager.RemoveHandler(CommandMain);
        }
#pragma warning restore

        private void OnCommand(string command, string args)
        {
            ui.Visible = !ui.Visible;
        }
        private void OpenConfig()
        {
            ui.Visible = !ui.Visible;
        }

        private void DrawUI()
        {
            ui.Draw();
        }
    }
}
