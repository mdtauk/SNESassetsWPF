using System;
using System.Diagnostics;
using SNESassetsWPF.Models;

namespace SNESassetsWPF.Formats
{
    /// <summary>
    /// H‑CG‑CAD‑style CGX parser with strict and partial modes.
    /// </summary>
    public class CgxFileParser
    {
        // ============================================================
        // CONSTANTS (no magic numbers)
        // ============================================================
        private const int MaxTiles = 1024;

        private const int BytesPerTile2Bpp = 16;
        private const int BytesPerTile4Bpp = 32;
        private const int BytesPerTile8Bpp = 64;

        private const int MetadataSize = 0x100;
        private const int PrefixTableSize = 0x400;

        // Known full‑sheet sizes (tile data only)
        private const int FullSheet2BppBytes = MaxTiles * BytesPerTile2Bpp;
        private const int FullSheet4BppBytes = MaxTiles * BytesPerTile4Bpp;
        private const int FullSheet8BppBytes = MaxTiles * BytesPerTile8Bpp;

        // ============================================================
        // WARNING HELPER (dedupe by Short)
        // ============================================================
        private static void AddWarning(CgxFileReadResult result , string longText , string shortText)
        {
            foreach ( var w in result.Warnings )
            {
                if ( w.Short == shortText )
                    return;
            }

            result.Warnings.Add( new CgxFileReadResult.CgxWarning
            {
                Long = longText ,
                Short = shortText
            } );
        }

