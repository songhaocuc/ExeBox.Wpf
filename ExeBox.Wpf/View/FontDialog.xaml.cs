using System;
using System.Collections.Generic;
using System.ComponentModel;
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
using System.Windows.Shapes;

namespace ExeBox.Wpf.View
{
    /// <summary>
    /// FontDialog.xaml 的交互逻辑
    /// </summary>
    public partial class FontDialog : Window, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        #region FontChoice
        private FontFamily fontChoice;
        public FontFamily FontChoice
        {
            get { return fontChoice; }
            set
            {
                fontChoice = value;
                if (PropertyChanged != null)
                {
                    PropertyChanged.Invoke(this, new PropertyChangedEventArgs("FontChoice"));
                }
            }
        }

        #endregion

        #region FontSize (与Control.FontSize重名， 改成Size)
        private double size;
        public double Size
        {
            get { return size; }
            set
            {
                size = value;
                if (PropertyChanged != null)
                {
                    PropertyChanged.Invoke(this, new PropertyChangedEventArgs("Size"));
                }
            }
        }

        #endregion


        public FontDialog()
        {
            InitializeComponent();
            this.Size = Application.Current.MainWindow.FontSize;
            this.FontFamily = Application.Current.MainWindow.FontFamily;
            this.fonts.ItemsSource = Fonts.SystemFontFamilies;
            this.fonts.SelectedItem = this.FontFamily;
            this.fonts.SelectionChanged += (sender, args) =>
            {
                FontChoice = fonts.SelectedItem as FontFamily;
            };

            this.sizeUp.Click += (sender, args) => { Size++; };
            this.sizeDown.Click += (sender, args) => { Size--; };

            this.DataContext = this;
        }



        private void OnOkClick(object sender, RoutedEventArgs eventArgs)
        {
            eventArgs.Handled = true;
            DialogResult = true;
            Hide();
        }

        private void OnCancelClick(object sender, RoutedEventArgs eventArgs)
        {
            eventArgs.Handled = true;
            DialogResult = false;
            Hide();
        }
    }
}
