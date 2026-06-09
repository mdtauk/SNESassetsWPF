using SNESassetsWPF.Enums;
using SNESassetsWPF.Formats;
using SNESassetsWPF.Models;
using System.Windows.Media;

namespace SNESassetsWPF.Rendering
{
    public static class ScrRenderer
    {
        private const int TileWidth  = 8;
        private const int TileHeight = 8;

        public static RenderResult Render(
            ScrFile scr ,
            CgxFile cgx ,
            ColFile col ,
            int zoom = 1 ,
            bool showGrid = false ,
            bool showInvisible = false ,
            ScrDebugMode debugMode = ScrDebugMode.None)
        {
            if ( scr == null )
            {
                return new RenderResult
                {
                    Buffer = Array.Empty<byte>() ,
                    Width = 0 ,
                    Height = 0
                };
            }

            if ( zoom < 1 )
                zoom = 1;

            bool hasCgx = cgx != null;
            bool hasCol = col != null;

            int blocks = scr.BlockCount;

            int widthTiles  = (blocks == 1) ? 32 : 64;
            int heightTiles = (blocks <= 2) ? 32 : 64;

            int spacing = (showGrid && zoom >= 2) ? 1 : 0;

            int baseWidth  = widthTiles  * TileWidth;
            int baseHeight = heightTiles * TileHeight;

            int width  = (baseWidth  * zoom) + ((widthTiles  - 1) * spacing);
            int height = (baseHeight * zoom) + ((heightTiles - 1) * spacing);

            var buffer = new byte[width * height * 4];

            // ─────────────────────────────────────────────
            // 1. Base tile rendering (SCR → CGX → COL)
            // ─────────────────────────────────────────────
            for ( int block = 0 ; block < blocks ; block++ )
            {
                var scrBlock = scr.Blocks[block];
                if ( scrBlock == null )
                    continue;

                int blockBaseX = (block == 1 || block == 3) ? 32 : 0;
                int blockBaseY = (block >= 2) ? 32 : 0;

                for ( int i = 0 ; i < scrBlock.Entries.Length ; i++ )
                {
                    var entry = scrBlock.Entries[i];
                    if ( entry == null )
                        continue;

                    if ( !entry.IsVisible && !showInvisible )
                        continue;

                    int localX = i % 32;
                    int localY = i / 32;

                    int tileX = blockBaseX + localX;
                    int tileY = blockBaseY + localY;

                    int px = (tileX * TileWidth  * zoom) + (tileX * spacing);
                    int py = (tileY * TileHeight * zoom) + (tileY * spacing);

                    if ( !hasCgx || !hasCol )
                    {
                        DrawFallbackTile( buffer , width , height , px , py , zoom );
                        continue;
                    }

                    int cgxIndex = entry.TileIndex;
                    if ( cgxIndex < 0 || cgxIndex >= cgx.TileCount )
                    {
                        DrawFallbackTile( buffer , width , height , px , py , zoom );
                        continue;
                    }

                    var tile = cgx.Tiles[cgxIndex];
                    if ( tile == null )
                    {
                        DrawFallbackTile( buffer , width , height , px , py , zoom );
                        continue;
                    }

                    DrawCgxTile(
                        buffer ,
                        width ,
                        height ,
                        px ,
                        py ,
                        tile ,
                        col ,
                        entry.PaletteRow ,
                        entry.HFlip ,
                        entry.VFlip ,
                        cgx.BitDepth ,
                        zoom
                    );
                }
            }

            // ─────────────────────────────────────────────
            // 2. Debug tint (global)
            // ─────────────────────────────────────────────
            if ( debugMode != ScrDebugMode.None )
            {
                DebugOverlay.ApplyTintToSurface(
                    buffer ,
                    width ,
                    height ,
                    Color.FromRgb( 128 , 128 , 128 ) ,
                    0.4
                );
            }

            // ─────────────────────────────────────────────
            // 3. Debug patterns (per CGX tile)
            // ─────────────────────────────────────────────
            if ( debugMode != ScrDebugMode.None )
            {
                var patterns = BuildScrTilePatterns(widthTiles, heightTiles);

                DebugOverlay.DrawScrPatterns(
                    buffer ,
                    width ,
                    height ,
                    patterns ,
                    zoom ,
                    spacing ,
                    widthTiles
                );
            }

            // ─────────────────────────────────────────────
            // 4. Debug glyphs (per tile)
            // ─────────────────────────────────────────────
            if ( debugMode != ScrDebugMode.None )
            {
                for ( int block = 0 ; block < blocks ; block++ )
                {
                    var scrBlock = scr.Blocks[block];
                    if ( scrBlock == null )
                        continue;

                    int blockBaseX = (block == 1 || block == 3) ? 32 : 0;
                    int blockBaseY = (block >= 2) ? 32 : 0;

                    for ( int i = 0 ; i < scrBlock.Entries.Length ; i++ )
                    {
                        var entry = scrBlock.Entries[i];
                        if ( entry == null )
                            continue;

                        int localX = i % 32;
                        int localY = i / 32;

                        int tileX = blockBaseX + localX;
                        int tileY = blockBaseY + localY;

                        int tilePixelX = (tileX * TileWidth  * zoom) + (tileX * spacing);
                        int tilePixelY = (tileY * TileHeight * zoom) + (tileY * spacing);

                        switch ( debugMode )
                        {
                            case ScrDebugMode.TileIndex:
                            case ScrDebugMode.PaletteRowIndex:
                            case ScrDebugMode.Priority:
                                {
                                    var glyph = DebugGlyphs.GetGlyphForMode(debugMode, entry);
                                    if ( glyph != null )
                                    {
                                        int inset = 2; // fixed screen pixels
                                        int glyphScale = (zoom > 1) ? 2 : 1;

                                        DebugOverlay.DrawGlyph8x8InTile(
                                            buffer ,
                                            width ,
                                            height ,
                                            tilePixelX + inset ,
                                            tilePixelY + inset ,
                                            glyphScale ,
                                            glyph
                                        );
                                    }
                                    break;
                                }

                            case ScrDebugMode.Flip:
                                {
                                    int inset = 3 * zoom;

                                    // Vertical arrow (top-left)
                                    if ( entry.VFlip )
                                    {
                                        DebugOverlay.DrawGlyphCustom(
                                            buffer ,
                                            width ,
                                            height ,
                                            tilePixelX + inset ,
                                            tilePixelY + inset ,
                                            zoom ,
                                            DebugGlyphs.FlipVerticalArrow
                                        );
                                    }

                                    // Horizontal arrow (bottom-right)
                                    if ( entry.HFlip )
                                    {
                                        int gh = DebugGlyphs.FlipHorizontalArrow.GetLength(0) * zoom;
                                        int gw = DebugGlyphs.FlipHorizontalArrow.GetLength(1) * zoom;

                                        DebugOverlay.DrawGlyphCustom(
                                            buffer ,
                                            width ,
                                            height ,
                                            tilePixelX + ( TileWidth * zoom - gw - inset ) ,
                                            tilePixelY + ( TileHeight * zoom - gh - inset ) ,
                                            zoom ,
                                            DebugGlyphs.FlipHorizontalArrow
                                        );
                                    }

                                    break;
                                }
                        }
                    }
                }
            }

            // ─────────────────────────────────────────────
            // 5. Grid (on top) – currently disabled
            // ─────────────────────────────────────────────
            if ( showGrid && zoom >= 2 )
            {
                // DebugOverlay.DrawGrid(
                //     buffer,
                //     width,
                //     height,
                //     TileWidth * zoom,
                //     TileHeight * zoom,
                //     zoom,
                //     spacing,
                //     widthTiles,
                //     heightTiles
                // );
            }

            return new RenderResult
            {
                Buffer = buffer ,
                Width = width ,
                Height = height
            };
        }

