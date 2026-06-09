using SNESassetsWPF.Models;
using System;

namespace SNESassetsWPF.Services
{
    public static class MapFileParser
    {
        /// <summary>
        /// Parse a raw MAP file into a MapFile model.
        /// 
        /// MAP format:
        ///   • 0x100‑byte header
        ///   • Followed by N × 2‑byte big‑endian MapCell entries
        ///
        /// Width is NOT stored in the file.
        /// S‑CG‑CAD and H‑CG‑CAD always use Width = 32.
        ///
        /// Height = (cellCount / 32).
        /// </summary>
        public static MapFile Parse(byte[] raw)
        {
            if ( raw == null || raw.Length < 0x100 )
                throw new ArgumentException( "MAP file too small or null." );

            var map = new MapFile();

            // 1. Copy header
            Buffer.BlockCopy( raw , 0 , map.Header , 0 , 0x100 );

            // 2. Compute cell count
            int dataBytes = raw.Length - 0x100;
            if ( dataBytes % 2 != 0 )
                throw new Exception( "MAP file has an odd number of data bytes." );

            int cellCount = dataBytes / 2;

            // 3. Try reading width/height from header
            //    (S‑CG‑CAD stores them at 0x60–0x63 in many versions)
            int headerWidth  = map.Header[0x60] | (map.Header[0x61] << 8);
            int headerHeight = map.Header[0x62] | (map.Header[0x63] << 8);

            bool headerValid =
        (headerWidth  == 32 || headerWidth  == 64) &&
        (headerHeight == 32 || headerHeight == 64) &&
        (headerWidth * headerHeight == cellCount);

            if ( headerValid )
            {
                map.Width = headerWidth;
                map.Height = headerHeight;
            }
            else
            {
                // 4. Fallback: infer from cell count
                int side = (int)Math.Sqrt(cellCount);

                if ( side * side == cellCount && ( side == 32 || side == 64 ) )
                {
                    map.Width = side;
                    map.Height = side;
                }
                else
                {
                    // Last resort: assume width = 32
                    map.Width = 32;
                    map.Height = cellCount / 32;
                }
            }

            // 5. Decode cells
            map.Cells = new MapCell[cellCount];

            int offset = 0x100;
            for ( int i = 0 ; i < cellCount ; i++ )
            {
                ushort rawValue = (ushort)((raw[offset] << 8) | raw[offset + 1]);
                offset += 2;

                map.Cells[i] = new MapCell
                {
                    RawValue = rawValue ,
                    PnlIndex = rawValue & 0x3FFF
                };
            }

            return map;
        }

    }
}
