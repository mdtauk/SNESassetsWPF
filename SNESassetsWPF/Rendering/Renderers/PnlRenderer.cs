using SNESassetsWPF.Formats;
using SNESassetsWPF.Models;
using System.Windows.Media;

namespace SNESassetsWPF.Rendering
{
    public class PnlRenderer
    {
        private const int TileWidth  = 8;
        private const int TileHeight = 8;
        private const int ColorsPerRow = 16;

        private readonly PnlFile _pnl;
        private readonly CgxFile _cgx;
        private readonly ColFile _col;

        public PnlRenderer(PnlFile pnl , CgxFile cgx , ColFile col)
        {
            _pnl = pnl;
            _cgx = cgx;
            _col = col;
        }

        public RenderResult Render(int zoom)
        {
            if ( zoom < 1 ) zoom = 1;

            int width  = PnlFile.PanelWidth  * TileWidth  * zoom;
            int height = PnlFile.PanelHeight * TileHeight * zoom;

            var buffer = new byte[width * height * 4];

            // Clear to transparent
            for ( int i = 0 ; i < buffer.Length ; i += 4 )
                buffer[i + 3] = 0;

            // Extract meta‑tile patterns
            var patterns = MapPnlPatternExtractor.Extract(_pnl);

            foreach ( var p in patterns )
                DrawMetaTile( buffer , width , height , zoom , p );

            return new RenderResult
            {
                Buffer = buffer ,
                Width = width ,
                Height = height
            };
        }

        private void DrawMetaTile(
            byte[] buf ,
            int width ,
            int height ,
            int zoom ,
            MapPnlDebugPattern p)
        {
            for ( int ty = 0 ; ty < p.HeightInTiles ; ty++ )
            {
                for ( int tx = 0 ; tx < p.WidthInTiles ; tx++ )
                {
                    int px = p.PanelX + tx;
                    int py = p.PanelY + ty;

                    var tile = _pnl.GetTile(px, py);
                    if ( tile == null || !tile.Present )
                        continue;

                    DrawTile( buf , width , height , zoom , px , py , tile );
                }
            }
        }

        private void DrawTile(
            byte[] buf ,
            int width ,
            int height ,
            int zoom ,
            int panelX ,
            int panelY ,
            PnlTile tile)
        {
            int cgxIndex = tile.TileId;
            if ( cgxIndex < 0 || cgxIndex >= _cgx.TileCount )
                return;

            var cgxTile = _cgx.Tiles[cgxIndex];
            byte[,] pixels = cgxTile.Pixels;

            int paletteGroup = tile.PaletteRow & 0x0F;
            bool flipX = tile.HFlip;
            bool flipY = tile.VFlip;

            int destTileX = panelX * TileWidth  * zoom;
            int destTileY = panelY * TileHeight * zoom;

            for ( int y = 0 ; y < TileHeight ; y++ )
            {
                int sy = flipY ? (TileHeight - 1 - y) : y;

                for ( int x = 0 ; x < TileWidth ; x++ )
                {
                    int sx = flipX ? (TileWidth - 1 - x) : x;

                    byte baseIndex = pixels[sy, sx];

                    int paletteIndex = ComputePaletteIndex(
                        _cgx.BitDepth,
                        paletteGroup,
                        baseIndex
                    );

                    Color c = ResolveColor(_col, paletteIndex);

                    int destX0 = destTileX + (x * zoom);
                    int destY0 = destTileY + (y * zoom);

                    for ( int zy = 0 ; zy < zoom ; zy++ )
                    {
                        int dy = destY0 + zy;
                        if ( dy < 0 || dy >= height ) continue;

                        int rowOffset = dy * width * 4;

                        for ( int zx = 0 ; zx < zoom ; zx++ )
                        {
                            int dx = destX0 + zx;
                            if ( dx < 0 || dx >= width ) continue;

                            int idx = rowOffset + dx * 4;

                            buf[idx + 0] = c.B;
                            buf[idx + 1] = c.G;
                            buf[idx + 2] = c.R;
                            buf[idx + 3] = 255;
                        }
                    }
                }
            }
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
            int row    = paletteIndex / ColorsPerRow;
            int colIdx = paletteIndex % ColorsPerRow;
            return col.GetColor( row , colIdx );
        }
    }
}
