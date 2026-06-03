using SNESassetsWPF.Formats;
using SNESassetsWPF.Models;

namespace SNESassetsWPF.Services
{
    /// <summary>
    /// Parses raw S‑CG‑CAD SCR file bytes into a ScrFile object.
    /// Supports 32×32, 64×32, 32×64, and 64×64 layouts.
    /// Reads tilemap, footer, and visibility mask in correct order.
    /// </summary>
    public static class ScrFileParser
    {
        private const int BlockTileSize  = 32;
        private const int TilesPerBlock  = BlockTileSize * BlockTileSize; // 1024
        private const int BytesPerTile   = 2;
        private const int BlockSizeBytes = TilesPerBlock * BytesPerTile;  // 2048

        /// <summary>
        /// Parses an SCR file from raw bytes.
        /// </summary>
        public static ScrFile Parse(byte[] data , int widthTiles , int heightTiles , int blockCount)
        {
            var scr = new ScrFile(data, widthTiles, heightTiles, blockCount);

            // --- Tilemap region ---
            if ( blockCount == 1 )
            {
                ParseBlock( scr , data , 0 , 0 , 0 );
            }
            else if ( blockCount == 2 )
            {
                // Default: horizontal layout (64×32)
                ParseBlock( scr , data , 0 , 0 , 0 );
                ParseBlock( scr , data , 1 , 32 , 0 );
            }
            else if ( blockCount == 4 )
            {
                ParseBlock( scr , data , 0 , 0 , 0 );
                ParseBlock( scr , data , 1 , 32 , 0 );
                ParseBlock( scr , data , 2 , 0 , 32 );
                ParseBlock( scr , data , 3 , 32 , 32 );
            }

            // --- Footer (0x100 bytes) ---
            int tilemapBytes = blockCount * BlockSizeBytes;
            int footerOffset = tilemapBytes;
            int footerSize   = 0x100;

            scr.Footer = new byte[footerSize];
            System.Buffer.BlockCopy( data , footerOffset , scr.Footer , 0 , footerSize );

            // --- Visibility mask ---
            int visibilityOffset = footerOffset + footerSize;
            int totalTiles = widthTiles * heightTiles;
            int visibilityBytes = blockCount * 0x80; // total visibility section size

            // Read visibility as continuous bitstream across full screen
            scr.Visibility = new bool[1][] { new bool[totalTiles] };

            for ( int i = 0 ; i < totalTiles ; i++ )
            {
                int byteIndex = i >> 3;   // i / 8
                int bitIndex  = i & 7;    // i % 8

                if ( byteIndex >= visibilityBytes )
                    break;

                byte visByte = data[visibilityOffset + byteIndex];

                // MSB-first bit order (bit 7 = tile 0)
                bool visible = ((visByte >> (7 - bitIndex)) & 1) != 0;
                scr.Visibility[0][i] = visible;

                int gx = i % widthTiles;
                int gy = i / widthTiles;

                scr.Tiles[gy , gx].Visible = visible;
            }

            return scr;
        }

        // ─────────────────────────────────────────────────────────────
        //  BLOCK PARSING
        // ─────────────────────────────────────────────────────────────

        private static void ParseBlock(ScrFile scr , byte[] data , int blockIndex , int baseX , int baseY)
        {
            int blockOffset = blockIndex * BlockSizeBytes;

            for ( int i = 0 ; i < TilesPerBlock ; i++ )
            {
                int localX = i % BlockTileSize;
                int localY = i / BlockTileSize;

                int globalX = baseX + localX;
                int globalY = baseY + localY;

                ushort raw = ReadWord(data, blockOffset + (i * BytesPerTile));
                var tile = DecodeTile(raw);

                scr.Tiles[globalY , globalX] = tile;
                scr.Blocks[blockIndex].Tiles[localY , localX] = tile;
            }
        }

        // ─────────────────────────────────────────────────────────────
        //  HELPERS
        // ─────────────────────────────────────────────────────────────

        /// <summary>
        /// Reads a 16‑bit word (little‑endian).
        /// </summary>
        private static ushort ReadWord(byte[] data , int offset)
        {
            return (ushort)( data[offset] | ( data[offset + 1] << 8 ) );
        }

        /// <summary>
        /// Decodes a single SCR tile word into a ScrTile.
        /// </summary>
        private static ScrTile DecodeTile(ushort raw)
        {
            return new ScrTile
            {
                Raw = raw ,
                TileIndex = raw & 0x03FF ,
                PaletteIndex = ( raw >> 10 ) & 0x07 ,
                Priority = ( raw & 0x2000 ) != 0 ,
                HFlip = ( raw & 0x4000 ) != 0 ,
                VFlip = ( raw & 0x8000 ) != 0 ,
                Visible = true // overridden by visibility mask
            };
        }
    }
}
