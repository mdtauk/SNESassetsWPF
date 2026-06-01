using SNESassetsWPF.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Media;

namespace SNESassetsWPF.Formats
{
    public class ColFileReader
    {
        public ColFileReadResult Read(string path)
        {
            var result = new ColFileReadResult();

            try
            {
                var bytes = File.ReadAllBytes(path);

                if ( bytes.Length < 0x200 )
                {
                    result.IsValid = false;
                    result.ErrorMessage = "COL file too small.";
                    return result;
                }

                // First 0x200 bytes = 256 colours (16 palettes × 16 colours)
                result.RawColorData = bytes.AsSpan( 0 , 0x200 ).ToArray();

                // Remaining bytes = metadata (32 rows)
                if ( bytes.Length > 0x200 )
                    result.RawMetadata = bytes.AsSpan( 0x200 ).ToArray();

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