        // ============================================================
        // CLASSIFY STRUCTURE
        // ============================================================
        public static void ClassifyCgxStructure(CgxFileReadResult result)
        {
            byte[] data = result.RawFile;
            int length = data.Length;

            result.RawTileData = Array.Empty<byte>();
            result.RawPrefixTable = Array.Empty<byte>();
            result.RawMetadata = Array.Empty<byte>();
            result.Warnings.Clear();
            result.Format = CgxFileReadResult.CgxFormatType.Valid;

            if ( !result.Success || data == null || length == 0 )
            {
                result.Format = CgxFileReadResult.CgxFormatType.Fail;
                AddWarning( result ,
                    "CGX file is empty or unreadable." ,
                    "Empty or unreadable CGX." );
                return;
            }

            // --------------------------------------------------------
            // 1. Try to detect full‑sheet layout (H‑CG‑CAD style)
            // --------------------------------------------------------
            int bitDepth = 0;
            int bytesPerTile = 0;
            int tileDataSize = 0;
            int extraSize = 0;
            bool hasMetadata = false;
            bool hasPrefix = false;

            if ( !TryDetectFullSheetLayout(
                    length ,
                    out bitDepth ,
                    out bytesPerTile ,
                    out tileDataSize ,
                    out extraSize ,
                    out hasMetadata ,
                    out hasPrefix ) )
            {
                // Fallback: assume 4bpp, no guaranteed metadata/prefix.
                bitDepth = 4;
                bytesPerTile = BytesPerTile4Bpp;
                tileDataSize = length;
                extraSize = 0;
                hasMetadata = false;
                hasPrefix = false;

                AddWarning( result ,
                    "CGX size does not match any known full‑sheet pattern. Assuming 4bpp with no metadata or prefix table." ,
                    "Unknown size, assumed 4bpp." );
                result.Format = CgxFileReadResult.CgxFormatType.Warn;
            }

            // --------------------------------------------------------
            // 2. Split regions based on detected layout
            // --------------------------------------------------------
            // Clamp tileDataSize to available data
            if ( tileDataSize > length )
                tileDataSize = length;

            int tileCountBytes = tileDataSize;
            int tileCount = tileCountBytes / bytesPerTile;
            int tileRemainder = tileCountBytes % bytesPerTile;

            if ( tileCount == 0 )
            {
                result.Format = CgxFileReadResult.CgxFormatType.Fail;
                AddWarning( result ,
                    "CGX file does not contain enough bytes for a single tile at the assumed bit depth." ,
                    "No complete tiles." );
                result.RawTileData = Array.Empty<byte>();
                result.RawMetadata = data;
                return;
            }

            if ( tileRemainder != 0 )
            {
                // Truncated tile data
                result.Format = CgxFileReadResult.CgxFormatType.Warn;
                AddWarning( result ,
                    $"CGX tile data is truncated. {tileRemainder} trailing bytes do not form a complete tile and will be ignored." ,
                    "Truncated tile data." );
                tileCountBytes = tileCount * bytesPerTile;
            }

            result.RawTileData = new byte[tileCountBytes];
            Array.Copy( data , 0 , result.RawTileData , 0 , tileCountBytes );

            int offset = tileCountBytes;
            int remaining = length - offset;

            // --------------------------------------------------------
            // 3. Metadata and prefix table (only trusted for full‑sheet)
            // --------------------------------------------------------
            if ( hasMetadata || hasPrefix )
            {
                // We only trust the extra layout when full‑sheet pattern matched.
                // extraSize = remaining bytes after tileDataSize.
                // But tileDataSize may have been clamped; recompute remaining.
                remaining = length - tileDataSize;
                offset = tileDataSize;

                // Metadata (up to 0x100)
                if ( hasMetadata )
                {
                    if ( remaining >= MetadataSize )
                    {
                        result.RawMetadata = new byte[MetadataSize];
                        Array.Copy( data , offset , result.RawMetadata , 0 , MetadataSize );
                        offset += MetadataSize;
                        remaining -= MetadataSize;
                    }
                    else
                    {
                        // Incomplete metadata
                        result.Format = CgxFileReadResult.CgxFormatType.Warn;
                        AddWarning( result ,
                            "CGX metadata/footer region is incomplete." ,
                            "Incomplete metadata." );
                        result.RawMetadata = new byte[remaining];
                        Array.Copy( data , offset , result.RawMetadata , 0 , remaining );
                        remaining = 0;
                    }
                }

                // Prefix table (up to 0x400)
                if ( hasPrefix && remaining > 0 )
                {
                    if ( remaining >= PrefixTableSize )
                    {
                        result.RawPrefixTable = new byte[PrefixTableSize];
                        Array.Copy( data , offset , result.RawPrefixTable , 0 , PrefixTableSize );
                        offset += PrefixTableSize;
                        remaining -= PrefixTableSize;
                    }
                    else
                    {
                        result.Format = CgxFileReadResult.CgxFormatType.Warn;
                        AddWarning( result ,
                            "CGX prefix table region is incomplete." ,
                            "Incomplete prefix table." );
                        result.RawPrefixTable = new byte[remaining];
                        Array.Copy( data , offset , result.RawPrefixTable , 0 , remaining );
                        remaining = 0;
                    }
                }

                // Any leftover bytes after expected regions
                if ( remaining > 0 )
                {
                    result.Format = CgxFileReadResult.CgxFormatType.Warn;
                    AddWarning( result ,
                        "CGX file contains extra data beyond expected tile/metadata/prefix regions." ,
                        "Extra trailing data." );
                }
            }
            else
            {
                // No trusted metadata/prefix layout → treat all remaining as metadata-ish junk.
                if ( remaining > 0 )
                {
                    result.RawMetadata = new byte[remaining];
                    Array.Copy( data , offset , result.RawMetadata , 0 , remaining );

                    result.Format = CgxFileReadResult.CgxFormatType.Warn;
                    AddWarning( result ,
                        "CGX file contains extra data beyond tile region. Treated as metadata/junk." ,
                        "Extra data after tiles." );
                }
            }

            // Store bit depth info in Parsed later; here we just classify.
        }

        /// <summary>
        /// Try to detect full‑sheet layout (1024 tiles) and extra regions.
        /// </summary>
        private static bool TryDetectFullSheetLayout(
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
                int bpt = bpp switch
                {
                    2 => BytesPerTile2Bpp,
                    4 => BytesPerTile4Bpp,
                    8 => BytesPerTile8Bpp,
                    _ => 0
                };

                if ( bpt == 0 )
                    continue;

                int fullTileData = bpt * MaxTiles;

                if ( length < fullTileData )
                    continue;

                int extra = length - fullTileData;

                // Known extra sizes: 0, 0x100, 0x400, 0x500
                if ( extra == 0 || extra == MetadataSize || extra == PrefixTableSize || extra == MetadataSize + PrefixTableSize )
                {
                    bitDepth = bpp;
                    bytesPerTile = bpt;
                    tileDataSize = fullTileData;
                    extraSize = extra;

                    hasMetadata = ( extra == MetadataSize || extra == MetadataSize + PrefixTableSize );
                    hasPrefix = ( extra == PrefixTableSize || extra == MetadataSize + PrefixTableSize );

                    return true;
                }
            }

