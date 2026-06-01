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

                // Determine bit depth from known CGX sizes
                result.BitDepth = length switch
                {
                    0x4500 => 2,  // 16 KB tiles + 1 KB prefix + 256 B metadata
                    0x8500 => 4,  // 32 KB tiles + 1 KB prefix + 256 B metadata
                    0x10100 => 8,  // 64 KB tiles + 256 B metadata
                    _ => 4   // fallback
                };

                int tileBytes = result.BitDepth switch
                {
                    2 => 0x4000,   // 16 KB
                    4 => 0x8000,   // 32 KB
                    8 => 0x10000,  // 64 KB
                    _ => 0x8000
                };

                // Extract raw tile data
                result.TileData = new byte[tileBytes];
                Buffer.BlockCopy( raw , 0x0000 , result.TileData , 0 , tileBytes );

                // Extract trailing metadata block (ASCII)
                // Always 0x100 bytes at the end
                result.Metadata = new byte[0x100];
                Buffer.BlockCopy( raw , length - 0x100 , result.Metadata , 0 , 0x100 );

                // Extract prefix table (if present)
                // Only 2bpp/4bpp CGX have this
                int prefixOffset = tileBytes + 0x100; // tiles + metadata
                int prefixLength = length - tileBytes - 0x100;

                if ( prefixLength == 0x400 )
                {
                    result.PrefixTable = new byte[0x400];
                    Buffer.BlockCopy( raw , tileBytes + 0x100 , result.PrefixTable , 0 , 0x400 );
                }
                else
                {
                    result.PrefixTable = Array.Empty<byte>();
                }

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
