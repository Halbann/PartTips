using System;
using System.IO;
using UnityEngine;

namespace PartTips
{
    public static class Settings
    {
        private static string PluginData =>
            Path.Combine(KSPUtil.ApplicationRootPath, "GameData", "PartTips", "PluginData");

        private static string Config =>
            Path.Combine(PluginData, "settings.cfg");

        public static KeyCode tooltipButton = KeyCode.Mouse2;

        public static void Save()
        {
            if (!Directory.Exists(PluginData))
                Directory.CreateDirectory(PluginData);

            ConfigNode settings = new ConfigNode("PartTipsSettings");
            settings.SetValue("tooltipButton", tooltipButton.ToString(), true);

            ConfigNode file = new ConfigNode();
            file.AddNode(settings);
            file.Save(Config);
        }

        public static void Load()
        {
            if (!File.Exists(Config))
                return;

            ConfigNode file = ConfigNode.Load(Config);
            ConfigNode settings = file.GetNode("PartTipsSettings");

            try
            {
                string tooltipButtonString = tooltipButton.ToString();
                if (settings.TryGetValue("tooltipButton", ref tooltipButtonString))
                    tooltipButton = (KeyCode)Enum.Parse(typeof(KeyCode), tooltipButtonString);
            }
            catch
            {
                Debug.LogError("[PartTips]: Failed to parse keybind. Falling back to MMB.");
            }
        }
    }
}