            return false;
        }

        // ============================================================
        // STRICT PARSER (no exceptions, uses classification)
        // ============================================================
        public static CgxFile ParseStrict(CgxFileReadResult result)
        {
            if ( !result.Success || result.RawTileData == null || result.RawTileData.Length == 0 )
                return null;

            // In strict mode we only accept:
            //  - full‑sheet layout detected OR
            //  - no truncated tiles
            //  - no incomplete metadata/prefix
            // But classification already downgraded to Warn when partial.
            if ( result.Format != CgxFileReadResult.CgxFormatType.Valid )
                return null;

            // We need to know bit depth. For strict, we trust full‑sheet detection:
            int bitDepth;
            int bytesPerTile;

            if ( !TryInferBitDepthFromTileData( result.RawTileData.Length , out bitDepth , out bytesPerTile ) )
            {
                AddWarning( result ,
                    "Unable to infer CGX bit depth in strict mode." ,
                    "Unknown bit depth (strict)." );
                result.Format = CgxFileReadResult.CgxFormatType.Fail;
                return null;
            }

            var cgx = new CgxFile
            {
                RawFile = result.RawFile,
                BitDepth = bitDepth,
                BytesPerTile = bytesPerTile
            };

            int tileCount = result.RawTileData.Length / bytesPerTile;
            cgx.TileCount = tileCount;
            cgx.RawTileData = new byte[result.RawTileData.Length];
            Array.Copy( result.RawTileData , cgx.RawTileData , result.RawTileData.Length );

            // Metadata / prefix as‑is
            cgx.Metadata = result.RawMetadata ?? Array.Empty<byte>();
            cgx.TilePrefixTable = result.RawPrefixTable ?? Array.Empty<byte>();

            // Decode tiles
            cgx.Tiles = new CgxTile[cgx.TileCount];

            for ( int i = 0 ; i < cgx.TileCount ; i++ )
            {
                cgx.Tiles[i] = DecodeTile(
                    cgx.RawTileData ,
                    i * cgx.BytesPerTile ,
                    cgx.BitDepth );
            }

            // Apply prefix → PaletteRow (4bpp only)
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

            // Default sheet layout (H‑CG‑CAD style)
            cgx.TilesX = 32;
            cgx.TilesY = (int)Math.Ceiling( cgx.TileCount / 32.0 );

            result.Parsed = cgx;
            return cgx;
        }

