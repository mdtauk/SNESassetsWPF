using SNESassetsWPF.Formats;
using SNESassetsWPF.Models;
using System.Windows.Media;

namespace SNESassetsWPF.Rendering
{
    public class ScrRenderer
    {
        private readonly ScrFile _scr;
        private readonly CgxFile _cgx;
        private readonly ColFile _col;
        private readonly bool _showGrid;

        private const int TileWidth  = 8;
        private const int TileHeight = 8;

        public ScrRenderer(ScrFile scr , CgxFile cgx , ColFile col , bool showGrid)
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

            int widthTiles  = _scr.WidthTiles;
            int heightTiles = _scr.HeightTiles;

            int baseWidth  = widthTiles  * TileWidth;
            int baseHeight = heightTiles * TileHeight;

            int width  = (baseWidth  * zoom) + ((widthTiles  - 1) * spacing);
            int height = (baseHeight * zoom) + ((heightTiles - 1) * spacing);

            var buffer = new byte[width * height * 4];

            // Draw tiles
            for ( int ty = 0 ; ty < heightTiles ; ty++ )
            {
                for ( int tx = 0 ; tx < widthTiles ; tx++ )
                {
                    var scrTile = _scr.Tiles[ty, tx];

                    if ( scrTile.TileIndex < 0 || scrTile.TileIndex >= _cgx.Tiles.Length )
                        continue;

                    var cgxTile = _cgx.GetTile(scrTile.TileIndex);

                    DrawTile(
                        buffer ,
                        width ,
                        height ,
                        tx ,
                        ty ,
                        cgxTile ,
                        scrTile ,
                        zoom ,
                        spacing
                    );
                }
            }

            // Grid in the gaps (same as ScrDebugRenderer)
            if ( _showGrid && zoom >= 2 )
            {
                byte R = 128, G = 128, B = 128, A = 255;

                // Vertical lines
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

                // Horizontal lines
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

        private void DrawTile(
            byte[] buffer ,
            int bufferWidth ,
            int bufferHeight ,
            int tileX ,
            int tileY ,
            CgxTile cgxTile ,
            ScrTile scrTile ,
            int zoom ,
            int spacing)
        {
            int tileOriginX = (tileX * TileWidth  * zoom) + (tileX * spacing);
            int tileOriginY = (tileY * TileHeight * zoom) + (tileY * spacing);

            int paletteRow = scrTile.PaletteIndex;

            for ( int py = 0 ; py < TileHeight ; py++ )
            {
                int srcY = scrTile.VFlip ? (TileHeight - 1 - py) : py;

                for ( int px = 0 ; px < TileWidth ; px++ )
                {
                    int srcX = scrTile.HFlip ? (TileWidth - 1 - px) : px;

                    byte colorIndex = cgxTile.Pixels[srcY, srcX];

                    if ( colorIndex == 0 )
                        continue;

                    if ( paletteRow < 0 || paletteRow >= 16 )
                        continue;
                    if ( colorIndex < 0 || colorIndex >= 16 )
                        continue;

                    Color c = _col.GetColor(paletteRow, colorIndex);

                    int destX0 = tileOriginX + (px * zoom);
                    int destY0 = tileOriginY + (py * zoom);

                    for ( int zy = 0 ; zy < zoom ; zy++ )
                    {
                        int destY = destY0 + zy;
                        if ( destY < 0 || destY >= bufferHeight )
                            continue;

                        int rowOffset = destY * bufferWidth * 4;

                        for ( int zx = 0 ; zx < zoom ; zx++ )
                        {
                            int destX = destX0 + zx;
                            if ( destX < 0 || destX >= bufferWidth )
                                continue;

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
}
