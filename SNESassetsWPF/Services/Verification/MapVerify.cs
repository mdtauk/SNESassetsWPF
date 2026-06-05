using System.Diagnostics;
using SNESassetsWPF.Formats;

namespace SNESassetsWPF.Services
{
    public static class MapVerify
    {
        public static void DumpSummary(MapFile map , PnlFile pnl)
        {
            Debug.WriteLine( "MAP SUMMARY" );
            Debug.WriteLine( "-----------" );

            if ( map == null )
            {
                Debug.WriteLine( "MapFile is NULL — cannot verify." );
                return;
            }

            Debug.WriteLine( $"Width:  {map.Width}" );
            Debug.WriteLine( $"Height: {map.Height}" );
            Debug.WriteLine( $"Total cells: {map.Width * map.Height}" );

            //
            // If PNL is missing, skip validation but still show MAP cells
            //
            if ( pnl == null )
            {
                Debug.WriteLine( "PNL not loaded — skipping PNL reference validation." );
                DumpFirstCells( map );
                return;
            }

            //
            // If PNL tiles array is missing, also skip validation
            //
            if ( pnl.PnlTiles == null )
            {
                Debug.WriteLine( "PNL loaded but PnlTiles[] is NULL — skipping validation." );
                DumpFirstCells( map );
                return;
            }

            int valid = 0;
            int empty = 0;
            int outOfRange = 0;

            for ( int y = 0 ; y < map.Height ; y++ )
            {
                for ( int x = 0 ; x < map.Width ; x++ )
                {
                    var cell = map.Cells?[x, y];
                    if ( cell == null )
                        continue;

                    // Compute PNL tile index from PnlX/PnlY
                    int index = cell.PnlY * PnlFile.PanelWidth + cell.PnlX;

                    if ( index < 0 || index >= pnl.PnlTiles.Length )
                    {
                        outOfRange++;
                        continue;
                    }

                    var pnlTile = pnl.PnlTiles[index];
                    if ( pnlTile == null )
                    {
                        empty++;
                        continue;
                    }

                    if ( !pnlTile.IsPresent )
                        empty++;
                    else
                        valid++;
                }
            }

            Debug.WriteLine( $"Valid PNL references: {valid}" );
            Debug.WriteLine( $"Empty PNL references: {empty}" );
            Debug.WriteLine( $"Out-of-range references: {outOfRange}" );

            DumpFirstCells( map );
        }

        private static void DumpFirstCells(MapFile map)
        {
            Debug.WriteLine( "First few MAP cells:" );

            if ( map.Cells == null )
            {
                Debug.WriteLine( "MAP.Cells is NULL — cannot dump." );
                return;
            }

            for ( int i = 0 ; i < 5 ; i++ )
            {
                int x = i % map.Width;
                int y = i / map.Width;
                if ( y >= map.Height ) break;

                var c = map.Cells[x, y];
                if ( c == null )
                {
                    Debug.WriteLine( $"  [{x},{y}] NULL cell" );
                    continue;
                }

                Debug.WriteLine(
                    $"  [{x},{y}] raw=0x{c.RawValue:X4} pnl=({c.PnlX},{c.PnlY}) usePanelAttr={c.UsePanelAttributes}"
                );
            }
        }
    }
}
