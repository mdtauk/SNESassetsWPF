using System;
using SNESassetsWPF.Models;

namespace SNESassetsWPF.Formats
{
    public class PnlFileParser
    {
        /// <summary>
        /// Parses raw PNL bytes into a PnlFile object.
        /// </summary>
        public PnlFile Parse(byte[] data)
        {
            if ( data == null || data.Length < 0x100 + 0x8000 + 0x8000 )
                throw new ArgumentException( "Invalid PNL data." );

            var pnl = new PnlFile();

            // ---------------------------------------------------------
            // 1. Copy header
            // ---------------------------------------------------------
            Buffer.BlockCopy( data , 0 , pnl.Header , 0 , 0x100 );

            // ---------------------------------------------------------
            // 2. Decode header fields
            // ---------------------------------------------------------
            pnl.IsMode7Enabled = ( pnl.Header[0x61] & 0x80 ) != 0;

            pnl.MetaWidth = 1 << ( pnl.Header[0x69] & 0x1F );
            pnl.MetaHeight = 1 << ( pnl.Header[0x6A] & 0x1F );

            // ---------------------------------------------------------
            // 3. Tile table + flag table offsets
            // ---------------------------------------------------------
            int tileTableOffset = 0x100;
            int flagTableOffset = tileTableOffset + 0x8000;

            // ---------------------------------------------------------
            // 4. Parse all 16384 tiles
            // ---------------------------------------------------------
            for ( int i = 0 ; i < pnl.PnlTiles.Length ; i++ )
            {
                var tile = new PnlTile();

                // -----------------------------
                // Read raw attribute word (big endian)
                // -----------------------------
                ushort rawAttr = (ushort)(
                    (data[tileTableOffset + i * 2] << 8) |
                     data[tileTableOffset + i * 2 + 1]
                );

                tile.RawAttributeWord = rawAttr;

                // -----------------------------
                // Decode attribute bits
                // -----------------------------
                tile.TileId = rawAttr & 0x03FF;          // bits 0–9
                tile.PaletteRow = ( rawAttr >> 10 ) & 0x07;    // bits 10–12
                tile.Priority = ( rawAttr & 0x2000 ) != 0;   // bit 13
                tile.HFlip = ( rawAttr & 0x4000 ) != 0;   // bit 14
                tile.VFlip = ( rawAttr & 0x8000 ) != 0;   // bit 15

                // -----------------------------
                // Read raw flag word (big endian)
                // -----------------------------
                ushort rawFlag = (ushort)(
                    (data[flagTableOffset + i * 2] << 8) |
                     data[flagTableOffset + i * 2 + 1]
                );

                tile.RawFlagWord = rawFlag;

                // -----------------------------
                // Decode present flag
                // -----------------------------
                tile.IsPresent = ( rawFlag & 0x8000 ) != 0;

                // -----------------------------
                // Store tile
                // -----------------------------
                pnl.PnlTiles[i] = tile;
            }

            return pnl;
        }
    }
}
