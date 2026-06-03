using SNESassetsWPF.Models;
using SNESassetsWPF.Formats;
using System;

namespace SNESassetsWPF.Services
{
    public class CgxFileParser
    {
        // ------------------------------------------------------------
        // SNES tile sizes in bytes (editor-side CGX uses same layout)
        // ------------------------------------------------------------
        private const int TileSize2Bpp = 16;  // 2 bytes per row × 8 rows
        private const int TileSize4Bpp = 32;  // 4 bytes per row × 8 rows
        private const int TileSize8Bpp = 64;  // 8 bytes per row × 8 rows

        // ------------------------------------------------------------
        // Prefix table bit masks (S‑CG‑CAD editor-side)
        // ------------------------------------------------------------
        private const int PrefixPaletteMask = 0x0F; // lower 4 bits = palette group

        // NOTE:
        // FlipX / FlipY / Priority are UNKNOWN in S‑CG‑CAD CGX prefix format.
        // Until we confirm the bit layout, we leave them unset (false/0).
        // ------------------------------------------------------------

        public CgxFile Parse(CgxFileReadResult raw)
        {
            if ( !raw.IsValid )
                throw new InvalidOperationException( raw.ErrorMessage );

            var cgx = new CgxFile
            {
                RawFile     = raw.RawFile,
                BitDepth    = raw.BitDepth,
                Metadata    = raw.Metadata,
                RawTileData = raw.TileData,
                TilePrefixTable = raw.PrefixTable
            };

            // Determine bytes per tile
            int bytesPerTile = raw.BitDepth switch
            {
                2 => TileSize2Bpp,
                4 => TileSize4Bpp,
                8 => TileSize8Bpp,
                _ => TileSize4Bpp
            };

            cgx.BytesPerTile = bytesPerTile;
            cgx.TileCount = raw.TileData.Length / bytesPerTile;
            cgx.Tiles = new CgxTile[cgx.TileCount];

            // Ensure prefix table exists
            if ( cgx.TilePrefixTable == null || cgx.TilePrefixTable.Length == 0 )
                cgx.TilePrefixTable = new byte[cgx.TileCount];

            // ------------------------------------------------------------
            // Decode each tile
            // ------------------------------------------------------------
            for ( int t = 0 ; t < cgx.TileCount ; t++ )
            {
                int tileOffset = t * bytesPerTile;

                // Decode pixels
                byte[,] decodedPixels = DecodeTile(raw.TileData, tileOffset, raw.BitDepth);

                // Extract raw bytes for inspection
                byte[] rawBytes = new byte[bytesPerTile];
                Buffer.BlockCopy( raw.TileData , tileOffset , rawBytes , 0 , bytesPerTile );

                // Safe prefix fetch
                byte prefix = (t < cgx.TilePrefixTable.Length)
            ? cgx.TilePrefixTable[t]
            : (byte)0;

                cgx.Tiles[t] = new CgxTile
                {
                    TileIndex = t ,
                    BitDepth = raw.BitDepth ,
                    RawBytes = rawBytes ,
                    Pixels = decodedPixels ,
                    PaletteGroup = prefix & PrefixPaletteMask ,
                    // FlipX / FlipY / Priority remain unset until prefix format confirmed
                };
            }

            return cgx;
        }


        // ------------------------------------------------------------
        // Tile decoding dispatcher
        // ------------------------------------------------------------
        private byte[,] DecodeTile(byte[] data , int offset , int bitDepth) =>
            bitDepth switch
            {
                2 => Decode2bppTile( data , offset ),
                4 => Decode4bppTile( data , offset ),
                8 => Decode8bppTile( data , offset ),
                _ => Decode4bppTile( data , offset )
            };

        // ------------------------------------------------------------
        // 2bpp tile decode (SNES bitplane format)
        // ------------------------------------------------------------
        private byte[,] Decode2bppTile(byte[] data , int offset)
        {
            var tile = new byte[8, 8];

            const int BytesPerRow = 2;

            for ( int row = 0 ; row < 8 ; row++ )
            {
                int rowOffset = offset + (row * BytesPerRow);

                byte plane0 = data[rowOffset + 0];
                byte plane1 = data[rowOffset + 1];

                for ( int col = 0 ; col < 8 ; col++ )
                {
                    int bit = 7 - col;

                    int b0 = (plane0 >> bit) & 1;
                    int b1 = (plane1 >> bit) & 1;

                    tile[row , col] = (byte)( b0 | ( b1 << 1 ) );
                }
            }

            return tile;
        }

