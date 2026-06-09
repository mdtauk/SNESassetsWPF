using System;
using System.IO;




namespace SNESassetsWPF.Formats
{
    /// <summary>
    /// Loads a MAP file from disk.
    /// Parsing is handled by the MapParser.
    /// </summary>
    public class PnlFileReader
    {
        public PnlFileReadResult Read(string path)
        {
            try
            {
                if ( !File.Exists( path ) )
                    return PnlFileReadResult.Fail( "PNL file not found." );

                byte[] raw = File.ReadAllBytes(path);

                if ( raw.Length < 0x100 + 0x8000 + 0x8000 )
                    return PnlFileReadResult.Fail( "Invalid PNL file: too small." );

                return PnlFileReadResult.Ok( raw );
            }
            catch ( Exception ex )
            {
                return PnlFileReadResult.Fail( ex.Message );
            }
        }
    }
}
