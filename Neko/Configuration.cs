using Dalamud.Configuration;
using System;
using System.Collections.Generic;

namespace Neko
{
    [Serializable]
    public class Configuration : IPluginConfiguration
    {
        public int Version { get; set; } = 1;


        public string Name;
        public uint ModelMain;
        public float Offset;
        public bool Enabled;



        public void Save()
        {
            Plugin.PluginInterface.SavePluginConfig(this);
        }
    }
}
