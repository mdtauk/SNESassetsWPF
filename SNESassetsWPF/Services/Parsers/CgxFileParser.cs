using System;
using System.Diagnostics;
using SNESassetsWPF.Models;
using SNESassetsWPF.Formats;

namespace SNESassetsWPF.Services
{
    /// <summary>
    /// H‑CG‑CAD‑style CGX parser.
    ///
    /// CGX structure (editor-side):
    ///   [0x0000 ...] Raw tile bitplane data (2/4/8bpp)
    ///   [optional]   Metadata block (0x100 bytes, ASCII-ish, ends with 0x00)
    ///   [optional]   Prefix table (0x400 bytes, 1 byte per tile, editor-only)
    ///
    /// This parser:
    ///   • detects bit depth (2/4/8 bpp) using size patterns
    ///   • supports up to 1024 tiles
    ///   • detects presence of metadata and prefix table
    ///   • decodes tiles into 8×8 pixel index arrays
    ///   • applies prefix → PaletteRow for 4bpp when prefix exists
    ///
    /// Bit depth can still be overridden later via ReinterpretBitDepth.
    /// </summary>
    public class CgxFileParser
    {
        public CgxFile Parse(byte[] raw)
        {
            if ( raw == null || raw.Length == 0 )
                throw new Exception( "Empty CGX data." );

            var cgx = new CgxFile
            {
                RawFile = raw
            };

            // ---------------------------------------------------------
            // 1. Detect bit depth and layout from file size
            // ---------------------------------------------------------
            // We assume H‑CG‑CAD-style full sheets: up to 1024 tiles.
            // For each candidate bpp, we check if the file size matches:
            //   tileDataSize = 1024 * bytesPerTile
            //   extra = raw.Length - tileDataSize
            //   extra ∈ { 0, 0x100, 0x400, 0x500 }
            //
            //  extra = 0      → tile data only
            //  extra = 0x100  → tile data + metadata
            //  extra = 0x400  → tile data + prefix
            //  extra = 0x500  → tile data + metadata + prefix
            //
            // If no pattern matches, we fall back to "best guess":
            //   • choose 4bpp
            //   • tileCount = floor(raw.Length / bytesPerTile)
            //   • no metadata/prefix
            int bitDepth;
            int bytesPerTile;
            int tileDataSize;
            int extraSize;
            bool hasMetadata = false;
            bool hasPrefix = false;

            if ( !TryDetectLayout( raw.Length , out bitDepth , out bytesPerTile , out tileDataSize , out extraSize , out hasMetadata , out hasPrefix ) )
            {
                // Fallback: assume 4bpp, no metadata/prefix, variable tile count
                bitDepth = 4;
                bytesPerTile = 32;
                tileDataSize = raw.Length;
                extraSize = 0;
                hasMetadata = false;
                hasPrefix = false;

                Debug.WriteLine( $"[CGX] Unknown size {raw.Length} bytes — assuming 4bpp, no metadata/prefix." );
            }

            cgx.BitDepth = bitDepth;
            cgx.BytesPerTile = bytesPerTile;

            // ---------------------------------------------------------
            // 2. Extract tile data (up to 1024 tiles)
            // ---------------------------------------------------------
            int maxTiles = 1024;
            int maxTileDataSize = bytesPerTile * maxTiles;

            // Clamp tileDataSize to available data
            if ( tileDataSize > raw.Length )
                tileDataSize = raw.Length;

            // Compute tile count from tileDataSize
            int tileCount = tileDataSize / bytesPerTile;
            if ( tileCount > maxTiles )
                tileCount = maxTiles;

            cgx.TileCount = tileCount;

            cgx.RawTileData = new byte[tileCount * bytesPerTile];
            Array.Copy( raw , 0 , cgx.RawTileData , 0 , cgx.RawTileData.Length );

            // ---------------------------------------------------------
            // 3. Extract metadata (if present)
            // ---------------------------------------------------------
            int offset = tileDataSize;
            cgx.Metadata = Array.Empty<byte>();

            if ( hasMetadata && extraSize >= 0x100 && raw.Length >= offset + 0x100 )
            {
                cgx.Metadata = new byte[0x100];
                Array.Copy( raw , offset , cgx.Metadata , 0 , 0x100 );
                offset += 0x100;
            }

            // ---------------------------------------------------------
            // 4. Extract prefix table (if present)
            // ---------------------------------------------------------
            cgx.TilePrefixTable = Array.Empty<byte>();

            if ( hasPrefix && raw.Length >= offset + 0x400 )
            {
                cgx.TilePrefixTable = new byte[0x400];
                Array.Copy( raw , offset , cgx.TilePrefixTable , 0 , 0x400 );
                offset += 0x400;
            }

            // ---------------------------------------------------------
            // 5. Decode tiles
            // ---------------------------------------------------------
            cgx.Tiles = new CgxTile[cgx.TileCount];

            for ( int i = 0 ; i < cgx.TileCount ; i++ )
            {
                cgx.Tiles[i] = DecodeTile(
                    cgx.RawTileData ,
                    i * cgx.BytesPerTile ,
                    cgx.BitDepth
                );
            }

            // ---------------------------------------------------------
            // 6. Apply per‑tile prefix → PaletteRow (4bpp only)
            // ---------------------------------------------------------
            // H‑CG‑CAD uses the prefix byte as:
            //   lower 4 bits = palette row (0–15) for 4bpp
            // For 2bpp/8bpp, we leave PaletteRow at 0 (renderer can ignore or override).
            if ( cgx.BitDepth == 4 &&
                cgx.TilePrefixTable != null &&
                cgx.TilePrefixTable.Length >= cgx.TileCount )
            {
                for ( int t = 0 ; t < cgx.TileCount ; t++ )
                {
                    byte prefix = cgx.TilePrefixTable[t];
                    cgx.Tiles[t].PaletteRow = prefix & 0x0F;
                }
            }

            // ---------------------------------------------------------
            // 7. Default sheet layout (H‑CG‑CAD style)
            // ---------------------------------------------------------
            // H‑CG‑CAD typically uses 32×32 for full sheets, but for fewer tiles
            // we still keep 32×32 as a logical grid; the renderer just won't
            // draw beyond TileCount.
            cgx.TilesX = 32;
            cgx.TilesY = (int)Math.Ceiling( cgx.TileCount / 32.0 );

            return cgx;
        }

