using System;
using System.Windows.Media;

namespace SNESassetsWPF.Models
{
    public class PaletteEntry
    {
        public SnesColor SnesColor { get; set; }
        public Color RgbColor { get; set; }

        public string SnesColorString { get; set; }
        public string RGBColorString { get; set; }

        public SolidColorBrush RgbColorBrush => new SolidColorBrush( RgbColor );

        public bool IsPlaceholder { get; set; }

        public PaletteEntry() { }

        // NEW: Constructor for RGB-only palette entries (from ColFile)
        public PaletteEntry(Color rgbColor)
        {
            RgbColor = rgbColor;
            RGBColorString = $"#{rgbColor.R:X2}{rgbColor.G:X2}{rgbColor.B:X2}";
            SnesColor = default;
            SnesColorString = "";
        }

        // SNES constructor (kept, but no ConvertBgr555 dependency)
        public PaletteEntry(byte low , byte high)
        {
            SnesColor = new SnesColor( low , high );

            int value = low | (high << 8);

            int r = (value & 0x1F) << 3;
            int g = ((value >> 5) & 0x1F) << 3;
            int b = ((value >> 10) & 0x1F) << 3;

            RgbColor = Color.FromArgb( 255 , (byte)r , (byte)g , (byte)b );

            SnesColorString = SnesColor.ToHexPair();
            RGBColorString = $"#{RgbColor.R:X2}{RgbColor.G:X2}{RgbColor.B:X2}";
        }
    }
}
