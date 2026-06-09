using SNESassetsWPF.Formats;
using SNESassetsWPF.Models;
using System;
using System.Collections.Generic;
using System.Windows.Media;

namespace SNESassetsWPF.Rendering
{
    public static class MapRenderer
    {
        private const int TileW = 8;
        private const int TileH = 8;

        private static readonly Color DebugTintColor = Color.FromArgb(255, 128, 128, 128);
        private const double DebugTintOpacity = 0.4;

        public static byte[] Render(
            MapFile map ,
            PnlFile pnl ,
            CgxFile cgx ,
            ColFile col ,
            int zoom ,
            bool showGrid ,
            bool showDebug ,
            out int width ,
            out int height)
        {
            if ( zoom <= 0 ) zoom = 1;

            // Fallback if any core asset is missing
            if ( map == null || pnl == null || cgx == null || col == null )
                return RenderFallback( zoom , out width , out height );

            int metaWidth  = map.MetaWidth;
            int metaHeight = map.MetaHeight;

            int tilesPerRow = map.Width * metaWidth;
            int rows        = map.Height * metaHeight;

            int spacing = (showGrid && zoom >= 2) ? 1 : 0;

            width = ( tilesPerRow * ( TileW * zoom ) ) + ( ( tilesPerRow - 1 ) * spacing );
            height = ( rows * ( TileH * zoom ) ) + ( ( rows - 1 ) * spacing );

            var buffer = new byte[width * height * 4];

            // 1. Render MAP → PNL → CGX tiles
            for ( int my = 0 ; my < map.Height ; my++ )
            {
                for ( int mx = 0 ; mx < map.Width ; mx++ )
                {
                    var cell = map.GetCell(mx, my);
                    int basePnlIndex = cell.PnlIndex;

                    int basePnlX = basePnlIndex % 128;
                    int basePnlY = basePnlIndex / 128;

                    for ( int ty = 0 ; ty < metaHeight ; ty++ )
                    {
                        for ( int tx = 0 ; tx < metaWidth ; tx++ )
                        {
                            int pnlX = basePnlX + tx;
                            int pnlY = basePnlY + ty;

                            if ( pnlX < 0 || pnlX >= 128 || pnlY < 0 || pnlY >= 128 )
                                continue;

                            var entry = pnl.GetEntry(pnlX, pnlY);
                            if ( entry == null || !entry.IsPresent )
                                continue;

                            int tileIndex = entry.TileIndex;
                            if ( tileIndex < 0 || tileIndex >= cgx.TileCount )
                                continue;

                            var tile = cgx.Tiles[tileIndex];
                            if ( tile == null )
                                continue;

                            byte[,] pixels = tile.Pixels;

                            int gridX = mx * metaWidth  + tx;
                            int gridY = my * metaHeight + ty;

                            int tileOriginX =
                                (gridX * (TileW * zoom)) +
                                (gridX * spacing);

                            int tileOriginY =
                                (gridY * (TileH * zoom)) +
                                (gridY * spacing);

                            for ( int y = 0 ; y < TileH ; y++ )
                            {
                                int srcY = entry.VFlip ? (TileH - 1 - y) : y;

                                for ( int x = 0 ; x < TileW ; x++ )
                                {
                                    int srcX = entry.HFlip ? (TileW - 1 - x) : x;

                                    byte palIndex = pixels[srcY, srcX];
                                    var c = col.GetColor(entry.PaletteRow, palIndex);

                                    int destX0 = tileOriginX + (x * zoom);
                                    int destY0 = tileOriginY + (y * zoom);

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
                        }
                    }
                }
            }

            // 2. Tint overlay
            if ( showDebug )
            {
                DebugOverlay.ApplyTintToSurface(
                    buffer ,
                    width ,
                    height ,
                    DebugTintColor ,
                    DebugTintOpacity
                );
            }

            // 3. Meta‑tile debug patterns (one per MAP cell)
            if ( showDebug )
            {
                var patterns = BuildMapPatterns(
                    map,
                    metaWidth,
                    metaHeight
                );

                DebugOverlay.DrawMapPatterns(
                    buffer ,
                    width ,
                    height ,
                    patterns ,
                    zoom ,
                    spacing ,
                    tilesPerRow
                );
            }

            // 4. CGX index hex glyphs per 8×8 tile
            if ( showDebug )
            {
                DebugOverlay.DrawMapGlyphs(
                    buffer ,
                    width ,
                    height ,
                    map ,
                    pnl ,
                    zoom ,
                    spacing ,
                    tilesPerRow
                );
            }

            // 5. Grid lines
            if ( showGrid )
            {
                DebugOverlay.DrawGrid(
                    buffer ,
                    width ,
                    height ,
                    TileW ,
                    TileH ,
                    zoom ,
                    spacing ,
                    tilesPerRow ,
                    rows
                );
            }

            return buffer;
        }

        private static List<DebugPattern> BuildMapPatterns(
            MapFile map ,
            int metaWidth ,
            int metaHeight)
        {
            var patterns = new List<DebugPattern>();

            for ( int my = 0 ; my < map.Height ; my++ )
            {
                for ( int mx = 0 ; mx < map.Width ; mx++ )
                {
                    int gx = mx * metaWidth;
                    int gy = my * metaHeight;

                    patterns.Add( new DebugPattern
                    {
                        GridX = gx ,
                        GridY = gy ,
                        WidthInTiles = metaWidth ,
                        HeightInTiles = metaHeight ,
                        OuterColor = Colors.Magenta ,
                        InnerColor = Colors.Transparent
                    } );
                }
            }

            return patterns;
        }

        private static byte[] RenderFallback(int zoom , out int width , out int height)
        {
            const int tiles = 8;
            width = tiles * TileW * zoom;
            height = tiles * TileH * zoom;

            var buffer = new byte[width * height * 4];

            Color a = Colors.DarkOliveGreen;
            Color b = Colors.DimGray;

            for ( int y = 0 ; y < height ; y++ )
            {
                int ty = (y / zoom) / TileH;

                for ( int x = 0 ; x < width ; x++ )
                {
                    int tx = (x / zoom) / TileW;
                    bool useA = ((tx + ty) & 1) == 0;

                    Color c = useA ? a : b;

                    int idx = (y * width + x) * 4;
                    buffer[idx + 0] = c.B;
                    buffer[idx + 1] = c.G;
                    buffer[idx + 2] = c.R;
                    buffer[idx + 3] = 255;
                }
            }

            return buffer;
        }
    }
}
