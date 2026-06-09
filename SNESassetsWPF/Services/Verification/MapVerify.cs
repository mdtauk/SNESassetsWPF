using System.Diagnostics;
using SNESassetsWPF.Models;

namespace SNESassetsWPF.Services
{
    /// <summary>
    /// Verification helper for MAP files.
    /// Dumps structural information and the first few cells.
    /// </summary>
    public static class MapVerify
    {
        public static void DumpSummary(MapFile map)
        {
            Debug.WriteLine( "MAP SUMMARY" );
            Debug.WriteLine( "-----------" );

            if ( map == null )
            {
                Debug.WriteLine( "MAP is null — nothing to verify." );
                return;
            }

            Debug.WriteLine( $"Raw file size:     {map.Header.Length + map.Cells.Length * 2} bytes" );
            Debug.WriteLine( $"Header size:       {map.Header.Length} bytes" );
            Debug.WriteLine( $"Cell count:        {map.Cells.Length}" );
            Debug.WriteLine( $"Dimensions:        {map.Width} × {map.Height}" );
            Debug.WriteLine( $"Meta‑tile width:   {map.MetaWidth} tiles" );
            Debug.WriteLine( $"Meta‑tile height:  {map.MetaHeight} tiles" );

            DumpPnlIndexStats( map );
            DumpFirstCells( map );
        }

        private static void DumpPnlIndexStats(MapFile map)
        {
            int valid = 0;
            int invalid = 0;

            for ( int i = 0 ; i < map.Cells.Length ; i++ )
            {
                int idx = map.Cells[i].PnlIndex;

                if ( idx >= 0 && idx < 16384 )
                    valid++;
                else
                    invalid++;
            }

            Debug.WriteLine( $"Valid PNL refs:    {valid}" );
            Debug.WriteLine( $"Invalid refs:      {invalid}" );
        }

        private static void DumpFirstCells(MapFile map)
        {
            Debug.WriteLine( "First few MAP cells:" );

            int count = 0;

            for ( int y = 0 ; y < map.Height && count < 10 ; y++ )
            {
                for ( int x = 0 ; x < map.Width && count < 10 ; x++ )
                {
                    var cell = map.GetCell(x, y);

                    Debug.WriteLine(
                        $"  [{x},{y}] raw=0x{cell.RawValue:X4} pnl={cell.PnlIndex}"
                    );

                    count++;
                }
            }
        }
    }
}
