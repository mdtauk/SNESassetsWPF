using System;
using System.IO;




namespace SNESassetsWPF.Formats
{
    /// <summary>
    /// Loads a CGX file from disk.
    /// Parsing is handled by the MapParser.
    /// </summary>
    public class CgxFileReader
    {
        public CgxFileReadResult Read(string path)
        {
            try
            {
                if ( !File.Exists( path ) )
                    return CgxFileReadResult.Fail( "CGX file not found." );

                byte[] raw = File.ReadAllBytes(path);
                return CgxFileReadResult.Ok( raw );
            }
            catch ( Exception ex )
            {
                return CgxFileReadResult.Fail( ex.Message );
            }
        }
    }

}
