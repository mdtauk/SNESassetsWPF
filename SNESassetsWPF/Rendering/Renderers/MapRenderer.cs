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
            zoom = Math.Max( 1 , zoom );

            if ( map == null || pnl == null || cgx == null || col == null )
            {
                width = height = 0;
                return Array.Empty<byte>();
            }

            // MAP always renders 1×1 tiles
            int metaW = 1;
            int metaH = 1;

            int tilesAcross = map.Width * metaW;
            int tilesDown   = map.Height * metaH;

            int spacing = (showGrid && zoom >= 2) ? 1 : 0;

            width = tilesAcross * ( TileW * zoom ) + ( tilesAcross - 1 ) * spacing;
            height = tilesDown * ( TileH * zoom ) + ( tilesDown - 1 ) * spacing;

            var buffer = new byte[width * height * 4];

            // ─────────────────────────────────────────────
            // MAIN RENDER LOOP
            // ─────────────────────────────────────────────
            for ( int my = 0 ; my < map.Height ; my++ )
            {
                for ( int mx = 0 ; mx < map.Width ; mx++ )
                {
                    var cell = map.GetCell(mx, my);
                    int baseIndex = cell.PnlIndex;

                    int baseX = baseIndex % 32;
                    int baseY = baseIndex / 32;

                    // Only 1×1 tile draw
                    var entry = pnl.GetEntry(baseX, baseY);
                    if ( entry == null || !entry.IsPresent )
                        continue;

                    int tileIndex = entry.TileIndex;
                    if ( tileIndex < 0 || tileIndex >= cgx.TileCount )
                        continue;

                    var tile = cgx.Tiles[tileIndex];
                    if ( tile == null )
                        continue;

                    DrawTile(
                        buffer ,
                        width ,
                        height ,
                        mx ,
                        my ,
                        zoom ,
                        spacing ,
                        entry ,
                        tile ,
                        cgx ,
                        col
                    );
                }
            }

            // ─────────────────────────────────────────────
            // DEBUG OVERLAYS
            // ─────────────────────────────────────────────
            if ( showDebug )
            {
                DebugOverlay.ApplyTintToSurface(
                    buffer , width , height ,
                    Color.FromRgb(  128 , 128 , 128 ) ,
                    0.4
                );

                // Debug grouping comes from PNL meta‑tile size
                int groupW = (pnl.MetaWidth  > 0) ? pnl.MetaWidth  : 2;
                int groupH = (pnl.MetaHeight > 0) ? pnl.MetaHeight : 2;

                var patterns = BuildMapPatterns(map, groupW, groupH);

                DebugOverlay.DrawMapPatterns(
                    buffer ,
                    width ,
                    height ,
                    patterns ,
                    zoom ,
                    spacing ,
                    tilesAcross
                );

                DebugOverlay.DrawMapGlyphs(
                    buffer ,
                    width ,
                    height ,
                    map ,
                    pnl ,
                    zoom ,
                    spacing ,
                    tilesAcross
                );
            }

            // ─────────────────────────────────────────────
            // GRID
            // ─────────────────────────────────────────────
            if ( showGrid && zoom >= 2 )
            {
                DebugOverlay.DrawGrid(
                    buffer ,
                    width ,
                    height ,
                    TileW ,
                    TileH ,
                    zoom ,
                    spacing ,
                    tilesAcross ,
                    tilesDown
                );
            }

            return buffer;
        }

        // ─────────────────────────────────────────────
        // TILE DRAWING
        // ─────────────────────────────────────────────
        private static void DrawTile(
            byte[] buffer ,
            int width ,
            int height ,
            int gridX ,
            int gridY ,
            int zoom ,
            int spacing ,
            PnlEntry entry ,
            CgxTile tile ,
            CgxFile cgx ,
            ColFile col)
        {
            int tileSize = TileW * zoom;

            int px0 = gridX * tileSize + gridX * spacing;
            int py0 = gridY * tileSize + gridY * spacing;

            for ( int y = 0 ; y < 8 ; y++ )
            {
                int sy = entry.VFlip ? (7 - y) : y;

                for ( int x = 0 ; x < 8 ; x++ )
                {
                    int sx = entry.HFlip ? (7 - x) : x;

                    byte pix = tile.Pixels[sy, sx];

                    int colorIndex =
                        (cgx.BitDepth == 8)
                        ? pix
                        : entry.PaletteRow * 16 + pix;

                    var c = col.CachedColors[colorIndex];

                    for ( int zy = 0 ; zy < zoom ; zy++ )
                    {
                        int dy = py0 + y * zoom + zy;
                        if ( dy < 0 || dy >= height ) continue;

                        for ( int zx = 0 ; zx < zoom ; zx++ )
                        {
                            int dx = px0 + x * zoom + zx;
                            if ( dx < 0 || dx >= width ) continue;

                            int idx = (dy * width + dx) * 4;
                            buffer[idx + 0] = c.B;
                            buffer[idx + 1] = c.G;
                            buffer[idx + 2] = c.R;
                            buffer[idx + 3] = 255;
                        }
                    }
                }
            }
        }

        // ─────────────────────────────────────────────
        // DEBUG PATTERNS
        // ─────────────────────────────────────────────
        private static List<DebugPattern> BuildMapPatterns(MapFile map , int groupW , int groupH)
        {
            var list = new List<DebugPattern>();

            // Total number of MAP cells
            int groupsAcross = map.Width;
            int groupsDown   = map.Height;

            for ( int my = 0 ; my < map.Height ; my++ )
            {
                for ( int mx = 0 ; mx < map.Width ; mx++ )
                {
                    // Linear index for colour selection
                    int patternIndex = my * groupsAcross + mx;

                    // Use your DebugColors system
                    Color c = DebugColors.GetColorForPnlTile(patternIndex);

                    list.Add( new DebugPattern
                    {
                        GridX = mx * groupW ,
                        GridY = my * groupH ,
                        WidthInTiles = groupW ,
                        HeightInTiles = groupH ,

                        // Same style as PNL patterns
                        OuterColor = Color.FromRgb( 128 , 128 , 128 ) ,
                        InnerColor = c ,
                        OuterBorderThickness = 1 ,
                        InnerBorderThickness = 1
                    } );
                }
            }

            return list;
        }

    }
}
