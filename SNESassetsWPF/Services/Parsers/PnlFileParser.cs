using System;
using SNESassetsWPF.Formats;
using SNESassetsWPF.Models;

namespace SNESassetsWPF.Services
{
    /// <summary>
    /// Parses raw PNL file bytes into a structured PnlFile model.
    /// This class performs all decoding of header fields, tile attributes,
    /// tile flags, and computed metadata such as meta‑tile size.
    /// </summary>
    public static class PnlFileParser
    {
        /// <summary>
        /// Parses a PNL file from raw byte data.
        /// </summary>
        public static PnlFileReadResult Parse(byte[] data)
        {
            try
            {
                if ( data == null || data.Length < 0x10100 )
                    return PnlFileReadResult.Fail( "PNL file is too small to be valid." );

                var pnl = new PnlFile
                {
                    Header = new byte[0x100],
                    Tiles = new PnlTile[PnlFile.PanelWidth, PnlFile.PanelHeight]
                };

                // Copy header
                Buffer.BlockCopy( data , 0 , pnl.Header , 0 , 0x100 );

                // Compute meta‑tile size from header
                pnl.MetaWidth = 1 << ( pnl.Header[0x69] & 0x1F );
                pnl.MetaHeight = 1 << ( pnl.Header[0x6A] & 0x1F );

                // Mode 7 UI flag
                pnl.Mode7Enabled = pnl.Header[0x61] != 0;

                // Offsets for the two 0x4000‑word tables
                int attrOffset = 0x100;
                int flagOffset = attrOffset + 0x8000;

                int index = 0;

                // Parse 32×512 = 16384 tiles
                for ( int y = 0 ; y < PnlFile.PanelHeight ; y++ )
                {
                    for ( int x = 0 ; x < PnlFile.PanelWidth ; x++ )
                    {
                        // Attribute word (big‑endian)
                        ushort attr = (ushort)((data[attrOffset + index] << 8) |
                                                data[attrOffset + index + 1]);

                        // Flag word (big‑endian)
                        ushort flag = (ushort)((data[flagOffset + index] << 8) |
                                               data[flagOffset + index + 1]);

                        index += 2;

                        var tile = new PnlTile
                        {
                            RawAttributeWord = attr,
                            RawFlagWord = flag,

                            TileId = attr & 0x03FF,
                            PaletteRow = (attr >> 10) & 0x07,
                            Priority = ((attr >> 13) & 0x01) != 0,
                            HFlip = ((attr >> 14) & 0x01) != 0,
                            VFlip = ((attr >> 15) & 0x01) != 0,

                            Present = ((flag >> 15) & 0x01) != 0
                        };

                        pnl.Tiles[x , y] = tile;
                    }
                }

                return PnlFileReadResult.Ok( pnl );
            }
            catch ( Exception ex )
            {
                return PnlFileReadResult.Fail( "Exception while parsing PNL: " + ex.Message );
            }
        }
    }
}
