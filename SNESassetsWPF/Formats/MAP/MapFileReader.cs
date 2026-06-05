using System;
using System.IO;
using SNESassetsWPF.Models;

namespace SNESassetsWPF.Formats
{
    /// <summary>
    /// Loads a MAP file from disk.
    /// Parsing is handled by the MapParser.
    /// </summary>
    public static class MapFileReader
    {
        public static MapFileReadResult Load(string path)
        {
            try
            {
                if ( !File.Exists( path ) )
                    return MapFileReadResult.Fail( "MAP file not found." );

                byte[] raw = File.ReadAllBytes(path);

                if ( raw.Length < 0x100 )
                    return MapFileReadResult.Fail( "MAP file too small to contain header." );

                // Return raw bytes only — parsing happens later
                return MapFileReadResult.Ok( null , raw );
            }
            catch ( Exception ex )
            {
                return MapFileReadResult.Fail( "MAP read error: " + ex.Message );
            }
        }
    }
}
