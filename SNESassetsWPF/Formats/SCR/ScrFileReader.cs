using System;
using System.IO;
using SNESassetsWPF.Services;

namespace SNESassetsWPF.Formats
{
    /// <summary>
    /// Loads an SCR file from disk and passes the raw bytes
    /// to the ScrFileParser service. Handles both simple 32×32 maps
    /// and full 64×64 S‑CAD SCR containers (4 blocks of 32×32 tiles).
    /// </summary>
    public static class ScrFileReader
    {
        // ─────────────────────────────────────────────────────────────
        //  SCR CONSTANTS
        // ─────────────────────────────────────────────────────────────

        /// <summary>
        /// Width/height of a single SCR tilemap block (in tiles).
        /// </summary>
        private const int BlockTileSize = 32;

        /// <summary>
        /// Number of tiles in a single 32×32 block.
        /// </summary>
        private const int TilesPerBlock = BlockTileSize * BlockTileSize; // 1024

        /// <summary>
        /// Size of a single tile entry in bytes (16‑bit word).
        /// </summary>
        private const int BytesPerTile = 2;

        /// <summary>
        /// Size of a single SCR block in bytes.
        /// </summary>
        private const int BlockSizeBytes = TilesPerBlock * BytesPerTile; // 2048

        /// <summary>
        /// Number of blocks in a full S‑CAD SCR file.
        /// </summary>
        private const int FullBlockCount = 4;

        /// <summary>
        /// Width/height of a full SCR composed of 4 blocks (in tiles).
        /// </summary>
        private const int FullScrTileSize = BlockTileSize * 2; // 64


        // ─────────────────────────────────────────────────────────────
        //  PUBLIC API
        // ─────────────────────────────────────────────────────────────

        /// <summary>
        /// Loads and parses an SCR file.
        /// Automatically detects whether the file contains one or four 32×32 blocks.
        /// </summary>
        public static ScrFileReadResult Load(string path)
        {
            try
            {
                if ( !File.Exists( path ) )
                    return ScrFileReadResult.Fail( "SCR file not found." );

                byte[] data = File.ReadAllBytes(path);

                // Determine how many 32×32 blocks are present.
                int totalBlocks = data.Length / BlockSizeBytes;

                int widthTiles;
                int heightTiles;

                // S‑CAD SCR files normally contain 4 blocks (64×64 tiles total).
                if ( totalBlocks >= FullBlockCount )
                {
                    widthTiles = FullScrTileSize;
                    heightTiles = FullScrTileSize;
                }
                else
                {
                    // Fallback for partial or simplified SCRs.
                    widthTiles = BlockTileSize;
                    heightTiles = BlockTileSize;
                }
                System.Diagnostics.Debug.WriteLine(
                    $"SCR size: {data.Length} bytes → {widthTiles}×{heightTiles} tiles" );

                // Diagnostic output.
                System.Diagnostics.Debug.WriteLine(
                    $"SCR detected {totalBlocks} block(s) → {widthTiles}×{heightTiles} tiles " +
                    $"({widthTiles * 8}×{heightTiles * 8} px)" );

                // Parse using the inferred dimensions.
                var scr = ScrFileParser.Parse(data, widthTiles, heightTiles);

                return ScrFileReadResult.Ok( scr );
            }
            catch ( Exception ex )
            {
                return ScrFileReadResult.Fail( ex.Message );
            }
        }
    }
}
