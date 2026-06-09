using System.Diagnostics;
using SNESassetsWPF.Models;

namespace SNESassetsWPF.Services
{
    public static class MapVerify
    {
        public static void DumpSummary(MapFile map)
        {
            Debug.WriteLine( "" );
            Debug.WriteLine( "MAP SUMMARY" );
            Debug.WriteLine( "-----------" );

            if ( map == null )
            {
                Debug.WriteLine( "MAP is null — nothing to verify." );
                return;
            }

            Debug.WriteLine( $"Raw file size:       {map.Header.Length + map.Cells.Length * 2} bytes" );
            Debug.WriteLine( $"Header size:         {map.Header.Length} bytes" );
            Debug.WriteLine( $"Cell count:          {map.Cells.Length}" );
            Debug.WriteLine( $"Dimensions:          {map.Width} × {map.Height}" );
            Debug.WriteLine( $"Meta‑tile width:     {map.MetaWidth} tiles" );
            Debug.WriteLine( $"Meta‑tile height:    {map.MetaHeight} tiles" );

            DumpPnlIndexStats( map );
            DumpStructuralWarnings( map );
            DumpFirstCells( map );
        }

        // ─────────────────────────────────────────────────────────────
        // 1. PNL index validity
        // ─────────────────────────────────────────────────────────────
        private static void DumpPnlIndexStats(MapFile map)
        {
            int valid = 0;
            int invalid = 0;
            int max = -1;

            for ( int i = 0 ; i < map.Cells.Length ; i++ )
            {
                int idx = map.Cells[i].PnlIndex;

                if ( idx >= 0 && idx < 16384 )
                    valid++;
                else
                    invalid++;

                if ( idx > max )
                    max = idx;
            }

            Debug.WriteLine( $"Valid PNL refs:      {valid}" );
            Debug.WriteLine( $"Invalid refs:        {invalid}" );
            Debug.WriteLine( $"Max PNL index seen:  {max}" );
        }

        // ─────────────────────────────────────────────────────────────
        // 2. Structural warnings (the important part)
        // ─────────────────────────────────────────────────────────────
        private static void DumpStructuralWarnings(MapFile map)
        {
            Debug.WriteLine( "" );
            Debug.WriteLine( "STRUCTURAL CHECKS" );
            Debug.WriteLine( "-----------------" );

            // Width sanity
            if ( map.Width != 32 && map.Width != 64 )
                Debug.WriteLine( $"WARNING: Width {map.Width} is unusual (expected 32 or 64)." );

            // Height sanity
            if ( map.Height != 32 && map.Height != 64 )
                Debug.WriteLine( $"WARNING: Height {map.Height} is unusual (expected 32 or 64)." );


            // Height sanity
            if ( map.Height * map.Width != map.Cells.Length )
                Debug.WriteLine( "ERROR: Width × Height does not match cell count!" );

            // Meta‑tile block boundary checks
            int mw = map.MetaWidth;
            int mh = map.MetaHeight;

            if ( mw > 1 || mh > 1 )
            {
                Debug.WriteLine( $"Meta‑tile mode active: {mw}×{mh}" );

                for ( int y = 0 ; y < map.Height ; y++ )
                {
                    for ( int x = 0 ; x < map.Width ; x++ )
                    {
                        int cellIndex = y * map.Width + x;
                        int pnl = map.Cells[cellIndex].PnlIndex;

                        // Check PNL block boundaries
                        int pnlX = pnl % 32;
                        int pnlY = pnl / 32;

                        if ( pnlX + mw > 32 )
                        {
                            Debug.WriteLine(
                                $"WARNING: Cell [{x},{y}] pnl={pnl} block overruns PNL row boundary (pnlX={pnlX}, mw={mw})."
                            );
                        }

                        if ( pnlY + mh > 512 )
                        {
                            Debug.WriteLine(
                                $"WARNING: Cell [{x},{y}] pnl={pnl} block overruns PNL height (pnlY={pnlY}, mh={mh})."
                            );
                        }
                    }
                }
            }
            else
            {
                Debug.WriteLine( "Meta‑tile mode: OFF (1×1 tiles)" );
            }
        }

        // ─────────────────────────────────────────────────────────────
        // 3. Dump first few cells
        // ─────────────────────────────────────────────────────────────
        private static void DumpFirstCells(MapFile map)
        {
            Debug.WriteLine( "" );
            Debug.WriteLine( "FIRST FEW MAP CELLS" );
            Debug.WriteLine( "-------------------" );

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
