using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using ExeBox.Wpf.Config;
using Xceed.Wpf.AvalonDock.Layout;

namespace ExeBox.Wpf
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            LoadAppConfig();
            this.Loaded += (sender, e) =>
            {
                Controller.MainController mainController = new Controller.MainController(this);
                mainController.Init();
            };
        }

        private void FontFamilyChange(object sender, RoutedEventArgs e)
        {
            var fontDialog = new View.FontDialog();
            fontDialog.Top = this.Top + this.Height / 2 - fontDialog.Height / 2;
            fontDialog.Left = this.Left + this.Width / 2 - fontDialog.Width / 2;
            if (fontDialog.ShowDialog() == true )
            {
                Config.Config config;
                if (!Config.ConfigManager.Instance.LoadConfig(out config))
                {
                    config = new Config.Config();
                }
                

                if (fontDialog.FontChoice != null)
                {
                    Application.Current.MainWindow.FontFamily = fontDialog.FontChoice;
                    config.FontFamily = fontDialog.FontChoice.Source;
                }
                Application.Current.MainWindow.FontSize = fontDialog.Size;
                config.FontSize = fontDialog.Size;


                Config.ConfigManager.Instance.SaveConfig(config);
            }

            
        }

        private void LoadAppConfig()
        {
            Config.Config config;
            if (Config.ConfigManager.Instance.LoadConfig(out config))
            {
                if (!string.IsNullOrEmpty(config.FontFamily))
                {
                    FontFamily fontFamily = new FontFamily(config.FontFamily);
                    Application.Current.MainWindow.FontFamily = fontFamily;
                }

                if(config.FontSize > 0)
                {
                    Application.Current.MainWindow.FontSize = config.FontSize;
                }
            }
        }

        public void SaveLayoutConfig(object sender, RoutedEventArgs e)
        {
            Config.Config config;
            if (!Config.ConfigManager.Instance.LoadConfig(out config))
            {
                config = new Config.Config();
            }
            var layouts = config.Layouts;
            if (layouts == null)
            {
                layouts = new Dictionary<string, LayoutConfig>();
                config.Layouts = layouts;
            }
            

            // layout
            string filename =  Model.LogTaskManager.Instance.FileName;
            LayoutConfig layout = new LayoutConfig();
            if (layouts.ContainsKey(filename))
            {
                layout = layouts[filename];
            }
            else
            {
                layout = new LayoutConfig();
                layouts.Add(filename, layout);
            }

            GroupLayoutInfo groupLayout = new GroupLayoutInfo();
            layout.PaneGroup = groupLayout;
            // group layout
            LayoutDocumentPaneGroup root = this.documentsRoot;
            //GroupLayoutInfo groupLayout = layout.PaneGroup;
            //if (groupLayout == null)
            //{
            //    groupLayout = new GroupLayoutInfo();
            //    layout.PaneGroup = groupLayout;
            //}
            GridLengthConverter _gridLengthConverter = new GridLengthConverter();
            groupLayout.DockWidth = _gridLengthConverter.ConvertToInvariantString(root.DockWidth);
            groupLayout.DockHeight = _gridLengthConverter.ConvertToInvariantString(root.DockHeight);
            groupLayout.DockMinWidth = root.DockMinWidth;
            groupLayout.DockMinHeight = root.DockMinHeight;
            groupLayout.FloatingWidth = root.FloatingWidth;
            groupLayout.FloatingHeight = root.FloatingHeight;
            groupLayout.FloatingLeft = root.FloatingLeft;
            groupLayout.FloatingTop = root.FloatingTop;
            groupLayout.IsMaximized = root.IsMaximized;
            groupLayout.Orientation = root.Orientation.ToString();
            groupLayout.Panes = new List<PaneLayoutInfo>();

            foreach (LayoutDocumentPane pane in root.Children)
            {
                // pane layout 
                if (pane == null) continue;
                PaneLayoutInfo paneLayout = new PaneLayoutInfo();
                paneLayout.DockWidth = _gridLengthConverter.ConvertToInvariantString(pane.DockWidth);
                paneLayout.DockHeight = _gridLengthConverter.ConvertToInvariantString(pane.DockHeight);
                paneLayout.DockMinWidth = root.DockMinWidth;
                paneLayout.DockMinHeight = root.DockMinHeight;
                paneLayout.FloatingWidth = root.FloatingWidth;
                paneLayout.FloatingHeight = root.FloatingHeight;
                paneLayout.FloatingLeft = root.FloatingLeft;
                paneLayout.FloatingTop = root.FloatingTop;
                paneLayout.IsMaximized = root.IsMaximized;
                paneLayout.Docs = new List<DocumentLayoutInfo>();
                foreach (var doc in pane.Children)
                {
                    var documentLayout = new Config.DocumentLayoutInfo();
                    documentLayout.Title = doc.Title;
                    documentLayout.FloatingWidth = doc.FloatingWidth;
                    documentLayout.FloatingHeight = doc.FloatingHeight;
                    documentLayout.FloatingLeft = doc.FloatingLeft;
                    documentLayout.FloatingTop = doc.FloatingTop;
                    paneLayout.Docs.Add(documentLayout);
                }
                groupLayout.Panes.Add(paneLayout);
            }

            Config.ConfigManager.Instance.SaveConfig(config);
        }
    }


}
