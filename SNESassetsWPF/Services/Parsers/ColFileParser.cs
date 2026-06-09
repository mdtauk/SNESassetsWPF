using SNESassetsWPF.Models;
using System;
using System.IO;
using System.Windows.Media;

namespace SNESassetsWPF.Formats
{
    public static class ColFileParser
    {
        public static ColFile Parse(ColFileReadResult raw)
        {
            if ( !raw.Success )
                throw new InvalidOperationException( raw.ErrorMessage );

            if ( raw.RawFile == null || raw.RawFile.Length < 512 )
                throw new InvalidDataException( "COL file too small — need at least 512 bytes." );

            var col = new ColFile();

            // ------------------------------------------------------------
            // Palette = FIRST 512 BYTES (this is how your old parser worked)
            // ------------------------------------------------------------
            byte[] palette = new byte[512];
            Buffer.BlockCopy( raw.RawFile , 0 , palette , 0 , 512 );

            for ( int i = 0 ; i < 512 ; i += 2 )
            {
                byte low  = palette[i];
                byte high = palette[i + 1];

                var snes = new SnesColor(low, high);

                int value = snes.Value;
                int r = (value & 0x1F) << 3;
                int g = ((value >> 5) & 0x1F) << 3;
                int b = ((value >> 10) & 0x1F) << 3;

                var rgb = Color.FromRgb((byte)r, (byte)g, (byte)b);

                int index = i / 2;
                int row = index / 16;
                int colIndex = index % 16;

                col.RawColors[row , colIndex] = snes;
                col.RgbColors[row , colIndex] = rgb;
            }

            // Your old parser used RawMetadata from the reader.
            // Since your current reader does NOT provide metadata,
            // we keep this empty (same behaviour as before).
            col.Metadata = Array.Empty<byte>();

            // Build flat 256‑entry cache
            col.BuildCachedColors();

            return col;
        }
    }
}
