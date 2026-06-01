using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Media;

namespace SNESassetsWPF.Helpers
{
    public class AccentHelper
    {
        public Color GetAccentColor()
        {
            if ( Application.Current.Resources["SystemAccentColor"] is Color c )
                return c;

            // Fallback for older Fluent versions
            if ( Application.Current.Resources["AccentColor"] is Color c2 )
                return c2;

            return Colors.Transparent;
        }

    }
}
