using System;
using System.IO;
using SNESassetsWPF.Services;




namespace SNESassetsWPF.Formats
{
    /// <summary>
    /// Loads a SCR file from disk.
    /// Parsing is handled by the MapParser.
    /// </summary>
    public static class ScrFileReader
    {
        public static ScrFileReadResult Load(string path)
        {
            try
            {
                if ( !File.Exists( path ) )
                    return ScrFileReadResult.Fail( "SCR file not found." );

                byte[] raw = File.ReadAllBytes(path);
                return ScrFileReadResult.Ok( raw );
            }
            catch ( Exception ex )
            {
                return ScrFileReadResult.Fail( ex.Message );
            }
        }
    }

}