        // ─────────────────────────────────────────────
        // Fallback tile (grey checkerboard)
        // ─────────────────────────────────────────────
        private static void DrawFallbackTile(
            byte[] buffer ,
            int width ,
            int height ,
            int px ,
            int py ,
            int zoom)
        {
            for ( int y = 0 ; y < TileHeight * zoom ; y++ )
            {
                for ( int x = 0 ; x < TileWidth * zoom ; x++ )
                {
                    int dx = px + x;
                    int dy = py + y;

                    if ( dx < 0 || dx >= width || dy < 0 || dy >= height )
                        continue;

                    bool dark = ((x / zoom) + (y / zoom)) % 2 == 0;
                    byte v = dark ? (byte)80 : (byte)140;

                    int idx = (dy * width + dx) * 4;
                    buffer[idx + 0] = v;
                    buffer[idx + 1] = v;
                    buffer[idx + 2] = v;
                    buffer[idx + 3] = 255;
                }
            }
        }

        // ─────────────────────────────────────────────
        // Per-tile debug patterns (borders via DebugColors)
        // ─────────────────────────────────────────────
        private static List<DebugPattern> BuildScrTilePatterns(int widthTiles , int heightTiles)
        {
            var list = new List<DebugPattern>();

            int groupsAcross = widthTiles;
            int groupsDown   = heightTiles;

            for ( int y = 0 ; y < heightTiles ; y++ )
            {
                for ( int x = 0 ; x < widthTiles ; x++ )
                {
                    int patternIndex = y * groupsAcross + x;
                    Color c = DebugColors.GetColorForPnlTile(patternIndex);

                    list.Add( new DebugPattern
                    {
                        GridX = x ,
                        GridY = y ,
                        WidthInTiles = 1 ,
                        HeightInTiles = 1 ,
                        OuterColor = Color.FromRgb( 128 , 128 , 128 ) ,
                        InnerColor = c ,
                        OuterBorderThickness = 1 ,
                        InnerBorderThickness = 1
                    } );
                }
            }

            return list;
        }

