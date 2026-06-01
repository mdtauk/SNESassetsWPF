using SNESassetsWPF.Formats;
using SNESassetsWPF.Models;
using System;
using System.Windows.Media;

namespace SNESassetsWPF.Services
{
    public class ColFileParser
    {
        public ColFile Parse(ColFileReadResult raw)
        {
            if ( !raw.IsValid )
                throw new InvalidOperationException( raw.ErrorMessage );

            var col = new ColFile();

            byte[] palette = raw.RawColorData;   // ← THIS is the palette data (512 bytes)

            if ( palette.Length < 512 )
                throw new InvalidOperationException( "Palette data is incomplete (less than 512 bytes)." );

            // 256 colours × 2 bytes = 512 bytes
            for ( int i = 0 ; i < 512 ; i += 2 )
            {
                byte low = palette[i];
                byte high = palette[i + 1];

                var snes = new SnesColor(low, high);

                // Decode SNES BGR555 → RGB8
                int value = snes.Value;
                int r = (value & 0x1F) << 3;
                int g = ((value >> 5) & 0x1F) << 3;
                int b = ((value >> 10) & 0x1F) << 3;

                var rgb = Color.FromRgb((byte)r, (byte)g, (byte)b);

                int index = i / 2;
                int p = index / 16;
                int c = index % 16;

                col.RawColors[p , c] = snes;
                col.RgbColors[p , c] = rgb;
            }

            col.Metadata = raw.RawMetadata;

            return col;
        }
    }
}
