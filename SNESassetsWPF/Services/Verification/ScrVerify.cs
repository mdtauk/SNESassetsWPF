using System.Diagnostics;
using SNESassetsWPF.Models;

namespace SNESassetsWPF.Services
{
    /// <summary>
    /// Verification helper for SCR files.
    /// Dumps structural information and the first few entries.
    /// </summary>
    public static class ScrVerify
    {
        public static void DumpSummary(ScrFile scr)
        {
            Debug.WriteLine( "SCR SUMMARY" );
            Debug.WriteLine( "-----------" );

            if ( scr == null )
            {
                Debug.WriteLine( "SCR is null — nothing to verify." );
                return;
            }

            Debug.WriteLine( $"Raw file size: {scr.RawFile.Length} bytes" );
            Debug.WriteLine( $"Block count:   {scr.BlockCount}" );
            Debug.WriteLine( $"Blocks array:  {scr.Blocks.Length}" );

            for ( int b = 0 ; b < scr.Blocks.Length ; b++ )
            {
                var block = scr.Blocks[b];
                int entryCount = block?.Entries?.Length ?? 0;

                Debug.WriteLine( $"  Block {b}: entries={entryCount}" );

                if ( block != null && block.Entries.Length == 32 * 32 )
                {
                    DumpFirstEntriesForBlock( b , block );
                }
                else
                {
                    Debug.WriteLine( $"    Block {b} has unexpected entry count." );
                }
            }
        }

        private static void DumpFirstEntriesForBlock(int blockIndex , ScrBlock block)
        {
            Debug.WriteLine( $"  First few entries for block {blockIndex}:" );

            int count = 0;

            for ( int y = 0 ; y < 32 && count < 10 ; y++ )
            {
                for ( int x = 0 ; x < 32 && count < 10 ; x++ )
                {
                    var e = block.GetEntry(x, y);

                    Debug.WriteLine(
                        $"    [{x},{y}] raw=0x{e.RawValue:X4} " +
                        $"idx={e.TileIndex} pal={e.PaletteRow} " +
                        $"P={e.Priority} H={e.HFlip} V={e.VFlip}"
                    );

                    count++;
                }
            }
        }
    }
}
