using System;
using System.Collections.Generic;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using SNESassetsWPF.Rendering.Fonts;

namespace SNESassetsWPF.Rendering
{
    public static class HeaderGenerator
    {
        // ------------------------------------------------------------
        //  FONT SELECTION
        // ------------------------------------------------------------
        private static Dictionary<char , FontGlyph> SelectFont(int zoom)
        {
            if ( zoom >= 4 ) return BitmapFonts.bmpFont_4; // 12×32
            if ( zoom == 3 ) return BitmapFonts.bmpFont_3; // 10×24
            if ( zoom == 2 ) return BitmapFonts.bmpFont_2; // 6×16
            return BitmapFonts.bmpFont_1;                // 4×8
        }

        private static int GetGlyphWidth(Dictionary<char , FontGlyph> font)
            => font.TryGetValue( '0' , out var g ) ? g.Width : 4;

        private static int GetGlyphHeight(Dictionary<char , FontGlyph> font)
            => font.TryGetValue( '0' , out var g ) ? g.Height : 8;

        // ------------------------------------------------------------
        //  COLUMN HEADER (0..F)
        // ------------------------------------------------------------
        public static WriteableBitmap GenerateColumnHeader(
            int tileColumns ,
            int zoom ,
            bool showGrid)
        {
            var font = SelectFont(zoom);
            int gWidth = GetGlyphWidth(font);
            int gHeight = GetGlyphHeight(font);

            int tileSize = 8 * zoom;
            int spacing = (showGrid && zoom >= 2) ? 1 : 0;

            int width = (tileColumns * tileSize) + ((tileColumns - 1) * spacing);
            int height = tileSize;
            int stride = width * 4;

            byte[] buffer = new byte[stride * height];

            int glyphOffsetY = (tileSize - gHeight) / 2;

            for ( int col = 0 ; col < tileColumns ; col++ )
            {
                string hex = col.ToString("X1");

                // Match CGXRenderer tile origin
                int cellX = (col * tileSize) + (col * spacing);
                int glyphOffsetX = (tileSize - gWidth) / 2;

                DrawHeaderGlyph(
                    buffer , width , height , stride ,
                    font ,
                    hex[0] ,
                    cellX + glyphOffsetX ,
                    glyphOffsetY );
            }

            if ( showGrid && zoom >= 2 )
                DrawVerticalGridLines( buffer , width , height , stride , tileColumns , spacing , tileSize );

            var wb = new WriteableBitmap(width, height, 96, 96, PixelFormats.Bgra32, null);
            wb.WritePixels( new System.Windows.Int32Rect( 0 , 0 , width , height ) , buffer , stride , 0 );
            return wb;
        }

        // ------------------------------------------------------------
        //  ROW HEADER (000, 010, 020, ...)
        // ------------------------------------------------------------
        public static WriteableBitmap GenerateRowHeader(
            int totalTiles ,
            int tilesPerRow ,
            int zoom ,
            bool showGrid)
        {
            if ( tilesPerRow <= 0 )
                throw new ArgumentOutOfRangeException( nameof( tilesPerRow ) );

            var font = SelectFont(zoom);
            int gWidth = GetGlyphWidth(font);
            int gHeight = GetGlyphHeight(font);

            int tileSize = 8 * zoom;
            int spacing = (showGrid && zoom >= 2) ? 1 : 0;

            int tileRows = (int)Math.Ceiling(totalTiles / (double)tilesPerRow);

            // Add 4 px padding after the third hex digit for visual spacing
            int headerWidth = (gWidth * 3) + 4 + 4;
            int height = (tileRows * tileSize) + ((tileRows - 1) * spacing);
            int stride = headerWidth * 4;

            byte[] buffer = new byte[stride * height];

            int glyphOffsetY = (tileSize - gHeight) / 2;

            int x0 = 1;
            int x1 = x0 + gWidth + 1;
            int x2 = x1 + gWidth + 1;

            for ( int row = 0 ; row < tileRows ; row++ )
            {
                int tileIndex = row * tilesPerRow;
                string hex = tileIndex.ToString("X3");

                // Match CGXRenderer tile origin
                int cellY = (row * tileSize) + (row * spacing);

                DrawHeaderGlyph( buffer , headerWidth , height , stride , font , hex[0] , x0 , cellY + glyphOffsetY );
                DrawHeaderGlyph( buffer , headerWidth , height , stride , font , hex[1] , x1 , cellY + glyphOffsetY );
                DrawHeaderGlyph( buffer , headerWidth , height , stride , font , hex[2] , x2 , cellY + glyphOffsetY );
            }

            if ( showGrid && zoom >= 2 )
                DrawHorizontalGridLines( buffer , headerWidth , height , stride , tileRows , spacing , tileSize );

            var wb = new WriteableBitmap(headerWidth, height, 96, 96, PixelFormats.Bgra32, null);
            wb.WritePixels( new System.Windows.Int32Rect( 0 , 0 , headerWidth , height ) , buffer , stride , 0 );
            return wb;
        }

