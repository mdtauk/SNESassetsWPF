using System.Diagnostics;
using SNESassetsWPF.Formats;
using SNESassetsWPF.Models;

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

            Debug.WriteLine( $"Panel size: {PnlFile.PanelWidth} × {PnlFile.PanelHeight}" );
            Debug.WriteLine( $"Total tiles: {pnl.PnlTiles.Length}" );

            Debug.WriteLine( $"Meta tile size: {pnl.MetaWidth} × {pnl.MetaHeight}" );
            Debug.WriteLine( $"Mode 7 enabled: {pnl.IsMode7Enabled}" );

            int present = 0;
            int empty = 0;

            foreach ( var tile in pnl.PnlTiles )
            {
                if ( tile == null )
                    continue;

                if ( tile.IsPresent )
                    present++;
                else
                    empty++;
            }

            Debug.WriteLine( $"Present tiles: {present}" );
            Debug.WriteLine( $"Empty tiles:   {empty}" );

            DumpFirstTiles( pnl );
        }

        private static void DumpFirstTiles(PnlFile pnl)
        {
            Debug.WriteLine( "First few PNL tiles:" );

            for ( int i = 0 ; i < 10 ; i++ )
            {
                if ( i >= pnl.PnlTiles.Length )
                    break;

                var t = pnl.PnlTiles[i];
                if ( t == null )
                    continue;

                Debug.WriteLine(
                    $"  [{i}] rawAttr=0x{t.RawAttributeWord:X4} rawFlag=0x{t.RawFlagWord:X4} " +
                    $"CGX={t.TileId} pal={t.PaletteRow} H={t.HFlip} V={t.VFlip} P={t.Priority} present={t.IsPresent}"
                );
            }
        }
    }
}
