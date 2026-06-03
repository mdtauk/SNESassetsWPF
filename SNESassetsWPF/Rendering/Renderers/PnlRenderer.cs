using SNESassetsWPF.Formats;
using SNESassetsWPF.Models;
using System;
using System.Diagnostics;
using System.Windows.Media;

namespace SNESassetsWPF.Rendering
{
    /// <summary>
    /// Renders a PNL panel using CGX + COL into a BGRA32 buffer.
    /// Supports zoom, grid, and invisible tile handling.
    /// </summary>
    public class PnlRenderer
    {
        private readonly PnlFile _pnl;
        private readonly CgxFile _cgx;
        private readonly ColFile _col;
        private readonly bool _showGrid;
        private readonly bool _showInvisibleTiles;

        public PnlRenderer(
            PnlFile pnl ,
            CgxFile cgx ,
            ColFile col ,
            bool showGrid ,
            bool showInvisibleTiles = false)
        {
            _pnl = pnl ?? throw new ArgumentNullException( nameof( pnl ) );
            _cgx = cgx ?? throw new ArgumentNullException( nameof( cgx ) );
            _col = col ?? throw new ArgumentNullException( nameof( col ) );
            _showGrid = showGrid;
            _showInvisibleTiles = showInvisibleTiles;
        }

        public RenderResult Render(int zoom)
        {
            Debug.WriteLine( $"[PNL] Render start: CGX null? {_cgx == null}, COL null? {_col == null}" );
            Debug.WriteLine( $"[PNL] Panel size: {PnlFile.PanelWidth}x{PnlFile.PanelHeight}" );
            Debug.WriteLine( $"[PNL] CGX tiles: {_cgx.Tiles.Length}" );

            if ( zoom < 1 )
                zoom = 1;

            int tileSize = 8 * zoom;

            int width = PnlFile.PanelWidth * tileSize;
            int height = PnlFile.PanelHeight * tileSize;

            var buffer = new byte[width * height * 4];

            Clear( buffer , width , height , Colors.Transparent );

            for ( int ty = 0 ; ty < PnlFile.PanelHeight ; ty++ )
            {
                for ( int tx = 0 ; tx < PnlFile.PanelWidth ; tx++ )
                {
                    var tile = _pnl.Tiles[tx, ty];

                    if ( !tile.Present && !_showInvisibleTiles )
                        continue;

                    DrawTile( buffer , width , height , tx , ty , tile , tileSize , zoom );
                }
            }

            if ( _showGrid && zoom >= 2 )
            {
                DrawGrid( buffer , width , height , tileSize , Colors.Gray );
            }

            return new RenderResult
            {
                Buffer = buffer ,
                Width = width ,
                Height = height
            };
        }

        private void DrawTile(
            byte[] buf ,
            int width ,
            int height ,
            int tileX ,
            int tileY ,
            PnlTile tile ,
            int tileSize ,
            int zoom)
        {
            int px0 = tileX * tileSize;
            int py0 = tileY * tileSize;

            if ( tile.TileId < 0 || tile.TileId >= _cgx.Tiles.Length )
            {
                Debug.WriteLine( $"[PNL] INVALID TILE ID {tile.TileId} at ({tileX},{tileY})" );
                return;
            }

            if ( tile.PaletteRow < 0 || tile.PaletteRow >= 16 )
            {
                Debug.WriteLine( $"[PNL] INVALID PALETTE ROW {tile.PaletteRow} at ({tileX},{tileY})" );
                return;
            }

            for ( int py = 0 ; py < 8 ; py++ )
            {
                for ( int px = 0 ; px < 8 ; px++ )
                {
                    var color = SampleTilePixel(tile, px, py);

                    if ( color.A == 0 )
                        continue;

                    int dstX0 = px0 + px * zoom;
                    int dstY0 = py0 + py * zoom;

                    for ( int zy = 0 ; zy < zoom ; zy++ )
                    {
                        int dy = dstY0 + zy;
                        if ( dy < 0 || dy >= height ) continue;

                        int row = dy * width * 4;

                        for ( int zx = 0 ; zx < zoom ; zx++ )
                        {
                            int dx = dstX0 + zx;
                            if ( dx < 0 || dx >= width ) continue;

                            int i = row + dx * 4;

                            buf[i + 0] = color.B;
                            buf[i + 1] = color.G;
                            buf[i + 2] = color.R;
                            buf[i + 3] = 255;
                        }
                    }
                }
            }
        }

