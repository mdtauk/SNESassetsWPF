using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Xml.Linq;




namespace SNESassetsWPF
{
    /// <summary>
    /// Interaction logic for HexWindow.xaml
    /// </summary>
    public partial class HexWindow : Window
    {
        public HexWindow(string hexText , string headerText)
        {
            InitializeComponent();
            txtHex.Text = hexText;   // assuming your TextBox is named txtHex
            txtHeader.Text = headerText;
        }

        private void btnCopyHex_Click(object sender , RoutedEventArgs e)
        {
            string raw = txtHex.Text
                .Replace(" ", "")
                .Replace("\r", "")
                .Replace("\n", "");

            Clipboard.SetText( raw );
        }
    }
}