        // ============================================================
        // PARTIAL PARSER (best‑effort, no exceptions)
        // ============================================================
        public static CgxFile ParsePartial(CgxFileReadResult result)
        {
            if ( !result.Success || result.RawTileData == null || result.RawTileData.Length == 0 )
            {
                AddWarning( result ,
                    "CGX file has no usable tile data. Nothing to render." ,
                    "No tile data." );
                result.Format = CgxFileReadResult.CgxFormatType.Fail;
                return null;
            }

            // Bit depth: try to infer from full‑sheet; otherwise assume 4bpp.
            int bitDepth;
            int bytesPerTile;

            if ( !TryInferBitDepthFromTileData( result.RawTileData.Length , out bitDepth , out bytesPerTile ) )
            {
                bitDepth = 4;
                bytesPerTile = BytesPerTile4Bpp;

                AddWarning( result ,
                    "Unable to infer CGX bit depth from size. Assuming 4bpp." ,
                    "Assumed 4bpp." );
            }

            int tileCount = result.RawTileData.Length / bytesPerTile;
            if ( tileCount == 0 )
            {
                AddWarning( result ,
                    "CGX tile data is too small for even one tile at the assumed bit depth." ,
                    "No complete tiles." );
                result.Format = CgxFileReadResult.CgxFormatType.Fail;
                return null;
            }

            var cgx = new CgxFile
            {
                RawFile = result.RawFile,
                BitDepth = bitDepth,
                BytesPerTile = bytesPerTile,
                TileCount = tileCount
            };

            cgx.RawTileData = new byte[tileCount * bytesPerTile];
            Array.Copy( result.RawTileData , cgx.RawTileData , cgx.RawTileData.Length );

            // Metadata/prefix: keep whatever was classified, but we do NOT rely on them.
            cgx.Metadata = result.RawMetadata ?? Array.Empty<byte>();
            cgx.TilePrefixTable = result.RawPrefixTable ?? Array.Empty<byte>();

            cgx.Tiles = new CgxTile[cgx.TileCount];

            for ( int i = 0 ; i < cgx.TileCount ; i++ )
            {
                cgx.Tiles[i] = DecodeTile(
                    cgx.RawTileData ,
                    i * cgx.BytesPerTile ,
                    cgx.BitDepth );
            }

            // Prefix table may be incomplete; only apply where safe.
            if ( cgx.BitDepth == 4 &&
                cgx.TilePrefixTable != null &&
                cgx.TilePrefixTable.Length > 0 )
            {
                int usable = Math.Min(cgx.TileCount, cgx.TilePrefixTable.Length);
                for ( int t = 0 ; t < usable ; t++ )
                {
                    byte prefix = cgx.TilePrefixTable[t];
                    cgx.Tiles[t].PaletteRow = prefix & 0x0F;
                }

                if ( cgx.TilePrefixTable.Length < cgx.TileCount )
                {
                    AddWarning( result ,
                        "CGX prefix table is shorter than tile count. Some tiles have no prefix information." ,
                        "Prefix shorter than tiles." );
                }
            }

            // Layout: choose a reasonable width (like your viewer default)
            cgx.TilesX = 32;
            cgx.TilesY = (int)Math.Ceiling( cgx.TileCount / (double)cgx.TilesX );

            result.Parsed = cgx;
            return cgx;
        }

        // ============================================================
        // Bit‑depth inference helper (from tile data size)
        // ============================================================
        private static bool TryInferBitDepthFromTileData(int tileDataBytes , out int bitDepth , out int bytesPerTile)
        {
            // Try 8, 4, 2 in that order.
            if ( tileDataBytes % BytesPerTile8Bpp == 0 )
            {
                bitDepth = 8;
                bytesPerTile = BytesPerTile8Bpp;
                return true;
            }

            if ( tileDataBytes % BytesPerTile4Bpp == 0 )
            {
                bitDepth = 4;
                bytesPerTile = BytesPerTile4Bpp;
                return true;
            }

            if ( tileDataBytes % BytesPerTile2Bpp == 0 )
            {
                bitDepth = 2;
                bytesPerTile = BytesPerTile2Bpp;
                return true;
            }

            bitDepth = 0;
            bytesPerTile = 0;
            return false;
        }

        // ============================================================
        // Decode a single tile (unchanged logic, but no throws here)
        // ============================================================
        private static CgxTile DecodeTile(byte[] data , int offset , int bpp)
        {
            var tile = new CgxTile();

            switch ( bpp )
            {
                case 2:
                    for ( int y = 0 ; y < 8 ; y++ )
                    {
                        int rowOffset = offset + (y * 2);
                        if ( rowOffset + 1 >= data.Length )
                            break;

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
                    for ( int y = 0 ; y < 8 ; y++ )
                    {
                        int rowOffset01 = offset + (y * 2);
                        int rowOffset23 = offset + 16 + (y * 2);

                        if ( rowOffset23 + 1 >= data.Length )
                            break;

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
                    for ( int y = 0 ; y < 8 ; y++ )
                    {
                        int rowOffset01 = offset + (y * 2);
                        int rowOffset23 = offset + 16 + (y * 2);
                        int rowOffset45 = offset + 32 + (y * 2);
                        int rowOffset67 = offset + 48 + (y * 2);

                        if ( rowOffset67 + 1 >= data.Length )
                            break;

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
                    // Unsupported bpp → leave tile as zeros.
                    break;
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