        /// <summary>
        /// Try to detect bit depth and layout from file size.
        /// Returns true if a known pattern is matched.
        /// </summary>
        private bool TryDetectLayout(
            int length ,
            out int bitDepth ,
            out int bytesPerTile ,
            out int tileDataSize ,
            out int extraSize ,
            out bool hasMetadata ,
            out bool hasPrefix)
        {
            bitDepth = 0;
            bytesPerTile = 0;
            tileDataSize = 0;
            extraSize = 0;
            hasMetadata = false;
            hasPrefix = false;

            // Candidate bit depths (try higher first)
            int[] bpps = { 8, 4, 2 };

            foreach ( int bpp in bpps )
            {
                int bpt = bpp * 8;          // bytes per tile
                int fullTileData = bpt * 1024; // full 1024-tile sheet

                if ( length < bpt ) // too small for even one tile
                    continue;

                if ( length >= fullTileData )
                {
                    int extra = length - fullTileData;

                    // Known extra sizes: 0, 0x100, 0x400, 0x500
                    if ( extra == 0 || extra == 0x100 || extra == 0x400 || extra == 0x500 )
                    {
                        bitDepth = bpp;
                        bytesPerTile = bpt;
                        tileDataSize = fullTileData;
                        extraSize = extra;

                        hasMetadata = ( extra == 0x100 || extra == 0x500 );
                        hasPrefix = ( extra == 0x400 || extra == 0x500 );

                        return true;
                    }
                }
            }

            // No known pattern matched
            return false;
        }

