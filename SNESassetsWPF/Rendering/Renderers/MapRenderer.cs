using System;
using System.Diagnostics;
using System.Windows.Media;
using SNESassetsWPF.Formats;
using SNESassetsWPF.Models;

namespace SNESassetsWPF.Rendering
{
    public class MapRenderer
    {
        private const int TileWidth  = 8;
        private const int TileHeight = 8;
        private const int ColorsPerRow = 16;

        public RenderResult Render(
            MapFile map ,
            PnlFile pnl ,
            CgxFile cgx ,
            ColFile col ,
            int zoom = 1 ,
            bool showGrid = false ,
            bool debugMode = false ,
            bool debugTint = false)
        {
            if ( map == null )
                throw new ArgumentNullException( "MAP must not be null." );

            if ( pnl == null || cgx == null || col == null )
                throw new ArgumentNullException( "PNL/CGX/COL must not be null." );

            if ( zoom < 1 )
                zoom = 1;

            int spacing = (showGrid && zoom >= 2) ? 1 : 0;

            int gw = pnl.GroupWidth;
            int gh = pnl.GroupHeight;

            int groupPixelWidth  = gw * TileWidth  * zoom;
            int groupPixelHeight = gh * TileHeight * zoom;

            int width  = (map.Width  * groupPixelWidth)  + ((map.Width  - 1) * spacing);
            int height = (map.Height * groupPixelHeight) + ((map.Height - 1) * spacing);

            var buffer = new byte[width * height * 4];

            // ------------------------------------------------------------
            // Render each MAP cell
            // ------------------------------------------------------------
            for ( int my = 0 ; my < map.Height ; my++ )
            {
                for ( int mx = 0 ; mx < map.Width ; mx++ )
                {
                    var cell = map.Cells[mx, my];
                    if ( cell == null )
                        continue;

                    int panelTileIndex = cell.PnlGroupIndex; // actually a PNL tile index
                    int panelTileX = panelTileIndex % 32;    // PNL grid width is always 32
                    int panelTileY = panelTileIndex / 32;

                    // Pixel origin (grid-aware)
                    int cellPixelX = (mx * gw * TileWidth * zoom) + (mx * spacing);
                    int cellPixelY = (my * gh * TileHeight * zoom) + (my * spacing);

                    // --------------------------------------------------------
                    // Draw each tile in the PNL block
                    // --------------------------------------------------------
                    for ( int gy = 0 ; gy < gh ; gy++ )
                    {
                        for ( int gx = 0 ; gx < gw ; gx++ )
                        {
                            int localX = gx;
                            int localY = gy;

                            if ( cell.HFlip ) localX = gw - 1 - localX;
                            if ( cell.VFlip ) localY = gh - 1 - localY;

                            int tileIndex = (panelTileY + localY) * 32 + (panelTileX + localX);
                            if ( tileIndex < 0 || tileIndex >= pnl.Tiles.Length )
                                continue;

                            var t = pnl.Tiles[tileIndex];
                            if ( t == null || !t.IsVisible )
                                continue;

                            int cgxIndex = t.TileIndex;
                            if ( cgxIndex < 0 || cgxIndex >= cgx.TileCount )
                                continue;

                            var cgxTile = cgx.Tiles[cgxIndex];
                            var pixels = cgxTile.Pixels;

                            // Palette override (MAP > PNL)
                            int paletteGroup = (cell.PaletteOverride != 0)
                                ? cell.PaletteOverride
                                : t.PaletteRow;

                            int tilePixelX = cellPixelX + (gx * TileWidth  * zoom);
                            int tilePixelY = cellPixelY + (gy * TileHeight * zoom);

                            // Draw tile
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
                                        int destY = destY0 + zy;
                                        int rowOffset = destY * width * 4;

                                        for ( int zx = 0 ; zx < zoom ; zx++ )
                                        {
                                            int destX = destX0 + zx;
                                            int idx = rowOffset + destX * 4;

                                            buffer[idx + 0] = c.B;
                                            buffer[idx + 1] = c.G;
                                            buffer[idx + 2] = c.R;
                                            buffer[idx + 3] = 255;
                                        }
                                    }
                                }
                            }

                            // ------------------------------------------------
                            // Debug glyph overlay (CGX index)
                            // ------------------------------------------------
                            if ( debugMode )
                            {
                                var glyph = ScrDebugGlyphs.GetGlyphForCgxIndex(cgxIndex);

                                DebugOverlay.DrawGlyph8x8InTile(
                                    buffer ,
                                    width ,
                                    height ,
                                    tilePixelX ,
                                    tilePixelY ,
                                    TileWidth * zoom ,
                                    TileHeight * zoom ,
                                    glyph
                                );
                            }
                        }
                    }
                }
            }

            // ------------------------------------------------------------
            // Debug tint
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
            // Debug pattern overlays (one per MAP cell)
            // ------------------------------------------------------------
            if ( debugMode )
            {
                for ( int my = 0 ; my < map.Height ; my++ )
                {
                    for ( int mx = 0 ; mx < map.Width ; mx++ )
                    {
                        var cell = map.Cells[mx, my];
                        if ( cell == null )
                            continue;

                        var pattern = new DebugPattern
                        {
                            GridX = mx,
                            GridY = my,
                            WidthInTiles  = gw,
                            HeightInTiles = gh,
                            OuterColor = Colors.White,
                            InnerColor = DebugColors.GetColorForPnlTile(cell.PnlGroupIndex),
                            PatternIndex = cell.PnlGroupIndex
                        };

                        DebugOverlay.DrawPatternRegion(
                            buffer ,
                            width ,
                            height ,
                            TileWidth * zoom ,
                            TileHeight * zoom ,
                            spacing ,
                            pattern
                        );
                    }
                }
            }

            // ------------------------------------------------------------
            // Grid lines
            // ------------------------------------------------------------
            if ( showGrid && zoom >= 2 )
            {
                byte gridR = 128;
                byte gridG = 128;
                byte gridB = 128;
                byte gridA = 255;

                // Vertical
                for ( int mx = 1 ; mx < map.Width ; mx++ )
                {
                    int x = (mx * groupPixelWidth) + ((mx - 1) * spacing);

                    for ( int y = 0 ; y < height ; y++ )
                    {
                        int idx = (y * width + x) * 4;
                        buffer[idx + 0] = gridB;
                        buffer[idx + 1] = gridG;
                        buffer[idx + 2] = gridR;
                        buffer[idx + 3] = gridA;
                    }
                }

                // Horizontal
                for ( int my = 1 ; my < map.Height ; my++ )
                {
                    int y = (my * groupPixelHeight) + ((my - 1) * spacing);

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
            int row = paletteIndex / ColorsPerRow;
            int colIdx = paletteIndex % ColorsPerRow;

            return col.GetColor( row , colIdx );
        }
    }
}
