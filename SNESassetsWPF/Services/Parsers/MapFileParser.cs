using System;
using System.Diagnostics;
using SNESassetsWPF.Models;

namespace SNESassetsWPF.Formats
{
    /// <summary>
    /// Parses a raw MAP file (loaded by MapFileReader)
    /// into a structured MapFile model.
    ///
    /// Responsibilities:
    ///   • Split header + cell data
    ///   • Infer width/height (MAP files do NOT store these)
    ///   • Decode each 16‑bit MapCell
    ///   • Extract PnlIndex (lower 14 bits)
    ///
    /// Does NOT:
    ///   • Perform rendering
    ///   • Modify raw bytes
    ///   • Apply meta‑tile grouping (renderer does that)
    /// </summary>
    public static class MapFileParser
    {
        public static MapFile Parse(byte[] raw)
        {
            if ( raw == null || raw.Length < 0x100 )
                throw new ArgumentException( "MAP file too small to contain header." );

            var map = new MapFile();

            // ------------------------------------------------------------
            // 1. Copy header (0x100 bytes)
            // ------------------------------------------------------------
            Array.Copy( raw , 0 , map.Header , 0 , 0x100 );

            // ------------------------------------------------------------
            // 2. Remaining bytes = 16‑bit MAP cells
            // ------------------------------------------------------------
            int cellBytes = raw.Length - 0x100;

            if ( cellBytes % 2 != 0 )
            {
                Debug.WriteLine( "[MAP PARSER] Warning: MAP cell data is not aligned to 2 bytes." );
                // We continue anyway, truncating the last odd byte.
                cellBytes -= 1;
            }

            int cellCount = cellBytes / 2;

            map.Cells = new MapCell[cellCount];

            // ------------------------------------------------------------
            // 3. Infer width/height
            //
            // S‑CG‑CAD MAPs are always rectangular.
            // Common sizes:
            //   32×32 (1024 cells)
            //   64×64 (4096 cells)
            //   128×128 (16384 cells)
            //
            // But MAP files do NOT store width/height.
            // We infer width by assuming the map is square.
            // If not square, we fall back to 1×N.
            // ------------------------------------------------------------
            int inferred = (int)Math.Sqrt(cellCount);

            if ( inferred * inferred == cellCount )
            {
                map.Width = inferred;
                map.Height = inferred;
            }
            else
            {
                // Fallback: treat as 1×N strip
                map.Width = cellCount;
                map.Height = 1;

                Debug.WriteLine( $"[MAP PARSER] Non‑square MAP detected. Using {map.Width}×{map.Height}." );
            }

            // ------------------------------------------------------------
            // 4. Parse each 16‑bit MAP entry
            // ------------------------------------------------------------
            int offset = 0x100;

            for ( int i = 0 ; i < cellCount ; i++ )
            {
                ushort rawValue = (ushort)(raw[offset] | (raw[offset + 1] << 8));

                var cell = new MapCell
                {
                    RawValue = rawValue,
                    PnlIndex = rawValue & 0x3FFF   // lower 14 bits
                };

                map.Cells[i] = cell;

                offset += 2;
            }

            return map;
        }
    }
}
