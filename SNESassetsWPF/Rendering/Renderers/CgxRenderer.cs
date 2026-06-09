using SNESassetsWPF.Formats;
using SNESassetsWPF.Models;
using System;
using System.Windows.Media;

namespace SNESassetsWPF.Rendering
{
    public class CgxRenderer
    {
        private const int TileWidth  = 8;
        private const int TileHeight = 8;
        private const int ColorsPerRow = 16;

        public RenderResult Render(
            CgxFile cgx ,
            ColFile col ,
            bool forceSingleRow ,
            int forcedRowIndex ,
            int tilesPerRow ,
            int zoom ,
            bool showGrid)
        {
            if ( zoom < 1 )
                zoom = 1;

            int spacing = (showGrid && zoom >= 2) ? 1 : 0;

            int rows = (int)Math.Ceiling(cgx.TileCount / (double)tilesPerRow);
            int tileSize = TileWidth * zoom;

            int width =
                (tilesPerRow * tileSize) +
                ((tilesPerRow - 1) * spacing);

            int height =
                (rows * tileSize) +
                ((rows - 1) * spacing);

            byte[] buffer = new byte[width * height * 4];

            // ------------------------------------------------------------
            // Render tiles
            // ------------------------------------------------------------
            for ( int t = 0 ; t < cgx.TileCount ; t++ )
            {
                CgxTile tile = cgx.Tiles[t];
                byte[,] pixels = tile.Pixels;

                int tileX = t % tilesPerRow;
                int tileY = t / tilesPerRow;

                int tileOriginX =
                    (tileX * tileSize) +
                    (tileX * spacing);

                int tileOriginY =
                    (tileY * tileSize) +
                    (tileY * spacing);

                for ( int y = 0 ; y < TileHeight ; y++ )
                {
                    for ( int x = 0 ; x < TileWidth ; x++ )
                    {
                        byte paletteIndex;

                        if ( cgx.BitDepth == 8 )
                        {
                            // 8bpp: pixel value is already the flat palette index (0–255)
                            paletteIndex = pixels[y , x];
                        }
                        else
                        {
                            // 4bpp: pixel is 0–15 within the selected palette row
                            byte colorInRow = pixels[y, x];
                            int row = tile.PaletteRow;

                            if ( forceSingleRow )
                                row = forcedRowIndex;

                            paletteIndex = (byte)( row * ColorsPerRow + colorInRow );
                        }

                        // Use flat cached palette (matches SNES CGRAM layout)
                        var c = col.CachedColors[paletteIndex];


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

            // ------------------------------------------------------------
            // Optional grid
            // ------------------------------------------------------------
            if ( showGrid && zoom >= 2 )
            {
                byte GR = 128, GG = 128, GB = 128, GA = 255;

                // Vertical
                for ( int tx = 1 ; tx < tilesPerRow ; tx++ )
                {
                    int x = (tx * tileSize) + ((tx - 1) * spacing);
                    for ( int y = 0 ; y < height ; y++ )
                    {
                        int idx = (y * width + x) * 4;
                        buffer[idx + 0] = GB;
                        buffer[idx + 1] = GG;
                        buffer[idx + 2] = GR;
                        buffer[idx + 3] = GA;
                    }
                }

                // Horizontal
                for ( int ty = 1 ; ty < rows ; ty++ )
                {
                    int y = (ty * tileSize) + ((ty - 1) * spacing);
                    int rowOffset = y * width * 4;

                    for ( int x = 0 ; x < width ; x++ )
                    {
                        int idx = rowOffset + x * 4;
                        buffer[idx + 0] = GB;
                        buffer[idx + 1] = GG;
                        buffer[idx + 2] = GR;
                        buffer[idx + 3] = GA;
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
