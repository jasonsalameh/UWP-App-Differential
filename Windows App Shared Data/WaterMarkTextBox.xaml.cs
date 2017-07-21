using System;
using System.Collections.Generic;
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

namespace Windows_App_Shared_Data
{
    /// <summary>
    /// Interaction logic for WaterMarkTextBox.xaml
    /// </summary>
    public partial class WaterMarkTextBox : UserControl
    {
        private string watermark;
        public string WaterMark
        {
            set { watermark = value; }
            get { return watermark; }
        }

        public string Text
        {
            get
            {
                if (WaterMarkBox.Text == this.watermark)
                {
                    return "";
                }
                else
                {
                    return WaterMarkBox.Text;
                }
            }
            set
            {
                DisableWaterMark();
                WaterMarkBox.Text = value;
            }
        }

        public WaterMarkTextBox()
        {
            InitializeComponent();
        }

        public void ResetWaterMark()
        {
            SetWaterMark();
        }

        private void SetWaterMark()
        {
            WaterMarkBox.Foreground = new SolidColorBrush(Colors.DarkGray);
            WaterMarkBox.Text = this.watermark;
        }

        void WaterMarkBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(WaterMarkBox.Text))
            {
                SetWaterMark();
                WaterMarkBox.GotFocus += new RoutedEventHandler(WaterMarkBox_GotFocus);
            }
        }

        private void DisableWaterMark()
        {
            WaterMarkBox.Text = "";
            WaterMarkBox.GotFocus -= WaterMarkBox_GotFocus;
            WaterMarkBox.Foreground = new SolidColorBrush(Colors.Black);
        }

        void WaterMarkBox_GotFocus(object sender, RoutedEventArgs e)
        {
            DisableWaterMark();
        }

        private void WaterMarkBox_Loaded(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(this.watermark))
            {
                WaterMarkBox.GotFocus += new RoutedEventHandler(WaterMarkBox_GotFocus);
                WaterMarkBox.LostFocus += new RoutedEventHandler(WaterMarkBox_LostFocus);
                SetWaterMark();
            }
        }
    }
}
