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
    /// <summary>
    /// Loads a COL file from disk.
    /// Parsing is handled by the MapParser.
    /// </summary>
    public class ColFileReader
    {
        public ColFileReadResult Read(string path)
        {
            try
            {
                if ( !File.Exists( path ) )
                    return ColFileReadResult.Fail( "COL file not found." );

                byte[] raw = File.ReadAllBytes(path);
                return ColFileReadResult.Ok( raw );
            }
            catch ( Exception ex )
            {
                return ColFileReadResult.Fail( ex.Message );
            }
        }

    }
}

