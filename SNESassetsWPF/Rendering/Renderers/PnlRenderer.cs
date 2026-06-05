using SNESassetsWPF.Formats;
using SNESassetsWPF.Models;
using System.Windows.Media;

namespace SNESassetsWPF.Rendering
{
    public class PnlRenderer
    {
        public int OutputWidth { get; private set; }
        public int OutputHeight { get; private set; }

        private const int TileSize     = 8;
        private const int ColorsPerRow = 16;

        public RenderResult Render(PnlFile pnl , CgxFile cgx , ColFile col , int zoom , bool showDebugOverlay)
        {
            if ( pnl == null || cgx == null || col == null )
                return new RenderResult { Buffer = [] , Width = 0 , Height = 0 };

            if ( zoom < 1 ) zoom = 1;

            int panelW = PnlFile.PanelWidth;   // 32 (in PNL tiles)
            int panelH = PnlFile.PanelHeight;  // 512 (in PNL tiles)

            OutputWidth = panelW * TileSize * zoom;
            OutputHeight = panelH * TileSize * zoom;

            byte[] buffer = new byte[OutputWidth * OutputHeight * 4];

            for ( int ty = 0 ; ty < panelH ; ty++ )
            {
                for ( int tx = 0 ; tx < panelW ; tx++ )
                {
                    int index = ty * panelW + tx;
                    if ( index < 0 || index >= pnl.PnlTiles.Length )
                        continue;

                    var tile = pnl.PnlTiles[index];
                    if ( tile == null || !tile.IsPresent )
                        continue;

                    int px0 = tx * TileSize;
                    int py0 = ty * TileSize;

                    DrawCgxTile( buffer , px0 , py0 , tile , cgx , col , zoom );
                }
            }

            if ( showDebugOverlay )
                DrawDebugOverlay( buffer , panelW , panelH , zoom );

            return new RenderResult
            {
                Buffer = buffer ,
                Width = OutputWidth ,
                Height = OutputHeight
            };
        }

        private void DrawCgxTile(
            byte[] buffer ,
            int px0 ,
            int py0 ,
            PnlTile tile ,
            CgxFile cgx ,
            ColFile col ,
            int zoom)
        {
            int cgxIndex = tile.TileId;
            if ( cgxIndex < 0 || cgxIndex >= cgx.Tiles.Length )
                return;

            var cgxTile = cgx.Tiles[cgxIndex];

            for ( int py = 0 ; py < TileSize ; py++ )
            {
                int sy = tile.VFlip ? (TileSize - 1 - py) : py;

                for ( int px = 0 ; px < TileSize ; px++ )
                {
                    int sx = tile.HFlip ? (TileSize - 1 - px) : px;

                    byte baseIndex = cgxTile.GetPixel(sx, sy);

                    int paletteIndex = ComputePaletteIndex(cgx.BitDepth, tile.PaletteRow, baseIndex);

                    int row    = paletteIndex / ColorsPerRow;
                    int colIdx = paletteIndex % ColorsPerRow;

                    if ( row < 0 || row >= 16 || colIdx < 0 || colIdx >= 16 )
                        continue;

                    Color c = col.GetColor(row, colIdx);

                    int dstX = (px0 + px) * zoom;
                    int dstY = (py0 + py) * zoom;

                    for ( int zy = 0 ; zy < zoom ; zy++ )
                        for ( int zx = 0 ; zx < zoom ; zx++ )
                            SetPixel( buffer , dstX + zx , dstY + zy , c );
                }
            }
        }


        private void DrawMetaTile(
            byte[] buffer ,
            int pnlX ,
            int pnlY ,
            PnlTile tile ,
            CgxFile cgx ,
            ColFile col ,
            int metaW ,
            int metaH ,
            int zoom)
        {
            // Top-left CGX tile coords for this PnlTile in the 32×512 CGX grid
            int baseCgxX = pnlX * metaW;
            int baseCgxY = pnlY * metaH;

            for ( int my = 0 ; my < metaH ; my++ )
            {
                for ( int mx = 0 ; mx < metaW ; mx++ )
                {
                    int cgxIndex = tile.TileId + my * metaW + mx;
                    if ( cgxIndex < 0 || cgxIndex >= cgx.Tiles.Length )
                        continue;

                    int cgxX = baseCgxX + mx;
                    int cgxY = baseCgxY + my;

                    int px0 = cgxX * TileSize;
                    int py0 = cgxY * TileSize;

                    DrawCgxTile( buffer , px0 , py0 , cgxIndex , tile , cgx , col , zoom );
                }
            }
        }

