using System;
using System.IO;

namespace SNESassetsWPF.Formats
{
    public class CgxFileReader
    {
        public CgxFileReadResult Read(string path)
        {
            var result = new CgxFileReadResult();

            try
            {
                byte[] raw = File.ReadAllBytes(path);
                int length = raw.Length;

                result.RawFile = raw;

                // Determine bit depth from known CGX sizes
                result.BitDepth = length switch
                {
                    0x4500 => 2,    // 16 KB tiles + 0x100 metadata + 0x400 prefix
                    0x8500 => 4,    // 32 KB tiles + 0x100 metadata + 0x400 prefix
                    0x10100 => 8,    // 64 KB tiles + 0x100 metadata (no prefix)
                    _ => 4     // fallback
                };

                // Compute tile data size
                int tileBytes = result.BitDepth switch
                {
                    2 => 0x4000,   // 16 KB
                    4 => 0x8000,   // 32 KB
                    8 => 0x10000,  // 64 KB
                    _ => 0x8000
                };

                // Extract raw tile data (always from byte 0)
                result.TileData = raw.AsSpan( 0 , tileBytes ).ToArray();

                if ( result.BitDepth == 2 || result.BitDepth == 4 )
                {
                    // 2/4bpp S‑CG‑CAD CGX layout:
                    // [0x0000..tileBytes-1]   tile data
                    // [tileBytes..tileBytes+0xFF]   metadata (ASCII "NAK1989 S‑CG‑CAD Ver...")
                    // [tileBytes+0x100..tileBytes+0x4FF] prefix table (0x400 bytes)

                    // Metadata at tileBytes
                    result.Metadata = raw.AsSpan( tileBytes , 0x100 ).ToArray();

                    // Prefix table immediately after metadata
                    int prefixOffset = tileBytes + 0x100;
                    int prefixLength = length - prefixOffset;

                    if ( prefixLength == 0x400 )
                        result.PrefixTable = raw.AsSpan( prefixOffset , 0x400 ).ToArray();
                    else
                        result.PrefixTable = Array.Empty<byte>();
                }
                else
                {
                    // 8bpp layout:
                    // [0x0000..0xFFFF] tile data
                    // [0x10000..0x100FF] metadata (last 0x100 bytes), no prefix table
                    result.Metadata = raw.AsSpan( length - 0x100 , 0x100 ).ToArray();
                    result.PrefixTable = Array.Empty<byte>();
                }

                // Compute bytes per tile
                result.BytesPerTile = result.BitDepth switch
                {
                    2 => 16,
                    4 => 32,
                    8 => 64,
                    _ => 32
                };

                // Compute tile count
                result.TileCount = tileBytes / result.BytesPerTile;

                result.IsValid = true;
            }
            catch ( Exception ex )
            {
                result.IsValid = false;
                result.ErrorMessage = ex.Message;
            }

            return result;
        }
    }
}
