using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Windows;
using System.Windows.Data;

namespace SNESassetsWPF.Helpers
{
    public class BoolToVisibilityConverter : IValueConverter
    {
        public bool Invert { get; set; }
        public bool Collapse { get; set; } = true;

        public object Convert(object value , Type targetType , object parameter , CultureInfo culture)
        {
            bool flag = value is bool b && b;

            if ( Invert )
                flag = !flag;

            if ( flag )
                return Visibility.Visible;

            return Collapse ? Visibility.Collapsed : Visibility.Hidden;
        }

        public object ConvertBack(object value , Type targetType , object parameter , CultureInfo culture)
        {
            if ( value is Visibility v )
                return v == Visibility.Visible;

            return false;
        }
    }
}
