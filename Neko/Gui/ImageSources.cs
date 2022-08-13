using System;
using System.Collections.Generic;
using System.Numerics;
using System.Threading.Tasks;
using Dalamud.Logging;
using ImGuiNET;
using ImGuiScene;
using Neko.Sources;

namespace Neko.Gui
{
    public class ImageSourcesGUI
    {
        private bool cbNekoLifes;
        private bool cbShibeOnline;

        public ImageSourcesGUI()
        {
            cbNekoLifes = Plugin.Config.ImageSource.Contains(typeof(NekosLife));
            cbShibeOnline = Plugin.Config.ImageSource.Contains(typeof(ShibeOnline));
        }

        public void Draw()
        {
            if (ImGui.Checkbox("nekos.life", ref cbNekoLifes))
            {
                if (cbNekoLifes)
                    Plugin.Config.ImageSource.AddSource(new NekosLife());
                else
                    Plugin.Config.ImageSource.RemoveAll(typeof(NekosLife));
            }
            if (ImGui.Checkbox("shibe.online", ref cbShibeOnline))
            {
                if (cbShibeOnline)
                    Plugin.Config.ImageSource.AddSource(new ShibeOnline());
                else
                    Plugin.Config.ImageSource.RemoveAll(typeof(ShibeOnline));
            }
            if (!cbNekoLifes && !cbShibeOnline)
            {
                if (Plugin.GuiMain != null) // Stop queue new images if there are no image sources
                    Plugin.GuiMain.queue.StopQueue = true;
                ImGui.TextColored(new Vector4(1f, 0f, 0f, 1f), "WARNING:");
                ImGui.SameLine();
                ImGui.TextWrapped("No Image source is selected. This makes loading new images impossible.");
            }
            else
            {
                if (Plugin.GuiMain != null)
                    Plugin.GuiMain.queue.StopQueue = false;
            }
        }
    }
}