        // ─────────────────────────────────────────────
        // CGX tile drawing
        // ─────────────────────────────────────────────
        private static void DrawCgxTile(
            byte[] buffer ,
            int width ,
            int height ,
            int px ,
            int py ,
            CgxTile tile ,
            ColFile col ,
            int paletteRow ,
            bool hFlip ,
            bool vFlip ,
            int bitDepth ,
            int zoom)
        {
            var pixels = tile.Pixels;

            for ( int y = 0 ; y < TileHeight ; y++ )
            {
                for ( int x = 0 ; x < TileWidth ; x++ )
                {
                    int sx = hFlip ? (TileWidth - 1 - x) : x;
                    int sy = vFlip ? (TileHeight - 1 - y) : y;

                    byte baseIndex = pixels[sy, sx];

                    int paletteIndex = ComputePaletteIndex(bitDepth, paletteRow, baseIndex);
                    Color c = ResolveColor(col, paletteIndex);

                    int dx0 = px + x * zoom;
                    int dy0 = py + y * zoom;

                    for ( int zy = 0 ; zy < zoom ; zy++ )
                    {
                        int dy = dy0 + zy;
                        if ( dy < 0 || dy >= height )
                            continue;

                        int rowOffset = dy * width * 4;

                        for ( int zx = 0 ; zx < zoom ; zx++ )
                        {
                            int dx = dx0 + zx;
                            if ( dx < 0 || dx >= width )
                                continue;

                            int idx = rowOffset + dx * 4;
                            buffer[idx + 0] = c.B;
                            buffer[idx + 1] = c.G;
                            buffer[idx + 2] = c.R;
                            buffer[idx + 3] = 255;
                        }
                    }
                }
            }
        }

        private static int ComputePaletteIndex(int bitDepth , int paletteRow , byte baseIndex)
        {
            return bitDepth switch
            {
                2 => ( paletteRow << 2 ) | ( baseIndex & 0x03 ),
                4 => ( paletteRow << 4 ) | ( baseIndex & 0x0F ),
                8 => baseIndex,
                _ => baseIndex
            };
        }

        private static Color ResolveColor(ColFile col , int paletteIndex)
        {
            int row = paletteIndex / 16;
            int colIdx = paletteIndex % 16;
            return col.GetColor( row , colIdx );
        }

        public static RenderResult Empty { get; } = new RenderResult
        {
            Buffer = Array.Empty<byte>() ,
            Width = 0 ,
            Height = 0
        };
    }
}
