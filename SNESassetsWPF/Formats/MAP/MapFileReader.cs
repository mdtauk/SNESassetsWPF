using System;
using System.Diagnostics;
using System.IO;

namespace SNESassetsWPF.Formats
{
    /// <summary>
    /// Loads a MAP file from disk.
    /// Performs I/O only — no parsing.
    /// Parsing is handled by MapParser.
    /// </summary>
    public static class MapFileReader
    {
        public static MapFileReadResult Load(string path)
        {
            try
            {
                if ( !File.Exists( path ) )
                {
                    Debug.WriteLine( $"[MAP LOADER] File not found: {path}" );
                    return MapFileReadResult.Fail( "MAP file not found." );
                }

                byte[] raw = File.ReadAllBytes(path);

                Debug.WriteLine( $"[MAP LOADER] Loaded {raw.Length} bytes from {path}" );

                // MAP must contain at least the 0x100‑byte header
                if ( raw.Length < 0x100 )
                {
                    Debug.WriteLine( $"[MAP LOADER] File too small ({raw.Length} bytes). Needs >= 256." );
                    return MapFileReadResult.Fail( "MAP file too small to contain header." );
                }

                // Return raw bytes only — parsing happens later
                return MapFileReadResult.Ok( raw );
            }
            catch ( Exception ex )
            {
                Debug.WriteLine( $"[MAP LOADER] Exception while reading file: {ex}" );
                return MapFileReadResult.Fail( "MAP read error: " + ex.Message );
            }
        }
    }
}
