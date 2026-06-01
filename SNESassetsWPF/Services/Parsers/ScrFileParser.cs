using SNESassetsWPF.Enums;
using SNESassetsWPF.Formats;
using SNESassetsWPF.Models;

namespace SNESassetsWPF.Services
{
    /// <summary>
    /// Parses raw SCR file bytes into a ScrFile object.
    ///
    /// According to the RetroReversing S‑CG‑CAD documentation:
    /// - SCR files contain tilemap data exported by the S‑CG‑CAD editor.
    /// - Data is stored as big‑endian 16‑bit tile words.
    /// - A full SCR contains 4 blocks of 32×32 tiles (each 2048 bytes).
    /// - Blocks are arranged in a 2×2 grid:
    ///
    ///       Block 0 | Block 1
    ///       --------+--------
    ///       Block 2 | Block 3
    ///
    /// - Some SCR files contain only a single 32×32 block.
    ///
    /// This parser follows the S‑CG‑CAD SCR format *exactly as documented*
    /// by RetroReversing — not generic SNES hardware tilemap rules.
    /// </summary>
    public static class ScrFileParser
    {
        private const int BlockTileSize   = 32;
        private const int TilesPerBlock   = BlockTileSize * BlockTileSize; // 1024
        private const int BytesPerTile    = 2;                             // 16‑bit word
        private const int BlockSizeBytes  = TilesPerBlock * BytesPerTile;  // 2048
        private const int FullBlockCount  = 4;
        private const int FullScrTileSize = BlockTileSize * 2;             // 64×64 tiles

        /// <summary>
        /// Parses an SCR tilemap from raw bytes.
        /// The caller provides widthTiles/heightTiles (32 or 64),
        /// determined by ScrFileReader based on block count.
        /// </summary>
        public static ScrFile Parse(byte[] data , int widthTiles , int heightTiles)
        {
            // ScrFile now requires a constructor
            var scr = new ScrFile(data, widthTiles, heightTiles);

            int totalBlocks = data.Length / BlockSizeBytes;

            if ( totalBlocks == 1 )
                ParseSingleBlock( scr , data );
            else
                ParseFourBlocks( scr , data );

            return scr;
        }

        /// <summary>
        /// Debug switch between Little and Big Endian
        /// </summary>
        public static ScrEndian DebugEndianMode { get; set; } = ScrEndian.LittleEndian;


        // ─────────────────────────────────────────────────────────────
        //  SINGLE BLOCK PARSER (32×32)
        // ─────────────────────────────────────────────────────────────

        /// <summary>
        /// Parses a simple 32×32 SCR file (one block).
        /// </summary>
        private static void ParseSingleBlock(ScrFile scr , byte[] data)
        {
            int offset = 0;

            for ( int y = 0 ; y < BlockTileSize ; y++ )
            {
                for ( int x = 0 ; x < BlockTileSize ; x++ )
                {
                    ushort raw = ReadWord(data, offset);
                    offset += BytesPerTile;

                    scr.Tiles[y , x] = DecodeTile( raw );
                }
            }
        }

        // ─────────────────────────────────────────────────────────────
        //  FOUR BLOCK PARSER (64×64)
        // ─────────────────────────────────────────────────────────────

        /// <summary>
        /// Parses a full 64×64 SCR file composed of 4 blocks.
        /// Blocks are arranged in a 2×2 grid as documented by RetroReversing:
        ///
        ///   0 | 1
        ///   -----
        ///   2 | 3
        /// </summary>
        private static void ParseFourBlocks(ScrFile scr , byte[] data)
        {
            for ( int block = 0 ; block < FullBlockCount ; block++ )
            {
                // Determine block position in the 2×2 grid
                int blockX = (block % 2) * BlockTileSize;
                int blockY = (block / 2) * BlockTileSize;

                // Byte offset of this block in the SCR file
                int blockOffset = block * BlockSizeBytes;

                for ( int i = 0 ; i < TilesPerBlock ; i++ )
                {
                    int localX = i % BlockTileSize;
                    int localY = i / BlockTileSize;

                    int globalX = blockX + localX;
                    int globalY = blockY + localY;

                    int tileOffset = blockOffset + (i * BytesPerTile);

                    ushort raw = ReadWord(data, tileOffset);
                    scr.Tiles[globalY , globalX] = DecodeTile( raw );
                }
            }
        }

        // ─────────────────────────────────────────────────────────────
        //  HELPERS
        // ─────────────────────────────────────────────────────────────

        /// <summary>
        /// Reads a 16‑bit word from the byte array.
        /// S‑CG‑CAD SCR files store tilemap entries big‑endian.
        /// For debug reasons we can switch Endianess
        /// </summary>
        private static ushort ReadWord(byte[] data , int offset)
        {
            if ( DebugEndianMode == ScrEndian.BigEndian )
            {
                return (ushort)( ( data[offset] << 8 ) | data[offset + 1] );
            }
            else
            {
                return (ushort)( data[offset] | ( data[offset + 1] << 8 ) );
            }
        }


        /// <summary>
        /// Decodes a single SCR tile word into a ScrTile.
        ///
        /// According to RetroReversing's S‑CG‑CAD SCR specification:
        ///
        ///   FEDC BA98 7654 3210
        ///   VHPP PTTT TTTT TTTT
        ///
        /// Bits:
        ///   15     = VFlip
        ///   14     = HFlip
        ///   13     = Priority
        ///   12‑10  = Palette group (0–7)
        ///   9‑0    = Tile index (0–1023)
        ///
        /// This is the editor‑side S‑CG‑CAD tilemap format,
        /// not generic SNES hardware tilemap rules.
        /// </summary>
        private static ScrTile DecodeTile(ushort raw)
        {
            return new ScrTile
            {
                TileIndex = raw & 0x03FF ,          // bits 0–9
                PaletteIndex = ( raw >> 10 ) & 0x07 ,    // bits 10–12
                Priority = ( raw & 0x2000 ) != 0 ,   // bit 13
                HFlip = ( raw & 0x4000 ) != 0 ,   // bit 14
                VFlip = ( raw & 0x8000 ) != 0    // bit 15
            };
        }
    }
}
