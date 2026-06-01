using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Windows.Data;

namespace SNESassetsWPF.Helpers
{
    public class RowSelectableConverter : IMultiValueConverter
    {
        public object Convert(object[] values , Type targetType , object parameter , CultureInfo culture)
        {
            int bpp = (int)values[0];
            bool forceSingleRow = (bool)values[1];

            return bpp switch
            {
                2 => true,              // always selectable
                4 => forceSingleRow,    // selectable only when checkbox checked
                8 => false,             // never selectable
                _ => false
            };
        }

        public object[] ConvertBack(object value , Type[] targetTypes , object parameter , CultureInfo culture)
            => throw new NotImplementedException();
    }

}
