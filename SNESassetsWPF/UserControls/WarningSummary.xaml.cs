using SNESassetsWPF.Models;
using System;
using System.Collections;
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




        public IEnumerable ItemsSource
        {
            get => (IEnumerable)GetValue( ItemsSourceProperty );
            set => SetValue( ItemsSourceProperty , value );
        }

        public static readonly DependencyProperty ItemsSourceProperty =
            DependencyProperty.Register(
                nameof(ItemsSource),
                typeof(IEnumerable),
                typeof(WarningSummary),
                new PropertyMetadata(null, OnItemsSourceChanged));

        private static void OnItemsSourceChanged(
            DependencyObject d , DependencyPropertyChangedEventArgs e)
        {
            var control = (WarningSummary)d;
            control.itmctlWarningList.ItemsSource = (IEnumerable)e.NewValue;
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
