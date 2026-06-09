using System;
using System.Collections.Generic;
using System.Windows.Media;
using SNESassetsWPF.Formats;
using SNESassetsWPF.Models;

namespace SNESassetsWPF.Rendering
{
    public static class PnlRenderer
    {
        public static byte[] Render(
            PnlFile pnl ,
            CgxFile cgx ,
            ColFile col ,
            MapFile map ,
            int zoom ,
            bool showGrid ,
            bool showDebug ,
            out int width ,
            out int height)
        {
            // No PNL → nothing to render
            if ( pnl == null )
            {
                width = height = 0;
                return Array.Empty<byte>();
            }

            zoom = Math.Max( 1 , zoom );

            // spacing only when grid is shown AND zoom ≥ 2
            int spacing = (showGrid && zoom >= 2) ? 1 : 0;

            int tilePixelSize = 8 * zoom;

            // final buffer size must include spacing between tiles
            width = PnlFile.Width * tilePixelSize + ( PnlFile.Width - 1 ) * spacing;
            height = PnlFile.Height * tilePixelSize + ( PnlFile.Height - 1 ) * spacing;

            var buffer = new byte[width * height * 4]; // BGRA32

            bool hasCgxCol = (cgx != null && col != null);

            // Group size for debug pattern
            int groupW = 2;
            int groupH = 2;

            if ( map != null )
            {
                groupW = Math.Max( 1 , map.Width );
                groupH = Math.Max( 1 , map.Height );
            }

            // Collect debug patterns
            List<DebugPattern> patterns = showDebug
                ? BuildPnlPatterns(groupW, groupH)
                : new List<DebugPattern>();

            // Main tile loop
            for ( int ty = 0 ; ty < PnlFile.Height ; ty++ )
            {
                for ( int tx = 0 ; tx < PnlFile.Width ; tx++ )
                {
                    int tileIndex = ty * PnlFile.Width + tx;
                    var tile = pnl.Entries[tileIndex];

                    int px0 = tx * tilePixelSize + tx * spacing;
                    int py0 = ty * tilePixelSize + ty * spacing;

                    if ( !hasCgxCol )
                    {
                        DrawCheckerboardTile( buffer , width , height , px0 , py0 , tilePixelSize );
                    }
                    else
                    {
                        if ( tile.IsPresent )
                            DrawCgxTile( buffer , width , height , px0 , py0 , tilePixelSize , tile , cgx , col );
                        else
                            ClearTile( buffer , width , height , px0 , py0 , tilePixelSize );
                    }
                }
            }

            // ─────────────────────────────────────────────
            // GRID (independent of debug)
            // ─────────────────────────────────────────────
            if ( showGrid && zoom >= 2 )
            {
                DebugOverlay.DrawGrid(
                    buffer ,
                    width ,
                    height ,
                    8 ,
                    8 ,
                    zoom ,
                    spacing ,
                    tilesPerRow: PnlFile.Width ,
                    rows: PnlFile.Height
                );
            }

            // ─────────────────────────────────────────────
            // DEBUG OVERLAYS
            // ─────────────────────────────────────────────
            if ( showDebug )
            {
                // 2. Tint overlay (global wash) – FIRST in debug
                DebugOverlay.ApplyTintToSurface(
                    buffer ,
                    width ,
                    height ,
                    tintColor: Color.FromArgb(255,128,128,128) ,
                    opacity: 0.3
                );

                // 3. Pattern boxes
                DebugOverlay.DrawPnlPatterns(
                    buffer ,
                    width ,
                    height ,
                    pnl ,
                    patterns ,
                    zoom ,
                    spacing ,
                    tilesPerRow: PnlFile.Width
                );

                // 4. Glyphs (CGX tile index)
                DebugOverlay.DrawPnlGlyphs(
                    buffer ,
                    width ,
                    height ,
                    pnl ,
                    zoom ,
                    spacing ,
                    tilesPerRow: PnlFile.Width
                );
            }


            return buffer;
        }

