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
        public HexWindow(string hexText , byte[] metadata)
        {
            InitializeComponent();
            txtHex.Text = hexText;

            string header = "(no header found)";

            if ( metadata != null && metadata.Length > 0 )
            {
                // Find first null terminator (end of ASCII header)
                int end = Array.IndexOf(metadata, (byte)0);
                if ( end < 0 ) end = metadata.Length;

                // Decode only up to the first null
                header = Encoding.ASCII.GetString( metadata , 0 , end ).Trim();
            }

            txtHeader.Text = header;
        }



        private void btnCopyHex_Click(object sender , RoutedEventArgs e)
        {
            // Split into lines
            var lines = txtHex.Text.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);

            // Skip the first line (the title)
            var hexLines = lines.Skip(1);

            // Join back together
            string rawHex = string.Join("", hexLines)
                .Replace(" ", "")
                .Replace("\r", "")
                .Replace("\n", "");

            Clipboard.SetText( rawHex );
        }

    }
}
