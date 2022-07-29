using Dalamud.Data;
using Dalamud.Game;
using Dalamud.Game.ClientState;
using Dalamud.Game.ClientState.Objects;
using Dalamud.Game.Command;
using Dalamud.IoC;
using Dalamud.Plugin;
using System;

namespace Neko
{
    public class Plugin : IDalamudPlugin
    {

        [PluginService] public static CommandManager CommandManager { get; private set; } = null!;
        [PluginService] public static DalamudPluginInterface PluginInterface { get; private set; } = null!;

        public string Name => "Neko Plugin";

        public static Configuration? Configuration;
        private readonly NekoWindow ui;

        public Plugin()
        {
            Configuration = PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
            ui = new();

            CommandManager.AddHandler("neko", new CommandInfo(OnCommand)
            {
                HelpMessage = "Display the main window, containing the image."
            });

            CommandManager.AddHandler("nekocfg", new CommandInfo(OnCommand)
            {
                HelpMessage = "TODO"
            });

            PluginInterface.UiBuilder.OpenConfigUi += OpenConfig;
            PluginInterface.UiBuilder.Draw += DrawUI;
        }

        public void Dispose()
        {
            CommandManager.RemoveHandler("neko");
        }

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
