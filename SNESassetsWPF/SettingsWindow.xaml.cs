using SNESassetsWPF.ViewModels;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;




namespace SNESassetsWPF
{
    /// <summary>
    /// Interaction logic for SettingsWindow.xaml
    /// </summary>
    public partial class SettingsWindow : Window
    {
        public SettingsWindow( PaletteViewModel PaletteView )
        {
            InitializeComponent();
            DataContext = PaletteView;

            if ( PaletteView == null )
                Debug.WriteLine( "[Settings] no palette view model was passed in" );
        }
    }
}
