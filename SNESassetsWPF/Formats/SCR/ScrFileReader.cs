using System;
using System.IO;
using SNESassetsWPF.Services;

namespace SNESassetsWPF.Formats
{
    public static class ScrFileReader
    {
        private const int BlockTileSize  = 32;
        private const int TilesPerBlock  = BlockTileSize * BlockTileSize; // 1024
        private const int BytesPerTile   = 2;
        private const int BlockSizeBytes = TilesPerBlock * BytesPerTile;  // 2048

        public static ScrFileReadResult Load(string path)
        {
            try
            {
                if ( !File.Exists( path ) )
                    return ScrFileReadResult.Fail( "SCR file not found." );

                byte[] data = File.ReadAllBytes(path);
                int length = data.Length;

                int blockCount;
                int widthTiles;
                int heightTiles;
                int visibilityBytes;

                // Determine layout by file size
                switch ( length )
                {
                    case 0x0980: // 1 block (32×32)
                        blockCount = 1;
                        widthTiles = 32;
                        heightTiles = 32;
                        visibilityBytes = 0x80;
                        break;

                    case 0x1200: // 2 blocks (64×32 or 32×64)
                        blockCount = 2;
                        visibilityBytes = 0x100;

                        // Default to 64×32 (horizontal); parser may refine if needed
                        widthTiles = 64;
                        heightTiles = 32;
                        break;

                    case 0x2300: // 4 blocks (64×64)
                        blockCount = 4;
                        widthTiles = 64;
                        heightTiles = 64;
                        visibilityBytes = 0x200;
                        break;

                    default:
                        return ScrFileReadResult.Fail(
                            $"Invalid SCR size: {length} bytes (expected 0x0980, 0x1200, or 0x2300)."
                        );
                }

                System.Diagnostics.Debug.WriteLine(
                    $"SCR size: {length} bytes → {widthTiles}×{heightTiles} tiles ({blockCount} blocks)"
                );

                // Parse SCR (tilemaps + footer + visibility)
                var scr = ScrFileParser.Parse(data, widthTiles, heightTiles, blockCount);

                return new ScrFileReadResult
                {
                    Success = true ,
                    Scr = scr ,
                    RawFile = data ,
                    WidthTiles = widthTiles ,
                    HeightTiles = heightTiles ,
                    BlockCount = blockCount ,
                    VisibilityBytes = visibilityBytes
                };
            }
            catch ( Exception ex )
            {
                return ScrFileReadResult.Fail( ex.Message );
            }
        }
    }
}
