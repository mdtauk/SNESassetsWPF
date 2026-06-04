using System.Windows.Media;
using SNESassetsWPF.Formats;

namespace SNESassetsWPF.Rendering
{
    public class MapRenderer
    {
        private readonly MapFile _map;

        // S‑CG‑CAD always uses 2×2 meta‑tiles
        private const int AssumedMetaWidth  = 2;
        private const int AssumedMetaHeight = 2;

        public MapRenderer(MapFile map)
        {
            _map = map;
        }

        public RenderResult RenderDebug(int zoom)
        {
            int tileSize = 8 * zoom;

            int width  = _map.Width  * tileSize * AssumedMetaWidth;
            int height = _map.Height * tileSize * AssumedMetaHeight;

            var buffer = new byte[width * height * 4];

            // Clear to transparent
            for ( int i = 0 ; i < buffer.Length ; i += 4 )
                buffer[i + 3] = 0;

            //
            // IMPORTANT FIX:
            // Only generate patterns for MAP cells that actually exist
            // AND have a non‑zero MetaTileIndex.
            //
            for ( int y = 0 ; y < _map.Height ; y++ )
            {
                for ( int x = 0 ; x < _map.Width ; x++ )
                {
                    var tile = _map.Tiles[x, y];

                    if ( tile == null )
                        continue;

                    if ( tile.MetaTileIndex == 0 )
                        continue;

                    // Build a debug pattern for this MAP cell
                    var p = new MapPnlDebugPattern
                    {
                        PanelX = x,
                        PanelY = y,
                        WidthInTiles  = AssumedMetaWidth,
                        HeightInTiles = AssumedMetaHeight,
                        OutlineColor  = DebugColorFor(tile.MetaTileIndex)
                    };

                    DrawPattern( buffer , width , height , tileSize , p );
                }
            }

            return new RenderResult
            {
                Buffer = buffer ,
                Width = width ,
                Height = height
            };
        }

        private Color DebugColorFor(int index)
        {
            // Simple deterministic color for debugging
            byte r = (byte)((index * 53) & 0xFF);
            byte g = (byte)((index * 97) & 0xFF);
            byte b = (byte)((index * 193) & 0xFF);
            return Color.FromRgb( r , g , b );
        }

        private void DrawPattern(
            byte[] buf ,
            int width ,
            int height ,
            int tileSize ,
            MapPnlDebugPattern p)
        {
            int px0 = p.PanelX * tileSize * AssumedMetaWidth;
            int py0 = p.PanelY * tileSize * AssumedMetaHeight;

            int w = p.WidthInTiles  * tileSize;
            int h = p.HeightInTiles * tileSize;

            // Outer white border
            DrawRect( buf , width , height , px0 , py0 , w , h , Colors.Black );

            // Inner coloured border
            DrawRect( buf , width , height , px0 + 1 , py0 + 1 , w - 2 , h - 2 , p.OutlineColor );

            // Bottom‑right corner marker (FIXED)
            DrawCornerMarker( buf , width , height , px0 , py0 , tileSize , p.OutlineColor );

        }

        private void DrawRect(
            byte[] buf ,
            int width ,
            int height ,
            int x0 ,
            int y0 ,
            int w ,
            int h ,
            Color c)
        {
            for ( int x = 0 ; x < w ; x++ )
            {
                SetPixel( buf , width , height , x0 + x , y0 , c );
                SetPixel( buf , width , height , x0 + x , y0 + h - 1 , c );
            }

            for ( int y = 0 ; y < h ; y++ )
            {
                SetPixel( buf , width , height , x0 , y0 + y , c );
                SetPixel( buf , width , height , x0 + w - 1 , y0 + y , c );
            }
        }

        private void DrawCornerMarker(
            byte[] buf ,
            int width ,
            int height ,
            int x0 ,
            int y0 ,
            int tileSize ,
            Color c)
        {
            // Bottom‑right of the *origin tile*, not the whole meta‑tile
            int mx = x0 + tileSize - 2;
            int my = y0 + tileSize - 2;

            for ( int y = 0 ; y < 2 ; y++ )
                for ( int x = 0 ; x < 2 ; x++ )
                    SetPixel( buf , width , height , mx + x , my + y , c );
        }


        private void SetPixel(byte[] buf , int width , int height , int x , int y , Color c)
        {
            if ( x < 0 || x >= width || y < 0 || y >= height )
                return;

            int i = (y * width + x) * 4;
            buf[i + 0] = c.B;
            buf[i + 1] = c.G;
            buf[i + 2] = c.R;
            buf[i + 3] = 255;
        }
    }
}
