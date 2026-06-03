using SNESassetsWPF.Formats;
using SNESassetsWPF.Models;
using SNESassetsWPF.ViewModels;
using SNESassetsWPF.Enums;
using System;
using System.Windows.Media;

namespace SNESassetsWPF.Rendering
{
    public class ScrDebugRenderer
    {
        private const int TileWidth  = 8;
        private const int TileHeight = 8;

        public RenderResult Render(
            ScrFile scr ,
            ScrDebugMode mode ,
            int zoom = 1 ,
            bool showGrid = false ,
            bool showInvisibleTiles = false)
        {
            if ( scr == null )
                throw new ArgumentNullException( nameof( scr ) );

            if ( zoom < 1 )
                zoom = 1;

            int spacing = (showGrid && zoom >= 2) ? 1 : 0;

            int widthTiles  = scr.WidthTiles;
            int heightTiles = scr.HeightTiles;

            int baseWidth  = widthTiles  * TileWidth;
            int baseHeight = heightTiles * TileHeight;

            int width  = (baseWidth  * zoom) + ((widthTiles  - 1) * spacing);
            int height = (baseHeight * zoom) + ((heightTiles - 1) * spacing);

            var buffer = new byte[width * height * 4];

            // Render each SCR tile
            for ( int ty = 0 ; ty < heightTiles ; ty++ )
            {
                for ( int tx = 0 ; tx < widthTiles ; tx++ )
                {
                    ScrTile tile = scr.Tiles[ty, tx];

                    // NEW: skip hidden tiles
                    if ( !tile.Visible && !showInvisibleTiles )
                        continue;

                    Color bg =
                        (mode == ScrDebugMode.TileIndex)
                        ? ScrDebugTileColors.GetColorForTileIndexMode(ty, tx)
                        : ScrDebugTileColors.GetColorForTile(tile);

                    Color[,] glyph = ScrDebugGlyphs.GetGlyphForMode(mode, tile);

                    int tileOriginX = (tx * TileWidth  * zoom) + (tx * spacing);
                    int tileOriginY = (ty * TileHeight * zoom) + (ty * spacing);

                    for ( int y = 0 ; y < TileHeight ; y++ )
                    {
                        for ( int x = 0 ; x < TileWidth ; x++ )
                        {
                            Color c = glyph[y, x].A != 0 ? glyph[y, x] : bg;

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
                                    buffer[idx + 3] = 0xFF;
                                }
                            }
                        }
                    }
                }
            }

            // Grid
            if ( showGrid && zoom >= 2 )
            {
                byte R = 128, G = 128, B = 128, A = 255;

                // Vertical
                for ( int tx = 1 ; tx < widthTiles ; tx++ )
                {
                    int x = (tx * TileWidth * zoom) + ((tx - 1) * spacing);

                    for ( int y = 0 ; y < height ; y++ )
                    {
                        int idx = (y * width + x) * 4;
                        buffer[idx + 0] = B;
                        buffer[idx + 1] = G;
                        buffer[idx + 2] = R;
                        buffer[idx + 3] = A;
                    }
                }

                // Horizontal
                for ( int ty = 1 ; ty < heightTiles ; ty++ )
                {
                    int y = (ty * TileHeight * zoom) + ((ty - 1) * spacing);

                    int rowOffset = y * width * 4;
                    for ( int x = 0 ; x < width ; x++ )
                    {
                        int idx = rowOffset + x * 4;
                        buffer[idx + 0] = B;
                        buffer[idx + 1] = G;
                        buffer[idx + 2] = R;
                        buffer[idx + 3] = A;
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
