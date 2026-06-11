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
using System.Windows.Navigation;
using System.Windows.Shapes;

using SNESassetsWPF.Models;

namespace SNESassetsWPF.UserControls
{
    /// <summary>
    /// Interaction logic for WarningSummary.xaml
    /// </summary>
    public partial class WarningSummary : UserControl
    {
        public WarningSummary()
        {
            InitializeComponent();
        }



        public void ClearMessages()
        {
            itmctlWarningList.Items.Clear();
        }



        public void AddMessage(string text , string severity)
        {
            itmctlWarningList.Items.Add( new UiMessage
            {
                Text = text ,
                Severity = severity
            } );
        }
    }
}