        private void DrawCgxTile(
            byte[] buffer ,
            int px0 ,
            int py0 ,
            int cgxIndex ,
            PnlTile tile ,
            CgxFile cgx ,
            ColFile col ,
            int zoom)
        {
            if ( cgxIndex < 0 || cgxIndex >= cgx.Tiles.Length )
                return;

            CgxTile cgxTile = cgx.Tiles[cgxIndex];

            for ( int py = 0 ; py < TileSize ; py++ )
            {
                int sy = tile.VFlip ? (TileSize - 1 - py) : py;

                for ( int px = 0 ; px < TileSize ; px++ )
                {
                    int sx = tile.HFlip ? (TileSize - 1 - px) : px;

                    byte baseIndex = cgxTile.GetPixel(sx, sy);

                    int paletteIndex = ComputePaletteIndex(cgx.BitDepth, tile.PaletteRow, baseIndex);

                    int row    = paletteIndex / ColorsPerRow;
                    int colIdx = paletteIndex % ColorsPerRow;

                    if ( row < 0 || row >= 16 || colIdx < 0 || colIdx >= 16 )
                        continue;

                    Color c = col.GetColor(row, colIdx);

                    int dstX = (px0 + px) * zoom;
                    int dstY = (py0 + py) * zoom;

                    for ( int zy = 0 ; zy < zoom ; zy++ )
                        for ( int zx = 0 ; zx < zoom ; zx++ )
                            SetPixel( buffer , dstX + zx , dstY + zy , c );
                }
            }
        }

        private static int ComputePaletteIndex(int bitDepth , int paletteRow , byte baseIndex)
        {
            return bitDepth switch
            {
                2 => ( paletteRow << 2 ) | ( baseIndex & 0x03 ),
                4 => ( paletteRow << 4 ) | ( baseIndex & 0x0F ),
                8 => baseIndex,
                _ => baseIndex
            };
        }

        private void SetPixel(byte[] buffer , int x , int y , Color c)
        {
            if ( x < 0 || x >= OutputWidth || y < 0 || y >= OutputHeight )
                return;

            int i = (y * OutputWidth + x) * 4;
            if ( i < 0 || i + 3 >= buffer.Length )
                return;

            buffer[i + 0] = c.B;
            buffer[i + 1] = c.G;
            buffer[i + 2] = c.R;
            buffer[i + 3] = 255;
        }

        private void DrawDebugOverlay(byte[] buffer , int panelCgxW , int panelCgxH , int zoom)
        {
            byte r = 255, g = 0, b = 0, a = 255;

            int width  = panelCgxW * TileSize * zoom;
            int height = panelCgxH * TileSize * zoom;

            // Vertical lines per CGX tile
            for ( int tx = 0 ; tx < panelCgxW ; tx++ )
            {
                int x = tx * TileSize * zoom;
                for ( int y = 0 ; y < height ; y++ )
                {
                    int i = (y * width + x) * 4;
                    buffer[i + 0] = b;
                    buffer[i + 1] = g;
                    buffer[i + 2] = r;
                    buffer[i + 3] = a;
                }
            }

            // Horizontal lines per CGX tile
            for ( int ty = 0 ; ty < panelCgxH ; ty++ )
            {
                int y = ty * TileSize * zoom;
                int rowOffset = y * width * 4;
                for ( int x = 0 ; x < width ; x++ )
                {
                    int i = rowOffset + x * 4;
                    buffer[i + 0] = b;
                    buffer[i + 1] = g;
                    buffer[i + 2] = r;
                    buffer[i + 3] = a;
                }
            }
        }
    }
}
