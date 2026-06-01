using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Windows.Data;

namespace SNESassetsWPF.Helpers
{
    public class InverseBoolConverter : IValueConverter
    {
        public object Convert(object value , Type targetType , object parameter , CultureInfo culture)
            => value is bool b ? !b : value;

        public object ConvertBack(object value , Type targetType , object parameter , CultureInfo culture)
            => value is bool b ? !b : value;
    }
}
