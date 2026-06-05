using System;
using SNESassetsWPF.Models;

namespace SNESassetsWPF.Formats
{
    /// <summary>
    /// Parses raw MAP bytes into a structured MapFile + MapCells.
    /// </summary>
    public static class MapParser
    {
        /// <summary>
        /// Entry point: parse a MapFileReadResult into a MapFile.
        /// </summary>
        public static MapFileReadResult Parse(MapFileReadResult readResult , PnlFile pnl = null)
        {
            if ( !readResult.Success )
                return readResult;

            try
            {
                byte[] raw = readResult.RawFile;

                byte[] header = ExtractHeader(raw);
                int totalTiles = CountTiles(raw);

                if ( totalTiles <= 0 )
                    return MapFileReadResult.Fail( "MAP contains no tile entries." );

                (int width , int height) = DetermineDimensions( totalTiles , pnl );

                var map = new MapFile(width, height)
                {
                    Header = header,
                    IsMode7Enabled = false // reserved for future decoding
                };

                FillCells( map , raw );

                readResult.Map = map;
                return readResult;
            }
            catch ( Exception ex )
            {
                return MapFileReadResult.Fail( "MAP parse error: " + ex.Message );
            }
        }

        // ------------------------------------------------------------
        //  Helpers
        // ------------------------------------------------------------

        private static byte[] ExtractHeader(byte[] raw)
        {
            byte[] header = new byte[0x100];
            Buffer.BlockCopy( raw , 0 , header , 0 , 0x100 );
            return header;
        }

        private static int CountTiles(byte[] raw)
        {
            int offset = 0x100;
            int tileBytes = raw.Length - offset;
            return tileBytes / 2;
        }

        private static (int width , int height) DetermineDimensions(int totalTiles , PnlFile pnl)
        {
            if ( pnl != null )
            {
                int metaW = pnl.MetaWidth;
                int width = PnlFile.PanelWidth / metaW;
                int height = totalTiles / width;
                return (width , height);
            }

            // No PNL: assume square-ish MAP
            int wGuess = (int)Math.Sqrt(totalTiles);
            if ( wGuess < 1 ) wGuess = 1;
            int hGuess = totalTiles / wGuess;

            return (wGuess , hGuess);
        }

        private static void FillCells(MapFile map , byte[] raw)
        {
            int pos = 0x100;

            for ( int y = 0 ; y < map.Height ; y++ )
            {
                for ( int x = 0 ; x < map.Width ; x++ )
                {
                    if ( pos + 1 >= raw.Length )
                    {
                        map.Cells[x , y] = null;
                        continue;
                    }

                    ushort word = (ushort)((raw[pos] << 8) | raw[pos + 1]);
                    pos += 2;

                    int pnlX = word & 0x1F;
                    int pnlY = (word >> 5) & 0x1FF;
                    bool usePanelAttr = (word & 0x4000) != 0;

                    map.Cells[x , y] = new MapCell
                    {
                        CellPositionX = x ,
                        CellPositionY = y ,
                        RawValue = word ,
                        PnlX = pnlX ,
                        PnlY = pnlY ,
                        UsePanelAttributes = usePanelAttr
                    };
                }
            }
        }
    }
}