        // ─────────────────────────────────────────────
        // Build debug patterns (group boxes)
        // ─────────────────────────────────────────────
        private static List<DebugPattern> BuildPnlPatterns(int groupW , int groupH)
        {
            var list = new List<DebugPattern>();

            for ( int gy = 0 ; gy < PnlFile.Height ; gy += groupH )
            {
                for ( int gx = 0 ; gx < PnlFile.Width ; gx += groupW )
                {
                    int patternIndex = (gy / groupH) * (PnlFile.Width / groupW) + (gx / groupW);
                    Color c = DebugColors.GetColorForPnlTile(patternIndex);

                    list.Add( new DebugPattern
                    {
                        GridX = gx ,
                        GridY = gy ,
                        WidthInTiles = groupW ,
                        HeightInTiles = groupH ,
                        OuterColor = Color.FromArgb(255,128,128,128) ,
                        InnerColor = c ,
                        OuterBorderThickness = 1 ,
                        InnerBorderThickness = 1
                    } );
                }
            }

            return list;
        }

        // ─────────────────────────────────────────────
        // Tile rendering helpers
        // ─────────────────────────────────────────────

        private static void DrawCgxTile(
            byte[] buffer ,
            int width ,
            int height ,
            int px0 ,
            int py0 ,
            int tileSize ,
            PnlEntry tile ,
            CgxFile cgx ,
            ColFile col)
        {
            if ( tile.TileIndex < 0 || tile.TileIndex >= cgx.Tiles.Length )
            {
                ClearTile( buffer , width , height , px0 , py0 , tileSize );
                return;
            }

            var cgxTile = cgx.Tiles[tile.TileIndex];
            int palRow = tile.PaletteRow;

            int scale = tileSize / 8;

            for ( int y = 0 ; y < 8 ; y++ )
            {
                for ( int x = 0 ; x < 8 ; x++ )
                {
                    int sx = tile.HFlip ? (7 - x) : x;
                    int sy = tile.VFlip ? (7 - y) : y;

                    byte pixIndex = cgxTile.Pixels[sy, sx];

                    int colorIndex;
                    if ( cgx.BitDepth == 8 )
                    {
                        // 8bpp: pixel is already the flat palette index (0–255)
                        colorIndex = pixIndex;
                    }
                    else
                    {
                        // 4bpp: pixel is 0–15 within the selected palette row
                        colorIndex = palRow * 16 + pixIndex;
                    }

                    // Safety clamp (optional but nice)
                    if ( colorIndex < 0 || colorIndex >= col.CachedColors.Length )
                        return; // or treat as transparent

                    var color = col.CachedColors[colorIndex];


                    for ( int zy = 0 ; zy < scale ; zy++ )
                    {
                        int dy = py0 + y * scale + zy;
                        if ( dy < 0 || dy >= height ) continue;

                        for ( int zx = 0 ; zx < scale ; zx++ )
                        {
                            int dx = px0 + x * scale + zx;
                            if ( dx < 0 || dx >= width ) continue;

                            int idx = (dy * width + dx) * 4;
                            buffer[idx + 0] = color.B;
                            buffer[idx + 1] = color.G;
                            buffer[idx + 2] = color.R;
                            buffer[idx + 3] = 255;
                        }
                    }
                }
            }
        }

        private static void ClearTile(byte[] buffer , int width , int height , int px0 , int py0 , int tileSize)
        {
            for ( int y = 0 ; y < tileSize ; y++ )
            {
                int dy = py0 + y;
                if ( dy < 0 || dy >= height ) continue;

                for ( int x = 0 ; x < tileSize ; x++ )
                {
                    int dx = px0 + x;
                    if ( dx < 0 || dx >= width ) continue;

                    int idx = (dy * width + dx) * 4;
                    buffer[idx + 0] = 0;
                    buffer[idx + 1] = 0;
                    buffer[idx + 2] = 0;
                    buffer[idx + 3] = 0;
                }
            }
        }

        private static void DrawCheckerboardTile(byte[] buffer , int width , int height , int px0 , int py0 , int tileSize)
        {
            int half = tileSize / 2;

            for ( int y = 0 ; y < tileSize ; y++ )
            {
                int dy = py0 + y;
                if ( dy < 0 || dy >= height ) continue;

                for ( int x = 0 ; x < tileSize ; x++ )
                {
                    int dx = px0 + x;
                    if ( dx < 0 || dx >= width ) continue;

                    bool dark = ((x / half) ^ (y / half)) % 2 == 0;
                    byte c = dark ? (byte)0x60 : (byte)0xA0;

                    int idx = (dy * width + dx) * 4;
                    buffer[idx + 0] = c;
                    buffer[idx + 1] = c;
                    buffer[idx + 2] = c;
                    buffer[idx + 3] = 255;
                }
            }
        }
    }
}