        private Color SampleTilePixel(PnlTile tile , int x , int y)
        {
            if ( tile.TileId < 0 || tile.TileId >= _cgx.Tiles.Length )
            {
                Debug.WriteLine( $"[PNL] INVALID TILE ID {tile.TileId}" );
                return Colors.Magenta;
            }

            if ( tile.PaletteRow < 0 || tile.PaletteRow >= 16 )
            {
                Debug.WriteLine( $"[PNL] INVALID PALETTE ROW {tile.PaletteRow}" );
                return Colors.Yellow;
            }

            int sx = tile.HFlip ? (7 - x) : x;
            int sy = tile.VFlip ? (7 - y) : y;

            var cgxTile = _cgx.Tiles[tile.TileId];

            if ( x == 0 && y == 0 )
            {
                Debug.WriteLine( $"[PNL] Tile {tile.TileId} Pal {tile.PaletteRow} FirstPixel={cgxTile.Pixels[0 , 0]}" );
            }

            byte colorIndex = cgxTile.Pixels[sy, sx];

            if ( x == 0 && y == 0 )
            {
                Debug.WriteLine( $"[PNL] Tile {tile.TileId} Pal {tile.PaletteRow} PixelIndex={colorIndex}" );
            }

            if ( colorIndex == 0 )
            {
                if ( x == 0 && y == 0 )
                    Debug.WriteLine( $"[PNL] Tile {tile.TileId} pixelIndex=0 (transparent)" );
                return Colors.Transparent;
            }

            if ( colorIndex >= 16 )
            {
                Debug.WriteLine( $"[PNL] INVALID COLOR INDEX {colorIndex} in tile {tile.TileId}" );
                return Colors.Cyan;
            }

            var rgb = _col.RgbColors[tile.PaletteRow, colorIndex];

            if ( x == 0 && y == 0 )
            {
                var snes = _col.RawColors[tile.PaletteRow, colorIndex];
                Debug.WriteLine( $"[PNL] Tile {tile.TileId} Pal {tile.PaletteRow} SNES={snes.ToHexPair()} RGB=({rgb.R},{rgb.G},{rgb.B})" );
            }

            return _col.GetColor( tile.PaletteRow , colorIndex );
        }

        private static void Clear(byte[] buf , int width , int height , Color c)
        {
            for ( int i = 0 ; i < buf.Length ; i += 4 )
            {
                buf[i + 0] = c.B;
                buf[i + 1] = c.G;
                buf[i + 2] = c.R;
                buf[i + 3] = c.A;
            }
        }

        private static void DrawGrid(byte[] buf , int width , int height , int tileSize , Color c)
        {
            for ( int x = 0 ; x <= width ; x += tileSize )
            {
                DrawLine( buf , width , height , x , 0 , x , height - 1 , c );
            }

            for ( int y = 0 ; y <= height ; y += tileSize )
            {
                DrawLine( buf , width , height , 0 , y , width - 1 , y , c );
            }
        }

        private static void DrawLine(byte[] buf , int width , int height ,
                                     int x0 , int y0 , int x1 , int y1 , Color c)
        {
            int dx = Math.Abs(x1 - x0);
            int sx = x0 < x1 ? 1 : -1;
            int dy = -Math.Abs(y1 - y0);
            int sy = y0 < y1 ? 1 : -1;
            int err = dx + dy;

            while ( true )
            {
                if ( x0 >= 0 && x0 < width && y0 >= 0 && y0 < height )
                {
                    int i = (y0 * width + x0) * 4;
                    buf[i + 0] = c.B;
                    buf[i + 1] = c.G;
                    buf[i + 2] = c.R;
                    buf[i + 3] = 255;
                }

                if ( x0 == x1 && y0 == y1 )
                    break;

                int e2 = 2 * err;
                if ( e2 >= dy )
                {
                    err += dy;
                    x0 += sx;
                }
                if ( e2 <= dx )
                {
                    err += dx;
                    y0 += sy;
                }
            }
        }
    }
}
