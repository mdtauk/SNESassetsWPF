using System;
using System.Collections.Generic;
using System.Linq;
using SNESassetsWPF.Enums;

namespace SNESassetsWPF.Helpers
{
    public class MapPnlViewModeItem
    {
        public MapPnlViewModes Mode { get; set; }
        public string DisplayName { get; set; }
        public bool IsEnabled { get; set; }
    }

    public static class MapPnlViewModeValues
    {
        public static IEnumerable<MapPnlViewModeItem> All =>
            Enum.GetValues( typeof( MapPnlViewModes ) )
                .Cast<MapPnlViewModes>()
                .Select( m => new MapPnlViewModeItem
                {
                    Mode = m ,
                    DisplayName = GetName( m )
                } );

        public static string GetName(MapPnlViewModes mode)
        {
            return mode switch
            {
                MapPnlViewModes.Auto => "Auto (Best Match)",
                MapPnlViewModes.PnlDebug => "PNL Debug View",
                MapPnlViewModes.PnlFull => "PNL Render",
                MapPnlViewModes.MapFull => "MAP Render",
                MapPnlViewModes.MapDebug => "MAP Debug View",
                _ => mode.ToString()
            };
        }
    }
}
