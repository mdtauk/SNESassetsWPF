using SNESassetsWPF.Enums;
using SNESassetsWPF.ViewModels;

namespace SNESassetsWPF.Helpers
{
    public class MapPnlViewModeItem : ViewModelBase
    {
        public MapPnlViewModes Mode { get; set; }
        public string DisplayName { get; set; }

        private bool _isEnabled;
        public bool IsEnabled
        {
            get => _isEnabled;
            set
            {
                if ( _isEnabled == value )
                    return;

                _isEnabled = value;
                OnPropertyChanged();
            }
        }

        public static string GetName(MapPnlViewModes mode)
        {
            return mode switch
            {
                MapPnlViewModes.PnlDebug => "PNL Debug View",
                MapPnlViewModes.PnlFull => "PNL Full Render",
                MapPnlViewModes.MapDebug => "MAP Debug View",
                MapPnlViewModes.MapFull => "MAP Full Render",

                _ => mode.ToString()
            };
        }
    }
}
