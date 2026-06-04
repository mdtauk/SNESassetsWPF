using System;
using System.IO;
using SNESassetsWPF.Models;

namespace SNESassetsWPF.Formats
{
    public static class MapFileReader
    {
        public static MapFileReadResult Load(string path , PnlFile pnl = null)
        {
            try
            {
                byte[] raw = File.ReadAllBytes(path);

                if ( raw.Length < 0x100 + 2 )
                    return MapFileReadResult.Fail( "MAP file too small." );

                // 1. Skip 0x100 header
                int offset = 0x100;

                int tileBytes = raw.Length - offset;
                int totalTiles = tileBytes / 2;

                if ( totalTiles == 0 )
                    return MapFileReadResult.Fail( "MAP contains no tile data." );

                int mapWidth;
                int mapHeight;

                // 2. If PNL is available → use correct SNES width
                if ( pnl != null )
                {
                    int metaW = pnl.MetaWidth;
                    mapWidth = PnlFile.PanelWidth / metaW;
                    mapHeight = totalTiles / mapWidth;
                }
                else
                {
                    // 3. MAP-only mode → guess width (square-ish)
                    mapWidth = (int)Math.Sqrt( totalTiles );
                    if ( mapWidth < 1 ) mapWidth = 1;

                    mapHeight = totalTiles / mapWidth;
                }

                var map = new MapFile(mapWidth, mapHeight)
                {
                    Header = new byte[0x100]
                };

                Buffer.BlockCopy( raw , 0 , map.Header , 0 , 0x100 );

                // 4. Read tile entries
                int pos = offset;

                for ( int y = 0 ; y < mapHeight ; y++ )
                {
                    for ( int x = 0 ; x < mapWidth ; x++ )
                    {
                        if ( pos + 1 >= raw.Length )
                        {
                            map.Tiles[x , y] = null;
                            continue;
                        }

                        ushort attr = (ushort)((raw[pos] << 8) | raw[pos + 1]);
                        pos += 2;

                        map.Tiles[x , y] = new MapTile
                        {
                            RawAttributeWord = attr ,
                            MetaTileIndex = attr & 0x03FF ,
                            PaletteRowOverride = ( attr >> 10 ) & 0x07 ,
                            Priority = ( attr & 0x2000 ) != 0 ,
                            HFlip = ( attr & 0x4000 ) != 0 ,
                            VFlip = ( attr & 0x8000 ) != 0
                        };
                    }
                }

                return MapFileReadResult.Ok( map , raw );
            }
            catch ( Exception ex )
            {
                return MapFileReadResult.Fail( "MAP parse error: " + ex.Message );
            }
        }

    }
}
