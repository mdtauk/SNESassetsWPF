using System;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;

namespace SNESassetsWPF.Helpers
{
    /// <summary>
    /// Ensures WPF elements (typically Images) render at true 100% pixel scale
    /// regardless of monitor DPI. Does NOT apply any user zoom.
    /// </summary>
    public static class DpiScaleHelper
    {
        private const int WM_DPICHANGED = 0x02E0;

        // -------------------------------------------------------------
        //  Attached Property: Maintain100PercentScale
        // -------------------------------------------------------------
        public static readonly DependencyProperty Maintain100PercentScaleProperty =
            DependencyProperty.RegisterAttached(
                "Maintain100PercentScale",
                typeof(bool),
                typeof(DpiScaleHelper),
                new PropertyMetadata(false, OnMaintain100PercentScaleChanged));

        public static void SetMaintain100PercentScale(DependencyObject obj , bool value)
            => obj.SetValue( Maintain100PercentScaleProperty , value );

        public static bool GetMaintain100PercentScale(DependencyObject obj)
            => (bool)obj.GetValue( Maintain100PercentScaleProperty );

        private static void OnMaintain100PercentScaleChanged(DependencyObject d , DependencyPropertyChangedEventArgs e)
        {
            if ( d is FrameworkElement element && (bool)e.NewValue )
            {
                element.Loaded += (s , _) =>
                {
                    ApplyScale( element );
                    HookDpiChanges( element );
                };
            }
        }

        // -------------------------------------------------------------
        //  Hook into WM_DPICHANGED
        // -------------------------------------------------------------
        private static void HookDpiChanges(FrameworkElement element)
        {
            var source = (HwndSource)PresentationSource.FromVisual(element);
            if ( source == null )
                return;

            source.AddHook( (IntPtr hwnd , int msg , IntPtr wParam , IntPtr lParam , ref bool handled) =>
            {
                if ( msg == WM_DPICHANGED )
                {
                    ApplyScale( element );
                }
                return IntPtr.Zero;
            } );
        }

        // -------------------------------------------------------------
        //  ApplyScale (DPI fix only)
        // -------------------------------------------------------------
        private static void ApplyScale(FrameworkElement element)
        {
            var dpi = VisualTreeHelper.GetDpi(element);

            // Cancel Windows DPI scaling so 1 bitmap pixel = 1 screen pixel
            double inverseX = 1.0 / dpi.DpiScaleX;
            double inverseY = 1.0 / dpi.DpiScaleY;

            element.LayoutTransform = new ScaleTransform( inverseX , inverseY );
        }
    }
}
