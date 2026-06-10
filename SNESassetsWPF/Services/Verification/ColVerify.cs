using SNESassetsWPF.Formats;
using SNESassetsWPF.Models;
using System.Diagnostics;

namespace SNESassetsWPF.Services
{
    /// <summary>
    /// Verification helper for COL files.
    /// Dumps structural information and the first few colours.
    /// </summary>
    public static class ColVerify
    {
        public static void DumpSummary(ColFileReadResult readResult , ColFile col)
        {
            Debug.WriteLine( "COL SUMMARY" );
            Debug.WriteLine( "-----------" );

            if ( readResult == null )
            {
                Debug.WriteLine( "COL read result is null." );
                return;
            }

            Debug.WriteLine( $"Classification: {readResult.Format}" );

            if ( readResult.Warnings.Count > 0 )
            {
                Debug.WriteLine( "Warnings:" );
                foreach ( var w in readResult.Warnings )
                    Debug.WriteLine( $"  • {w}" );
            }

            if ( col == null )
            {
                Debug.WriteLine( "COL is null — nothing to verify." );
                return;
            }

            Debug.WriteLine( $"Raw file size: {readResult.RawFile.Length} bytes" );
            Debug.WriteLine( $"Color data:    {readResult.RawColorData.Length} bytes" );
            Debug.WriteLine( $"Metadata:      {readResult.RawMetadata.Length} bytes" );

            DumpFirstColours( col );
        }

        private static void DumpFirstColours(ColFile col)
        {
            Debug.WriteLine( "First few colours:" );

            int count = 0;

            for ( int row = 0 ; row < 16 && count < 10 ; row++ )
            {
                for ( int colIndex = 0 ; colIndex < 16 && count < 10 ; colIndex++ )
                {
                    var snes = col.RawColors[row, colIndex];
                    var rgb = col.RgbColors[row, colIndex];

                    Debug.WriteLine(
                        $"  [{row},{colIndex}] SNES=0x{snes.Value:X4} " +
                        $"RGB=({rgb.R},{rgb.G},{rgb.B})"
                    );

                    count++;
                }
            }
        }
    }
}
