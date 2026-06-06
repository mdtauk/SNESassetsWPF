using SNESassetsWPF.Formats;
using SNESassetsWPF.Models;
using System.Diagnostics;

namespace SNESassetsWPF.Services
{
    public static class PnlVerify
    {
        public static void DumpSummary(PnlFile pnl)
        {
            Debug.WriteLine( "PNL SUMMARY" );
            Debug.WriteLine( "-----------" );

            if ( pnl == null )
            {
                Debug.WriteLine( "PNL is null — nothing to verify." );
                return;
            }

            Debug.WriteLine( $"Total tiles: {pnl.Tiles.Length} (expected 16384)" );
            Debug.WriteLine( $"Panel layout: 32 × {pnl.Tiles.Length / 32}" );
            Debug.WriteLine( $"Palette block (ColHalf): {pnl.ColHalf}" );
            Debug.WriteLine( $"Palette cell  (ColCell): {pnl.ColCell}" );
            Debug.WriteLine( "" );

            int visible = 0;
            int hidden = 0;

            foreach ( var t in pnl.Tiles )
            {
                if ( t.IsVisible ) visible++;
                else hidden++;
            }

            Debug.WriteLine( $"Visible tiles:   {visible}" );
            Debug.WriteLine( $"Hidden tiles:    {hidden}" );
            Debug.WriteLine( "" );

            DumpFirstTiles( pnl );
        }

        private static void DumpFirstTiles(PnlFile pnl , int count = 16)
        {
            Debug.WriteLine( "First PNL tiles:" );
            Debug.WriteLine( "----------------" );

            int max = pnl.Tiles.Length < count ? pnl.Tiles.Length : count;

            for ( int i = 0 ; i < max ; i++ )
            {
                var t = pnl.Tiles[i];

                int x = i % 32;
                int y = i / 32;

                Debug.WriteLine(
                    $"Tile[{i}] at ({x},{y})  " +
                    $"vis={( t.IsVisible ? "Y" : "N" )}  " +
                    $"idx={t.TileIndex}  " +
                    $"pal={t.PaletteRow}  " +
                    $"H={t.HFlip}  V={t.VFlip}  " +
                    $"raw=0x{t.RawAttributeWord:X4}"
                );
            }

            Debug.WriteLine( "--------------------------------------------" );
        }
    }
}
