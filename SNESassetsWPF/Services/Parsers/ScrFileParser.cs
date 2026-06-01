using SNESassetsWPF.Models;
using SNESassetsWPF.Formats;

namespace SNESassetsWPF.Services
{
    /// <summary>
    /// Parses raw SCR file bytes into a ScrFile object.
    /// S‑CG‑CAD SCR format:
    /// - 4 blocks of 32×32 tiles (each 2048 bytes)
    /// - Big‑endian 16‑bit tile words
    /// - Blocks arranged as:
    ///       Block 0 | Block 1
    ///       --------+--------
    ///       Block 2 | Block 3
    /// </summary>
    public static class ScrFileParser
    {
        private const int BlockTileSize   = 32;
        private const int TilesPerBlock   = BlockTileSize * BlockTileSize; // 1024
        private const int BytesPerTile    = 2;
        private const int BlockSizeBytes  = TilesPerBlock * BytesPerTile;  // 2048
        private const int FullBlockCount  = 4;
        private const int FullScrTileSize = BlockTileSize * 2;             // 64

        /// <summary>
        /// Parses an SCR tilemap from raw bytes.
        /// Automatically handles 32×32 and full 64×64 S‑CG‑CAD SCR files.
        /// </summary>
        public static ScrFile Parse(byte[] data , int widthTiles , int heightTiles)
        {
            var scr = new ScrFile
            {
                WidthTiles  = widthTiles,
                HeightTiles = heightTiles,
                Tiles       = new ScrTile[heightTiles, widthTiles]
            };

            int totalBlocks = data.Length / BlockSizeBytes;

            if ( totalBlocks < FullBlockCount )
            {
                ParseSingleBlock( scr , data );
            }
            else
            {
                ParseFourBlocks( scr , data );
            }

            return scr;
        }

        // ─────────────────────────────────────────────────────────────
        //  SINGLE BLOCK PARSER (32×32)
        // ─────────────────────────────────────────────────────────────

        private static void ParseSingleBlock(ScrFile scr , byte[] data)
        {
            int offset = 0;

            for ( int y = 0 ; y < BlockTileSize ; y++ )
            {
                for ( int x = 0 ; x < BlockTileSize ; x++ )
                {
                    ushort raw = ReadBigEndianWord(data, offset);
                    offset += BytesPerTile;

                    scr.Tiles[y , x] = DecodeTile( raw );
                }
            }
        }

        // ─────────────────────────────────────────────────────────────
        //  FOUR BLOCK PARSER (64×64)
        // ─────────────────────────────────────────────────────────────

        private static void ParseFourBlocks(ScrFile scr , byte[] data)
        {
            for ( int block = 0 ; block < FullBlockCount ; block++ )
            {
                int blockX     = (block % 2) * BlockTileSize;
                int blockY     = (block / 2) * BlockTileSize;
                int blockOffset = block * BlockSizeBytes;

                for ( int i = 0 ; i < TilesPerBlock ; i++ )
                {
                    int localX = i % BlockTileSize;
                    int localY = i / BlockTileSize;

                    int globalX = blockX + localX;
                    int globalY = blockY + localY;

                    int tileOffset = blockOffset + (i * BytesPerTile);

                    ushort raw = ReadBigEndianWord(data, tileOffset);
                    ScrTile tile = DecodeTile(raw);

                    if ( globalY == 0 && ( globalX == 0 || globalX == 1 || globalX == 17 || globalX == 18 ) )
                    {
                        System.Diagnostics.Debug.WriteLine(
                            $"GREEN raw=0x{raw:X4} at SCR[{globalY},{globalX}]" );
                    }

                    // Replace with the tile coordinates that show the green background tile
                    int targetX = 4;   // example column
                    int targetY = 12;  // example row

                    if ( globalX == targetX && globalY == targetY )
                    {
                        System.Diagnostics.Debug.WriteLine(
                            $"GREEN_TILE raw=0x{raw:X4} tile={tile.TileIndex:X3}" );
                    }

                    // DEBUG: print first few tiles of each block
                    if ( globalY < 4 && globalX < 8 )   // top-left 8×4 region
                    {
                        System.Diagnostics.Debug.WriteLine(
                            $"SCR[{globalY},{globalX}] raw=0x{raw:X4} " +
                            $"tile={tile.TileIndex:X3} pal={tile.PaletteIndex} " +
                            $"H={tile.HFlip} V={tile.VFlip} P={tile.Priority}" );
                    }

                    scr.Tiles[globalY , globalX] = tile;
                }
            }
        }

        // ─────────────────────────────────────────────────────────────
        //  HELPERS
        // ─────────────────────────────────────────────────────────────

        /// <summary>
        /// Reads a 16‑bit big‑endian word from the byte array.
        /// S‑CG‑CAD SCR files store tilemap words big‑endian.
        /// </summary>
        private static ushort ReadBigEndianWord(byte[] data , int offset)
        {
            return (ushort)( ( data[offset] << 8 ) | data[offset + 1] );
        }




        private static ScrTile DecodeTile(ushort raw)
        {
            return new ScrTile
            {
                //TileIndex = ( raw >> 4 ) & 0x03FF ,
                TileIndex = 0 ,   // TEMPORARY: we want raw values only
                PaletteIndex = 0 ,
                Priority = false ,
                HFlip = ( raw & 0x4000 ) != 0 ,
                VFlip = ( raw & 0x8000 ) != 0
            };
        }


    }
}
