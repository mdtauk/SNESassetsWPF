using System;
using System.IO;
using SNESassetsWPF.Models;

namespace SNESassetsWPF.Formats
{
    public static class PnlFileReader
    {
        public static PnlFileReadResult Load(string path)
        {
            if ( !File.Exists( path ) )
                return PnlFileReadResult.Fail( "File not found: " + path );

            try
            {
                byte[] data = File.ReadAllBytes(path);

                if ( data.Length < 0x10100 )
                    return PnlFileReadResult.Fail( "PNL file is too small to be valid." );

                var pnl = new PnlFile
                {
                    Header = new byte[0x100],
                    Tiles = new PnlTile[PnlFile.PanelWidth, PnlFile.PanelHeight]
                };

                Buffer.BlockCopy( data , 0 , pnl.Header , 0 , 0x100 );

                pnl.MetaWidth = 1 << ( pnl.Header[0x69] & 0x1F );
                pnl.MetaHeight = 1 << ( pnl.Header[0x6A] & 0x1F );

                pnl.Mode7Enabled = pnl.Header[0x61] != 0;

                int attrOffset = 0x100;
                int flagOffset = attrOffset + 0x8000;

                int index = 0;
                for ( int y = 0 ; y < PnlFile.PanelHeight ; y++ )
                {
                    for ( int x = 0 ; x < PnlFile.PanelWidth ; x++ )
                    {
                        ushort attr = (ushort)((data[attrOffset + index] << 8) |
                                                data[attrOffset + index + 1]);

                        ushort flag = (ushort)((data[flagOffset + index] << 8) |
                                               data[flagOffset + index + 1]);

                        index += 2;

                        pnl.Tiles[x , y] = new PnlTile
                        {
                            RawAttributeWord = attr ,
                            RawFlagWord = flag ,

                            TileId = attr & 0x03FF ,
                            PaletteRow = ( attr >> 10 ) & 0x07 ,
                            Priority = ( ( attr >> 13 ) & 0x01 ) != 0 ,
                            HFlip = ( ( attr >> 14 ) & 0x01 ) != 0 ,
                            VFlip = ( ( attr >> 15 ) & 0x01 ) != 0 ,

                            Present = ( ( flag >> 15 ) & 0x01 ) != 0
                        };
                    }
                }

                //  Build meta‑tiles for MapPnlRenderer
                pnl.BuildMetaTiles();

                return PnlFileReadResult.Ok( pnl );
            }
            catch ( Exception ex )
            {
                return PnlFileReadResult.Fail( "Exception while reading PNL: " + ex.Message );
            }
        }
    }
}
