using SNESassetsWPF.Formats;
using SNESassetsWPF.Models;
using System;
using System.Windows.Media;

namespace SNESassetsWPF.Rendering
{
    public class ScrRenderer
    {
        private const int TileWidth    = 8;
        private const int TileHeight   = 8;
        private const int ColorsPerRow = 16;

        private readonly ScrFile _scr;
        private readonly CgxFile _cgx;
        private readonly ColFile _col;
        private readonly bool _showGrid;

        public ScrRenderer(
            ScrFile scr ,
            CgxFile cgx ,
            ColFile col ,
            bool showGrid)
        {
            _scr = scr;
            _cgx = cgx;
            _col = col;
            _showGrid = showGrid;
        }

        public RenderResult Render(int zoom)
        {
            if ( zoom < 1 )
                zoom = 1;

            int spacing = (_showGrid && zoom >= 2) ? 1 : 0;

            int tilesWide  = _scr.WidthTiles;
            int tilesHigh  = _scr.HeightTiles;

            int baseWidth  = tilesWide  * TileWidth;
            int baseHeight = tilesHigh * TileHeight;

            int width  = (baseWidth  * zoom) + ((tilesWide  - 1) * spacing);
            int height = (baseHeight * zoom) + ((tilesHigh - 1) * spacing);

            var buffer = new byte[width * height * 4];

            for ( int ty = 0 ; ty < tilesHigh ; ty++ )
            {
                for ( int tx = 0 ; tx < tilesWide ; tx++ )
                {
                    ScrTile entry = _scr.Tiles[ty, tx];

                    int tileIndex = entry.TileIndex;
                    if ( tileIndex < 0 || tileIndex >= _cgx.TileCount )
                        continue;

                    CgxTile cgxTile = _cgx.Tiles[tileIndex];
                    byte[,] pixels  = cgxTile.Pixels;

                    // S‑CG‑CAD: palette comes from CGX tile prefix, not SCR
                    int paletteGroup = cgxTile.PaletteGroup;

                    bool hFlip = entry.HFlip;
                    bool vFlip = entry.VFlip;

                    int tileOriginX = (tx * TileWidth  * zoom) + (tx * spacing);
                    int tileOriginY = (ty * TileHeight * zoom) + (ty * spacing);

                    for ( int y = 0 ; y < TileHeight ; y++ )
                    {
                        int srcY = vFlip ? (TileHeight - 1 - y) : y;

                        for ( int x = 0 ; x < TileWidth ; x++ )
                        {
                            int srcX = hFlip ? (TileWidth - 1 - x) : x;

                            byte baseIndex = pixels[srcY, srcX];

                            int paletteIndex = ComputePaletteIndex(
                                _cgx.BitDepth,
                                paletteGroup,
                                baseIndex
                            );

                            Color c = ResolveColor(_col, paletteIndex);

                            int destX0 = tileOriginX + (x * zoom);
                            int destY0 = tileOriginY + (y * zoom);

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
                                    buffer[idx + 3] = 0xFF;
                                }
                            }
                        }
                    }
                }
            }

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

        // Same as CgxRenderer
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