        // ------------------------------------------------------------
        // 4bpp tile decode (SNES bitplane format)
        // ------------------------------------------------------------
        private byte[,] Decode4bppTile(byte[] data , int offset)
        {
            var tile = new byte[8, 8];

            const int BytesPerRow = 2;
            const int Plane2Offset = 16; // bytes after plane 0/1

            for ( int row = 0 ; row < 8 ; row++ )
            {
                int rowOffset = offset + (row * BytesPerRow);

                byte p0 = data[rowOffset + 0];
                byte p1 = data[rowOffset + 1];
                byte p2 = data[rowOffset + Plane2Offset + 0];
                byte p3 = data[rowOffset + Plane2Offset + 1];

                for ( int col = 0 ; col < 8 ; col++ )
                {
                    int bit = 7 - col;

                    int b0 = (p0 >> bit) & 1;
                    int b1 = (p1 >> bit) & 1;
                    int b2 = (p2 >> bit) & 1;
                    int b3 = (p3 >> bit) & 1;

                    tile[row , col] = (byte)(
                        b0 |
                        ( b1 << 1 ) |
                        ( b2 << 2 ) |
                        ( b3 << 3 )
                    );
                }
            }

            return tile;
        }

        // ------------------------------------------------------------
        // 8bpp tile decode (SNES bitplane format)
        // ------------------------------------------------------------
        private byte[,] Decode8bppTile(byte[] data , int offset)
        {
            var tile = new byte[8, 8];

            const int BytesPerRow = 2;
            const int Plane2Offset = 16;
            const int Plane4Offset = 32;
            const int Plane6Offset = 48;

            for ( int row = 0 ; row < 8 ; row++ )
            {
                int rowOffset = offset + (row * BytesPerRow);

                byte p0 = data[rowOffset + 0];
                byte p1 = data[rowOffset + 1];
                byte p2 = data[rowOffset + Plane2Offset + 0];
                byte p3 = data[rowOffset + Plane2Offset + 1];
                byte p4 = data[rowOffset + Plane4Offset + 0];
                byte p5 = data[rowOffset + Plane4Offset + 1];
                byte p6 = data[rowOffset + Plane6Offset + 0];
                byte p7 = data[rowOffset + Plane6Offset + 1];

                for ( int col = 0 ; col < 8 ; col++ )
                {
                    int bit = 7 - col;

                    int b0 = (p0 >> bit) & 1;
                    int b1 = (p1 >> bit) & 1;
                    int b2 = (p2 >> bit) & 1;
                    int b3 = (p3 >> bit) & 1;
                    int b4 = (p4 >> bit) & 1;
                    int b5 = (p5 >> bit) & 1;
                    int b6 = (p6 >> bit) & 1;
                    int b7 = (p7 >> bit) & 1;

                    tile[row , col] = (byte)(
                        b0 |
                        ( b1 << 1 ) |
                        ( b2 << 2 ) |
                        ( b3 << 3 ) |
                        ( b4 << 4 ) |
                        ( b5 << 5 ) |
                        ( b6 << 6 ) |
                        ( b7 << 7 )
                    );
                }
            }

            return tile;
        }



        // ------------------------------------------------------------
        // Reinterpret bit depth (editor-side override)
        // Re-decodes all tiles using the new bit depth.
        // ------------------------------------------------------------
        public void ReinterpretBitDepth(CgxFile cgx , int newBitDepth)
        {
            // Update bit depth
            cgx.BitDepth = newBitDepth;

            // Determine bytes per tile
            int bytesPerTile = newBitDepth switch
            {
                2 => TileSize2Bpp,
                4 => TileSize4Bpp,
                8 => TileSize8Bpp,
                _ => TileSize4Bpp
            };

            cgx.BytesPerTile = bytesPerTile;
            cgx.TileCount = cgx.RawTileData.Length / bytesPerTile;

            // Recreate tile array
            cgx.Tiles = new CgxTile[cgx.TileCount];

            for ( int t = 0 ; t < cgx.TileCount ; t++ )
            {
                int tileOffset = t * bytesPerTile;

                // Decode pixels using new bit depth
                byte[,] decodedPixels = DecodeTile(
            cgx.RawTileData,
            tileOffset,
            newBitDepth
        );

                // Extract raw bytes for inspection
                byte[] rawBytes = new byte[bytesPerTile];
                Buffer.BlockCopy( cgx.RawTileData , tileOffset , rawBytes , 0 , bytesPerTile );

                // Safe prefix fetch
                byte prefix = (t < cgx.TilePrefixTable.Length)
            ? cgx.TilePrefixTable[t]
            : (byte)0;

                cgx.Tiles[t] = new CgxTile
                {
                    TileIndex = t ,
                    BitDepth = newBitDepth ,
                    RawBytes = rawBytes ,
                    Pixels = decodedPixels ,
                    PaletteGroup = prefix & PrefixPaletteMask ,
                    // FlipX / FlipY / Priority left unset until prefix bit layout confirmed
                };
            }

            // Resize prefix table if needed
            if ( cgx.TilePrefixTable == null || cgx.TilePrefixTable.Length != cgx.TileCount )
            {
                var prefix = cgx.TilePrefixTable;
                Array.Resize( ref prefix , cgx.TileCount );
                cgx.TilePrefixTable = prefix;
            }
        }


    }
}
