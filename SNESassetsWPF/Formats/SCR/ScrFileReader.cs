using System;
using System.IO;
using SNESassetsWPF.Services;

namespace SNESassetsWPF.Formats
{
    public static class ScrFileReader
    {
        private const int BlockTileSize   = 32;
        private const int TilesPerBlock   = BlockTileSize * BlockTileSize; // 1024
        private const int BytesPerTile    = 2;
        private const int BlockSizeBytes  = TilesPerBlock * BytesPerTile;  // 2048
        private const int FullBlockCount  = 4;
        private const int FullScrTileSize = BlockTileSize * 2;             // 64

        public static ScrFileReadResult Load(string path)
        {
            try
            {
                if ( !File.Exists( path ) )
                    return ScrFileReadResult.Fail( "SCR file not found." );

                byte[] data = File.ReadAllBytes(path);
                int length = data.Length;

                // Determine block count
                int totalBlocks = length / BlockSizeBytes;

                int widthTiles;
                int heightTiles;

                if ( totalBlocks == FullBlockCount )
                {
                    widthTiles = FullScrTileSize;
                    heightTiles = FullScrTileSize;
                }
                else if ( totalBlocks == 1 )
                {
                    widthTiles = BlockTileSize;
                    heightTiles = BlockTileSize;
                }
                else
                {
                    return ScrFileReadResult.Fail(
                        $"Invalid SCR size: {length} bytes ({totalBlocks} blocks)."
                    );
                }

                System.Diagnostics.Debug.WriteLine(
                    $"SCR size: {length} bytes → {widthTiles}×{heightTiles} tiles" );

                // Parse SCR
                var scr = ScrFileParser.Parse(data, widthTiles, heightTiles);

                // Build result
                return new ScrFileReadResult
                {
                    Success = true ,
                    Scr = scr ,
                    RawFile = data ,
                    WidthTiles = widthTiles ,
                    HeightTiles = heightTiles ,
                    BlockCount = totalBlocks
                };
            }
            catch ( Exception ex )
            {
                return ScrFileReadResult.Fail( ex.Message );
            }
        }
    }
}
