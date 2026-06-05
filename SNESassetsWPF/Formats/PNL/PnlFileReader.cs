using System;
using System.IO;

namespace SNESassetsWPF.Formats
{
    public class PnlFileReader
    {
        /// <summary>
        /// Reads a PNL file from disk and returns the raw bytes.
        /// Does not parse or interpret the data.
        /// </summary>
        public PnlFileReadResult Read(string path)
        {
            var result = new PnlFileReadResult();

            try
            {
                if ( !File.Exists( path ) )
                {
                    result.Success = false;
                    result.ErrorMessage = "File not found.";
                    return result;
                }

                // Read all bytes
                byte[] data = File.ReadAllBytes(path);

                // Basic sanity check: PNL files are always >= 0x100 header + 0x8000 tile table + 0x8000 flag table
                if ( data.Length < 0x100 + 0x8000 + 0x8000 )
                {
                    result.Success = false;
                    result.ErrorMessage = "Invalid PNL file: too small.";
                    return result;
                }

                result.Success = true;
                result.Data = data;
                return result;
            }
            catch ( Exception ex )
            {
                result.Success = false;
                result.ErrorMessage = ex.Message;
                return result;
            }
        }
    }
}
