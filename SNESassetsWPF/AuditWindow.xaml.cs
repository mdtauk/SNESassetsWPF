using SNESassetsWPF.ViewModels;
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

namespace SNESassetsWPF
{
    /// <summary>
    /// Interaction logic for AuditWindow.xaml
    /// </summary>
    public partial class AuditWindow : Window
    {
        public MainViewModel MainVM { get; }

        public AuditWindow(MainViewModel mainView)
        {
            InitializeComponent();
            MainVM = mainView;
            DataContext = this;
        }
    }
}
