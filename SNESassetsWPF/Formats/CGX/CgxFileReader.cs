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
                    0x4500 => 2,   // 16 KB tiles + 1 KB prefix + 256 B metadata
                    0x8500 => 4,   // 32 KB tiles + 1 KB prefix + 256 B metadata
                    0x10100 => 8,   // 64 KB tiles + 256 B metadata
                    _ => 4    // fallback
                };

                // Compute tile data size
                int tileBytes = result.BitDepth switch
                {
                    2 => 0x4000,   // 16 KB
                    4 => 0x8000,   // 32 KB
                    8 => 0x10000,  // 64 KB
                    _ => 0x8000
                };

                // Extract raw tile data
                result.TileData = raw.AsSpan( 0 , tileBytes ).ToArray();

                // Metadata is ALWAYS last 0x100 bytes
                result.Metadata = raw.AsSpan( length - 0x100 , 0x100 ).ToArray();

                // Prefix table sits between tile data and metadata
                int prefixLength = length - tileBytes - 0x100;

                if ( prefixLength == 0x400 && ( result.BitDepth == 2 || result.BitDepth == 4 ) )
                {
                    result.PrefixTable = raw.AsSpan( tileBytes , 0x400 ).ToArray();
                }
                else
                {
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