        // -------------------------------------------------------------
        // Decode a single tile (8×8 pixels) from SNES bitplane format
        // -------------------------------------------------------------
        private CgxTile DecodeTile(byte[] data , int offset , int bpp)
        {
            var tile = new CgxTile();

            switch ( bpp )
            {
                case 2:
                    // 2bpp: 16 bytes per tile
                    for ( int y = 0 ; y < 8 ; y++ )
                    {
                        int rowOffset = offset + (y * 2);
                        byte p0 = data[rowOffset + 0];
                        byte p1 = data[rowOffset + 1];

                        for ( int x = 0 ; x < 8 ; x++ )
                        {
                            int shift = 7 - x;

                            int bit0 = (p0 >> shift) & 1;
                            int bit1 = (p1 >> shift) & 1;

                            int value =
                                (bit0 << 0) |
                                (bit1 << 1);

                            tile.Pixels[y , x] = (byte)value;
                        }
                    }
                    break;

                case 4:
                    // 4bpp: 32 bytes per tile
                    for ( int y = 0 ; y < 8 ; y++ )
                    {
                        int rowOffset01 = offset + (y * 2);        // planes 0–1
                        int rowOffset23 = offset + 16 + (y * 2);   // planes 2–3

                        byte p0 = data[rowOffset01 + 0];
                        byte p1 = data[rowOffset01 + 1];
                        byte p2 = data[rowOffset23 + 0];
                        byte p3 = data[rowOffset23 + 1];

                        for ( int x = 0 ; x < 8 ; x++ )
                        {
                            int shift = 7 - x;

                            int bit0 = (p0 >> shift) & 1;
                            int bit1 = (p1 >> shift) & 1;
                            int bit2 = (p2 >> shift) & 1;
                            int bit3 = (p3 >> shift) & 1;

                            int value =
                                (bit0 << 0) |
                                (bit1 << 1) |
                                (bit2 << 2) |
                                (bit3 << 3);

                            tile.Pixels[y , x] = (byte)value;
                        }
                    }
                    break;

                case 8:
                    // 8bpp: 64 bytes per tile
                    for ( int y = 0 ; y < 8 ; y++ )
                    {
                        int rowOffset01 = offset + (y * 2);          // planes 0–1
                        int rowOffset23 = offset + 16 + (y * 2);     // planes 2–3
                        int rowOffset45 = offset + 32 + (y * 2);     // planes 4–5
                        int rowOffset67 = offset + 48 + (y * 2);     // planes 6–7

                        byte p0 = data[rowOffset01 + 0];
                        byte p1 = data[rowOffset01 + 1];
                        byte p2 = data[rowOffset23 + 0];
                        byte p3 = data[rowOffset23 + 1];
                        byte p4 = data[rowOffset45 + 0];
                        byte p5 = data[rowOffset45 + 1];
                        byte p6 = data[rowOffset67 + 0];
                        byte p7 = data[rowOffset67 + 1];

                        for ( int x = 0 ; x < 8 ; x++ )
                        {
                            int shift = 7 - x;

                            int bit0 = (p0 >> shift) & 1;
                            int bit1 = (p1 >> shift) & 1;
                            int bit2 = (p2 >> shift) & 1;
                            int bit3 = (p3 >> shift) & 1;
                            int bit4 = (p4 >> shift) & 1;
                            int bit5 = (p5 >> shift) & 1;
                            int bit6 = (p6 >> shift) & 1;
                            int bit7 = (p7 >> shift) & 1;

                            int value =
                                (bit0 << 0) |
                                (bit1 << 1) |
                                (bit2 << 2) |
                                (bit3 << 3) |
                                (bit4 << 4) |
                                (bit5 << 5) |
                                (bit6 << 6) |
                                (bit7 << 7);

                            tile.Pixels[y , x] = (byte)value;
                        }
                    }
                    break;

                default:
                    throw new Exception( $"Unsupported CGX bit depth: {bpp}" );
            }

            return tile;
        }

        /// <summary>
        /// Reinterprets the loaded bytes when the bit depth is changed.
        /// Keeps tile count and prefix table; only re-decodes pixels and reapplies PaletteRow.
        /// </summary>
        public void ReinterpretBitDepth(CgxFile cgx , int newBpp)
        {
            cgx.BitDepth = newBpp;
            cgx.BytesPerTile = newBpp * 8;

            for ( int i = 0 ; i < cgx.TileCount ; i++ )
            {
                var tile = DecodeTile(
                    cgx.RawTileData,
                    i * cgx.BytesPerTile,
                    cgx.BitDepth
                );

                // Reapply palette row from prefix table (4bpp only)
                if ( cgx.BitDepth == 4 &&
                    cgx.TilePrefixTable != null &&
                    cgx.TilePrefixTable.Length > i )
                {
                    byte prefix = cgx.TilePrefixTable[i];
                    tile.PaletteRow = prefix & 0x0F;
                }

                cgx.Tiles[i] = tile;
            }
        }
    }
}
