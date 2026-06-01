using SNESassetsWPF.Formats;
using SNESassetsWPF.Models;
using System;
using System.Diagnostics;
using System.Windows.Media;

namespace SNESassetsWPF.Rendering
{
    public class CgxRenderer
    {
        private const int DefaultTilesPerRow = 16;
        private const int TileWidth         = 8;
        private const int TileHeight        = 8;

        // Palette layout in COL: 16 rows × 16 colours
        private const int ColorsPerRow      = 16;
        private const int PaletteRowMask    = 0x0F; // lower 4 bits of index

        /// <summary>
        /// Renders a CGX tile sheet into a BGRA32 buffer.
        /// </summary>
        public RenderResult Render(
            CgxFile cgx ,
            ColFile col ,
            bool forceSingleRow ,
            int forcedRowIndex ,
            int tilesPerRow = DefaultTilesPerRow ,
            int zoom = 1 ,
            bool showGrid = false)
        {
            if ( zoom < 1 )
                zoom = 1;

            // Grid only appears at zoom >= 2
            int spacing = (showGrid && zoom >= 2) ? 1 : 0;

            // Compute layout
            int rows = (int)Math.Ceiling(cgx.TileCount / (double)tilesPerRow);

            int baseWidth  = tilesPerRow * TileWidth;
            int baseHeight = rows        * TileHeight;

            // Add spacing between tiles
            int width  = (baseWidth  * zoom) + ((tilesPerRow - 1) * spacing);
            int height = (baseHeight * zoom) + ((rows        - 1) * spacing);

            Debug.WriteLine( $"CGX tile count: {cgx.TileCount}" );
            Debug.WriteLine( $"Rendering as: {tilesPerRow} × {rows} tiles" );
            Debug.WriteLine( $"Pixel size: {baseWidth} × {baseHeight}" );
            Debug.WriteLine( $"Zoomed size: {width} × {height}" );
            Debug.WriteLine( $"Grid spacing: {spacing}" );

            var buffer = new byte[width * height * 4];

            // ------------------------------------------------------------
            // Render each tile
            // ------------------------------------------------------------
            for ( int t = 0 ; t < cgx.TileCount ; t++ )
            {
                int tileX = t % tilesPerRow;
                int tileY = t / tilesPerRow;

                // TEMP: log a few candidate tiles
                if ( t == 181 || t == 182 || t == 191 || t == 192 || t == 0x11D || t == 0x21D )
                {
                    System.Diagnostics.Debug.WriteLine(
                        $"CGX tile t={t} at sheet pos ({tileX},{tileY})" );
                }

                CgxTile tile = cgx.Tiles[t];
                byte[,] pixels = tile.Pixels;

                // Editor-side palette group from prefix (already decoded in parser)
                int paletteGroup = tile.PaletteGroup & PaletteRowMask;

                // Compute tile origin including spacing
                int tileOriginX = (tileX * TileWidth  * zoom) + (tileX * spacing);
                int tileOriginY = (tileY * TileHeight * zoom) + (tileY * spacing);

                for ( int y = 0 ; y < TileHeight ; y++ )
                {
                    for ( int x = 0 ; x < TileWidth ; x++ )
                    {
                        byte baseIndex = pixels[y, x]; // 0–3 / 0–15 / 0–255 depending on bpp

                        // For 2/4bpp, baseIndex is the colour within the palette group.
                        // For 8bpp, PaletteGroup is typically 0 and baseIndex is full index.
                        int paletteIndex = ComputePaletteIndex(
                            cgx.BitDepth,
                            paletteGroup,
                            baseIndex
                        );

                        if ( forceSingleRow )
                        {
                            // Force all colours into a single palette row for debugging.
                            int colorInRow = paletteIndex & PaletteRowMask;
                            paletteIndex = ( forcedRowIndex * ColorsPerRow ) + colorInRow;
                        }

                        Color c = ResolveColor(col, paletteIndex);

                        // Write zoomed pixel block
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

            // ------------------------------------------------------------
            // Draw grid lines (only when enabled and zoom >= 2)
            // ------------------------------------------------------------
            if ( showGrid && zoom >= 2 )
            {
                byte gridR = 128;
                byte gridG = 128;
                byte gridB = 128;
                byte gridA = 255;

                // Vertical grid lines
                for ( int tx = 1 ; tx < tilesPerRow ; tx++ )
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
                for ( int ty = 1 ; ty < rows ; ty++ )
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

        // ------------------------------------------------------------
        // Palette index computation (editor-side CGX semantics)
        // ------------------------------------------------------------
        private static int ComputePaletteIndex(int bitDepth , int paletteGroup , byte baseIndex)
        {
            return bitDepth switch
            {
                // 2bpp: 4 colours per group
                2 => ( paletteGroup << 2 ) | ( baseIndex & 0x03 ),

                // 4bpp: 16 colours per group
                4 => ( paletteGroup << 4 ) | ( baseIndex & 0x0F ),

                // 8bpp: full 0–255 index, paletteGroup usually ignored
                8 => baseIndex,

                _ => baseIndex
            };
        }

        // ------------------------------------------------------------
        // Palette lookup
        // ------------------------------------------------------------
        private static Color ResolveColor(ColFile col , int paletteIndex)
        {
            int row    = paletteIndex / ColorsPerRow;
            int colIdx = paletteIndex % ColorsPerRow;

            return col.GetColor( row , colIdx );
        }
    }
}
