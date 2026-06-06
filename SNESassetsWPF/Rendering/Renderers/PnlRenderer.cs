using System;
using System.Diagnostics;
using System.Windows.Media;
using SNESassetsWPF.Formats;
using SNESassetsWPF.Models;

namespace SNESassetsWPF.Rendering
{
    public class PnlRenderer
    {
        private const int TileWidth    = 8;
        private const int TileHeight   = 8;
        private const int TilesPerRow  = 32;
        private const int ColorsPerRow = 16;

        public RenderResult Render(
            PnlFile pnl ,
            CgxFile cgx ,
            ColFile col ,
            int zoom = 1 ,
            bool forceVisible = false ,
            bool showGrid = false ,
            bool debugMode = false ,
            bool debugTint = false)
        {
            if ( pnl == null || cgx == null || col == null )
                throw new ArgumentNullException( "PNL/CGX/COL must not be null." );

            if ( zoom < 1 )
                zoom = 1;

            int spacing = (showGrid && zoom >= 2) ? 1 : 0;

            int tileCount = pnl.Tiles.Length;
            int rows      = tileCount / TilesPerRow;

            int baseWidth  = TilesPerRow * TileWidth;
            int baseHeight = rows       * TileHeight;

            int width  = (baseWidth  * zoom) + ((TilesPerRow - 1) * spacing);
            int height = (baseHeight * zoom) + ((rows        - 1) * spacing);

            var buffer = new byte[width * height * 4];

            // ------------------------------------------------------------
            // Render each PNL tile
            // ------------------------------------------------------------
            for ( int i = 0 ; i < tileCount ; i++ )
            {
                var t = pnl.Tiles[i];
                if ( t == null )
                    continue;

                if ( !forceVisible && !t.IsVisible )
                    continue;

                int tileIndex = t.TileIndex;
                if ( tileIndex < 0 || tileIndex >= cgx.TileCount )
                    continue;

                CgxTile cgxTile = cgx.Tiles[tileIndex];
                byte[,] pixels  = cgxTile.Pixels;

                int paletteGroup = t.PaletteRow & 0x0F;

                int tileX = i % TilesPerRow;
                int tileY = i / TilesPerRow;

                // IMPORTANT: include spacing
                int tilePixelX = (tileX * TileWidth  * zoom) + (tileX * spacing);
                int tilePixelY = (tileY * TileHeight * zoom) + (tileY * spacing);

                for ( int y = 0 ; y < TileHeight ; y++ )
                {
                    for ( int x = 0 ; x < TileWidth ; x++ )
                    {
                        int sx = t.HFlip ? (TileWidth  - 1 - x) : x;
                        int sy = t.VFlip ? (TileHeight - 1 - y) : y;

                        byte baseIndex = pixels[sy, sx];

                        int paletteIndex = ComputePaletteIndex(
                            cgx.BitDepth,
                            paletteGroup,
                            baseIndex
                        );

                        Color c = ResolveColor(col, paletteIndex);

                        int destX0 = tilePixelX + (x * zoom);
                        int destY0 = tilePixelY + (y * zoom);

                        for ( int zy = 0 ; zy < zoom ; zy++ )
                        {
                            int destY     = destY0 + zy;
                            int rowOffset = destY * width * 4;

                            for ( int zx = 0 ; zx < zoom ; zx++ )
                            {
                                int destX = destX0 + zx;
                                int idx   = rowOffset + destX * 4;

                                buffer[idx + 0] = c.B;
                                buffer[idx + 1] = c.G;
                                buffer[idx + 2] = c.R;
                                buffer[idx + 3] = 255;
                            }
                        }
                    }
                }

                // ------------------------------------------------------------
                // Debug glyph overlay (grid-aware)
                // ------------------------------------------------------------
                if ( debugMode && t.IsVisible )
                {
                    var glyph = ScrDebugGlyphs.GetGlyphForCgxIndex(tileIndex);

                    DebugOverlay.DrawGlyph8x8InTile(
                        buffer ,
                        width ,
                        height ,
                        tilePixelX ,          // pixel-based, spacing-aware
                        tilePixelY ,
                        TileWidth * zoom ,
                        TileHeight * zoom ,
                        glyph
                    );
                }
            }

            // ------------------------------------------------------------
            // Debug tint (apply ONCE)
            // ------------------------------------------------------------
            if ( debugTint )
            {
                DebugOverlay.ApplyTintToSurface(
                    buffer ,
                    width ,
                    height ,
                    Color.FromArgb( 128 , 128 , 128 , 255 ) ,
                    0.25
                );
            }

            // ------------------------------------------------------------
            // Debug pattern overlays (grid-aware)
            // ------------------------------------------------------------
            if ( debugMode )
            {
                int gw = pnl.GroupWidth;
                int gh = pnl.GroupHeight;

                int tilesPerGroup = gw * gh;
                int groupCount = tileCount / tilesPerGroup;

                int groupsPerRow = TilesPerRow / gw;

                for ( int g = 0 ; g < groupCount ; g++ )
                {
                    bool groupVisible = false;

                    for ( int t = 0 ; t < tilesPerGroup ; t++ )
                    {
                        int tileIndex = g * tilesPerGroup + t;
                        if ( pnl.Tiles[tileIndex].IsVisible )
                        {
                            groupVisible = true;
                            break;
                        }
                    }

                    if ( !groupVisible )
                        continue;

                    int gx = g % groupsPerRow;
                    int gy = g / groupsPerRow;

                    var pattern = new DebugPattern
                    {
                        GridX = gx * gw,
                        GridY = gy * gh,
                        WidthInTiles  = gw,
                        HeightInTiles = gh,
                        OuterColor = Colors.White,
                        InnerColor = DebugColors.GetColorForPnlTile(g),
                        PatternIndex = g
                    };

                    DebugOverlay.DrawPatternRegion(
                        buffer ,
                        width ,
                        height ,
                        TileWidth * zoom ,
                        TileHeight * zoom ,
                        spacing ,            // NEW: spacing-aware
                        pattern
                    );
                }
            }

            // ------------------------------------------------------------
            // Draw grid lines (optional)
            // ------------------------------------------------------------
            if ( showGrid && zoom >= 2 )
            {
                byte gridR = 128;
                byte gridG = 128;
                byte gridB = 128;
                byte gridA = 255;

                // Vertical grid lines
                for ( int tx = 1 ; tx < TilesPerRow ; tx++ )
                {
                    int x = (tx * TileWidth * zoom) + ((tx - 1) * spacing);

                    for ( int y = 0 ; y < height ; y++ )
                    {
                        int idx = (y * width + x) * 4;
                        buffer[idx + 0] = gridB;
                        buffer[idx + 1] = gridG;
                        buffer[idx + 2] = gridR;
                        buffer[idx + 3] = gridA;
                    }
                }

                // Horizontal grid lines
                for ( int ty = 1 ; ty < rows ; ty++ )
                {
                    int y = (ty * TileHeight * zoom) + ((ty - 1) * spacing);

                    int rowOffset = y * width * 4;
                    for ( int x = 0 ; x < width ; x++ )
                    {
                        int idx = rowOffset + x * 4;
                        buffer[idx + 0] = gridB;
                        buffer[idx + 1] = gridG;
                        buffer[idx + 2] = gridR;
                        buffer[idx + 3] = gridA;
                    }
                }
            }

            return new RenderResult
            {
                Buffer = buffer ,
                Width = width ,
                Height = height
            };
        }

        private static int ComputePaletteIndex(int bitDepth , int paletteGroup , byte baseIndex)
        {
            return bitDepth switch
            {
                2 => ( paletteGroup << 2 ) | ( baseIndex & 0x03 ),
                4 => ( paletteGroup << 4 ) | ( baseIndex & 0x0F ),
                8 => baseIndex,
                _ => baseIndex
            };
        }

        private static Color ResolveColor(ColFile col , int paletteIndex)
        {
            int row    = paletteIndex / ColorsPerRow;
            int colIdx = paletteIndex % ColorsPerRow;

            return col.GetColor( row , colIdx );
        }
    }
}
