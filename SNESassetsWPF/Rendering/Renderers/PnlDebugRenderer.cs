using System;
using System.Windows.Media;
using SNESassetsWPF.Formats;

namespace SNESassetsWPF.Rendering
{
    /// <summary>
    /// Renders a PNL panel with debug pattern overlays.
    /// Produces a BGRA32 buffer wrapped in a RenderResult.
    /// </summary>
    public static class PnlDebugRenderer
    {
        public static RenderResult Render(PnlFile pnl , PnlDebugPattern[] patterns , int tileSize = 8)
        {
            int width = PnlFile.PanelWidth * tileSize;
            int height = PnlFile.PanelHeight * tileSize;

            var buffer = new byte[width * height * 4];

            // Optional: fill background with transparent or dark grey
            Clear( buffer , width , height , Colors.Transparent );

            // Draw pattern fills first (so borders appear on top)
            foreach ( var p in patterns )
                DrawPatternFill( buffer , width , height , p , tileSize );

            // Draw borders on top
            foreach ( var p in patterns )
            {
                DrawPatternBorders( buffer , width , height , p , tileSize );
                DrawOriginTileMarker( buffer , width , height , p , tileSize );
            }

            return new RenderResult
            {
                Buffer = buffer ,
                Width = width ,
                Height = height
            };
        }

        // ───────────────────────────────────────────────────────────────
        // Helpers
        // ───────────────────────────────────────────────────────────────

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

        /// <summary>
        /// Draws the low opacity fill colour for the Pattern
        /// </summary>
        /// <param name="buf"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <param name="p"></param>
        /// <param name="tileSize"></param>
        private static void DrawPatternFill(byte[] buf , int width , int height , PnlDebugPattern p , int tileSize)
        {
            int px = p.PanelX * tileSize;
            int py = p.PanelY * tileSize;
            int pw = p.WidthInTiles * tileSize;
            int ph = p.HeightInTiles * tileSize;

            var c = p.FillColor;

            for ( int y = py ; y < py + ph ; y++ )
            {
                if ( y < 0 || y >= height ) continue;

                int row = y * width * 4;

                for ( int x = px ; x < px + pw ; x++ )
                {
                    if ( x < 0 || x >= width ) continue;

                    int i = row + x * 4;

                    // Alpha blend fill over background
                    byte a = c.A;
                    if ( a == 0 ) continue;

                    byte br = buf[i + 2];
                    byte bg = buf[i + 1];
                    byte bb = buf[i + 0];

                    buf[i + 2] = (byte)( ( c.R * a + br * ( 255 - a ) ) / 255 );
                    buf[i + 1] = (byte)( ( c.G * a + bg * ( 255 - a ) ) / 255 );
                    buf[i + 0] = (byte)( ( c.B * a + bb * ( 255 - a ) ) / 255 );
                }
            }
        }

        /// <summary>
        /// Draws Borders around the width and height of the Pattern
        /// </summary>
        /// <param name="buf"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <param name="p"></param>
        /// <param name="tileSize"></param>
        private static void DrawPatternBorders(byte[] buf , int width , int height , PnlDebugPattern p , int tileSize)
        {
            int px = p.PanelX * tileSize;
            int py = p.PanelY * tileSize;
            int pw = p.WidthInTiles * tileSize;
            int ph = p.HeightInTiles * tileSize;

            // Outer border (white)
            DrawRect( buf , width , height , px , py , pw , ph , Colors.White , p.OuterBorderThickness );

            // Inner border (pattern colour)
            DrawRect( buf , width , height , px , py , pw , ph , p.OutlineColor , p.InnerBorderThickness );
        }

        private static void DrawRect(byte[] buf , int width , int height ,
                                     int x , int y , int w , int h ,
                                     Color c , double thickness)
        {
            int t = (int)Math.Max(1, thickness);

            // Top
            DrawFilledRect( buf , width , height , x , y , w , t , c );

            // Bottom
            DrawFilledRect( buf , width , height , x , y + h - t , w , t , c );

            // Left
            DrawFilledRect( buf , width , height , x , y , t , h , c );

            // Right
            DrawFilledRect( buf , width , height , x + w - t , y , t , h , c );
        }

        /// <summary>
        /// Draws a filled rectangle
        /// the extent of the Pattern
        /// </summary>
        /// <param name="buf"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="w"></param>
        /// <param name="h"></param>
        /// <param name="c"></param>
        private static void DrawFilledRect(byte[] buf , int width , int height ,
                                           int x , int y , int w , int h , Color c)
        {
            for ( int yy = y ; yy < y + h ; yy++ )
            {
                if ( yy < 0 || yy >= height ) continue;

                int row = yy * width * 4;

                for ( int xx = x ; xx < x + w ; xx++ )
                {
                    if ( xx < 0 || xx >= width ) continue;

                    int i = row + xx * 4;

                    buf[i + 0] = c.B;
                    buf[i + 1] = c.G;
                    buf[i + 2] = c.R;
                    buf[i + 3] = 255;
                }
            }
        }

        /// <summary>
        /// Draws a border marking the Tile that acts as the position
        /// for the Pattern
        /// </summary>
        /// <param name="buf"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <param name="p"></param>
        /// <param name="tileSize"></param>
        private static void DrawOriginTileMarker(byte[] buf , int width , int height , PnlDebugPattern p , int tileSize)
        {
            int originX = p.PanelX * tileSize;
            int originY = p.PanelY * tileSize;

            // Offset 2px down/right from the pattern outline
            int markerX = originX + 2;
            int markerY = originY + 2;
            int markerSize = tileSize - 2;

            // Draw bottom and right edges of the origin tile
            DrawFilledRect( buf , width , height , markerX , markerY + markerSize - 1 , markerSize , 1 , p.OutlineColor ); // bottom edge
            DrawFilledRect( buf , width , height , markerX + markerSize - 1 , markerY , 1 , markerSize , p.OutlineColor ); // right edge
        }

    }
}