        // ------------------------------------------------------------
        //  SPACER (top-left corner)
        // ------------------------------------------------------------
        public static WriteableBitmap GenerateSpacer(
            int zoom ,
            bool showGrid)
        {
            var font = SelectFont(zoom);
            int gWidth = GetGlyphWidth(font);

            int tileSize = 8 * zoom;

            // Match row header width including the new 4 px padding
            int headerWidth = (gWidth * 3) + 4 + 4;
            int width = headerWidth;
            int height = tileSize;
            int stride = width * 4;

            byte[] buffer = new byte[stride * height];

            if ( showGrid && zoom >= 2 )
            {
                byte R = 128, G = 128, B = 128, A = 255;

                int x = width - 1;
                for ( int y = 0 ; y < height ; y++ )
                {
                    int idx = (y * stride) + (x * 4);
                    buffer[idx + 0] = B;
                    buffer[idx + 1] = G;
                    buffer[idx + 2] = R;
                    buffer[idx + 3] = A;
                }

                int y2 = height - 1;
                for ( int xx = 0 ; xx < width ; xx++ )
                {
                    int idx = (y2 * stride) + (xx * 4);
                    buffer[idx + 0] = B;
                    buffer[idx + 1] = G;
                    buffer[idx + 2] = R;
                    buffer[idx + 3] = A;
                }
            }

            var wb = new WriteableBitmap(width, height, 96, 96, PixelFormats.Bgra32, null);
            wb.WritePixels( new System.Windows.Int32Rect( 0 , 0 , width , height ) , buffer , stride , 0 );
            return wb;
        }

        // ------------------------------------------------------------
        //  DRAW GLYPH (pixel‑perfect)
        // ------------------------------------------------------------
        private static void DrawHeaderGlyph(
            byte[] buffer ,
            int bmpWidth ,
            int bmpHeight ,
            int stride ,
            Dictionary<char , FontGlyph> font ,
            char ch ,
            int destX ,
            int destY)
        {
            if ( !font.TryGetValue( ch , out FontGlyph glyph ) )
                return;

            for ( int gy = 0 ; gy < glyph.Height ; gy++ )
            {
                int rowOffset = gy * glyph.BytesPerRow;

                for ( int gx = 0 ; gx < glyph.Width ; gx++ )
                {
                    int byteIndex = rowOffset + (gx / 8);
                    int bitIndex = 7 - (gx % 8);

                    if ( byteIndex >= glyph.Data.Length )
                        continue;

                    int bit = (glyph.Data[byteIndex] >> bitIndex) & 1;
                    if ( bit == 0 )
                        continue;

                    int px = destX + gx;
                    int py = destY + gy;

                    if ( px < 0 || px >= bmpWidth || py < 0 || py >= bmpHeight )
                        continue;

                    int idx = (py * stride) + (px * 4);
                    buffer[idx + 0] = 128;
                    buffer[idx + 1] = 128;
                    buffer[idx + 2] = 128;
                    buffer[idx + 3] = 255;
                }
            }
        }

        // ------------------------------------------------------------
        //  GRID HELPERS
        // ------------------------------------------------------------
        private static void DrawVerticalGridLines(
            byte[] buffer ,
            int width ,
            int height ,
            int stride ,
            int cols ,
            int spacing ,
            int tileSize)
        {
            byte R = 128, G = 128, B = 128, A = 255;

            for ( int col = 1 ; col < cols ; col++ )
            {
                // Match CGXRenderer: line between tiles
                int x = (col * tileSize) + ((col - 1) * spacing);
                if ( x < 0 || x >= width ) continue;

                for ( int y = 0 ; y < height ; y++ )
                {
                    int idx = (y * stride) + (x * 4);
                    buffer[idx + 0] = B;
                    buffer[idx + 1] = G;
                    buffer[idx + 2] = R;
                    buffer[idx + 3] = A;
                }
            }
        }

        private static void DrawHorizontalGridLines(
            byte[] buffer ,
            int width ,
            int height ,
            int stride ,
            int rows ,
            int spacing ,
            int tileSize)
        {
            byte R = 128, G = 128, B = 128, A = 255;

            for ( int row = 1 ; row < rows ; row++ )
            {
                // Match CGXRenderer: line between tiles
                int y = (row * tileSize) + ((row - 1) * spacing);
                if ( y < 0 || y >= height ) continue;

                for ( int x = 0 ; x < width ; x++ )
                {
                    int idx = (y * stride) + (x * 4);
                    buffer[idx + 0] = B;
                    buffer[idx + 1] = G;
                    buffer[idx + 2] = R;
                    buffer[idx + 3] = A;
                }
            }
        }
    }
}
