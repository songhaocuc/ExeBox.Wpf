using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Newtonsoft.Json;
using Xceed.Wpf.AvalonDock.Layout;

namespace ExeBox.Wpf.Config
{
    class ConfigManager
    {
        const string filename = @"exebox.config.json";
        private string filepath;

        #region 单例
        public static ConfigManager Instance
        {
            get
            {
                return ConfigManagerInstance.INSTANCE;
            }
        }
        private static class ConfigManagerInstance
        {
            public static readonly ConfigManager INSTANCE = new ConfigManager();

        }
        private ConfigManager()
        {
            filepath = AppDomain.CurrentDomain.BaseDirectory + filename;
        }
        #endregion
        public bool LoadConfig(out Config config)
        {
            bool result = false;
            config = null;
            try
            {
                string configFile = System.IO.File.ReadAllText(filepath);
                config = JsonConvert.DeserializeObject<Config>(configFile);
                if (config != null) result = true;
            }
            catch
            {

            }
            return result;
        }

        public bool SaveConfig(Config config)
        {
            bool result;
            try
            {
                using (StreamWriter sw = new StreamWriter(filepath))
                {

                    sw.Write(JsonConvert.SerializeObject(config, typeof(Config), Formatting.Indented, new JsonSerializerSettings()));
                }
                result = true;
            }
            catch
            {
                result = false;
            }
            return result;
        }
    }

    class Config
    {
        public string FontFamily { get; set; }
        public double FontSize { get; set; }
        public Dictionary<string, LayoutConfig> Layouts { get; set; }
    }

    class LayoutConfig
    {
        public GroupLayoutInfo PaneGroup { get; set; }
    }

    class GroupLayoutInfo
    {
        public string DockWidth { get; set; }
        public string DockHeight { get; set; }
        public double DockMinWidth { get; set; }
        public double DockMinHeight { get; set; }
        public double FloatingWidth { get; set; }
        public double FloatingHeight { get; set; }
        public double FloatingLeft { get; set; }
        public double FloatingTop { get; set; }
        public bool IsMaximized { get; set; }
        public string Orientation { get; set; }
        public List<PaneLayoutInfo> Panes { get; set; }
    }

    class PaneLayoutInfo
    {
        public string DockWidth { get; set; }
        public string DockHeight { get; set; }
        public double DockMinWidth { get; set; }
        public double DockMinHeight { get; set; }
        public double FloatingWidth { get; set; }
        public double FloatingHeight { get; set; }
        public double FloatingLeft { get; set; }
        public double FloatingTop { get; set; }
        public bool IsMaximized { get; set; }
        public List<DocumentLayoutInfo> Docs { get; set; }
    }

    class DocumentLayoutInfo
    {
        public string Title { get; set; }
        public double FloatingWidth { get; set; }
        public double FloatingHeight { get; set; }
        public double FloatingLeft { get; set; }
        public double FloatingTop { get; set; }
    }

}
