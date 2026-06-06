using System;
using SNESassetsWPF.Models;

namespace SNESassetsWPF.Formats
{
    /// <summary>
    /// Parses raw MAP bytes into a structured MapFile + MapCells.
    /// Matches G‑CG‑CAD behaviour.
    /// </summary>
    public static class MapParser
    {
        public static MapFile Parse(byte[] data)
        {
            if ( data == null || data.Length < 0x100 + 2 )
                throw new ArgumentException( "MAP file too small to be valid." );

            // ------------------------------------------------------------
            // 1. Read header (first 0x100 bytes)
            // ------------------------------------------------------------
            byte[] header = new byte[0x100];
            Buffer.BlockCopy( data , 0 , header , 0 , 0x100 );

            // Mode 7 flag (bit 7 of header[0])
            bool mode7 = (header[0] & 0x80) != 0;

            // ------------------------------------------------------------
            // 2. MAP entries begin at 0x100
            // ------------------------------------------------------------
            int offset = 0x100;
            int entryBytes = data.Length - offset;

            if ( entryBytes % 2 != 0 )
                throw new ArgumentException( "MAP tile data is not aligned to 16-bit entries." );

            int entryCount = entryBytes / 2;

            // ------------------------------------------------------------
            // 3. MAP width is ALWAYS 64 groups (G‑CG‑CAD rule)
            // ------------------------------------------------------------
            int mapWidth = 64;

            // Height is computed from total entries
            int mapHeight = entryCount / mapWidth;
            if ( mapHeight < 1 )
                mapHeight = 1;

            var map = new MapFile(mapHeight)
            {
                IsMode7Enabled = mode7
            };

            // Copy header into MapFile
            Buffer.BlockCopy( header , 0 , map.Header , 0 , 0x100 );

            // ------------------------------------------------------------
            // 4. Parse MAP entries into MapCells
            // ------------------------------------------------------------
            int pos = offset;

            for ( int i = 0 ; i < entryCount ; i++ )
            {
                int x = i % mapWidth;
                int y = i / mapWidth;

                ushort raw = (ushort)((data[pos] << 8) | data[pos + 1]);
                pos += 2;

                map.Cells[x , y] = new MapCell
                {
                    X = x ,
                    Y = y ,
                    RawValue = raw
                };
            }

            return map;
        }
    }
}
