using SNESassetsWPF.Formats;
using SNESassetsWPF.Models;
using SNESassetsWPF.ViewModels;
using System;
using System.Windows.Media;

namespace SNESassetsWPF.Rendering
{
    public class ScrDebugRenderer
    {
        private const int TileWidth = 8;
        private const int TileHeight = 8;

        private readonly ScrFile _scr;
        private readonly ScrDebugMode _mode;
        private readonly bool _showGrid;

        public ScrDebugRenderer(ScrFile scr , ScrDebugMode mode , bool showGrid)
        {
            _scr = scr ?? throw new ArgumentNullException( nameof( scr ) );
            _mode = mode;
            _showGrid = showGrid;
        }

        public RenderResult Render(int zoom)
        {
            if ( zoom < 1 )
                zoom = 1;

            int tilesWide = _scr.WidthTiles;
            int tilesHigh = _scr.HeightTiles;

            // 1px spacing only when grid is enabled AND zoom >= 2
            int spacing = (_showGrid && zoom >= 2) ? 1 : 0;

            int baseWidth = tilesWide * TileWidth;
            int baseHeight = tilesHigh * TileHeight;

            int width = (baseWidth * zoom) + ((tilesWide - 1) * spacing);
            int height = (baseHeight * zoom) + ((tilesHigh - 1) * spacing);

            var buffer = new byte[width * height * 4];

            // ───────────────────────────────────────────────
            // Render tiles
            // ───────────────────────────────────────────────
            for ( int ty = 0 ; ty < tilesHigh ; ty++ )
            {
                for ( int tx = 0 ; tx < tilesWide ; tx++ )
                {
                    ScrTile tile = _scr.Tiles[ty, tx];

                    // Background colour from debug palette
                    Color bg = ScrDebugTileColors.GetColorForTile(tile);

                    // 8×8 glyph overlay (tile index, palette, flags, etc.)
                    Color[,] glyph = ScrDebugGlyphs.GetGlyphForMode(_mode, tile);

                    int tileOriginX = (tx * TileWidth * zoom) + (tx * spacing);
                    int tileOriginY = (ty * TileHeight * zoom) + (ty * spacing);

                    for ( int y = 0 ; y < TileHeight ; y++ )
                    {
                        for ( int x = 0 ; x < TileWidth ; x++ )
                        {
                            // Default = background colour
                            Color c = bg;

                            // Overlay glyph pixel if present
                            if ( glyph != null && glyph.HasPixel( x , y ) )
                                c = glyph.GetPixelColor( x , y );

                            int destX0 = tileOriginX + (x * zoom);
                            int destY0 = tileOriginY + (y * zoom);

                            // Write zoomed block
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
                                    buffer[idx + 3] = 0xFF;
                                }
                            }
                        }
                    }
                }
            }

            // ───────────────────────────────────────────────
            // Draw grid (1px lines between tiles, never over tiles)
            // ───────────────────────────────────────────────
            if ( _showGrid && zoom >= 2 )
            {
                byte gridR = 128;
                byte gridG = 128;
                byte gridB = 128;
                byte gridA = 255;

                // Vertical grid lines
                for ( int tx = 1 ; tx < tilesWide ; tx++ )
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
                for ( int ty = 1 ; ty < tilesHigh ; ty++ )
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
    }
}
