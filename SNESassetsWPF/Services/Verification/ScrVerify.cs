using System.Diagnostics;
using SNESassetsWPF.Formats;
using SNESassetsWPF.Models;

namespace SNESassetsWPF.Services
{
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

            Debug.WriteLine( $"Dimensions: {scr.WidthTiles} × {scr.HeightTiles}" );
            Debug.WriteLine( $"Blocks:     {scr.BlockCount}" );
            Debug.WriteLine( $"Footer:     {scr.Footer?.Length ?? 0} bytes" );
            Debug.WriteLine( $"Raw bytes:  {scr.RawBytes.Length}" );

            // Visibility stats
            int totalTiles = scr.WidthTiles * scr.HeightTiles;
            int visible = 0;
            int hidden = 0;

            for ( int y = 0 ; y < scr.HeightTiles ; y++ )
            {
                for ( int x = 0 ; x < scr.WidthTiles ; x++ )
                {
                    if ( scr.Tiles[y , x].Visible )
                        visible++;
                    else
                        hidden++;
                }
            }

            Debug.WriteLine( $"Visible tiles: {visible}" );
            Debug.WriteLine( $"Hidden tiles:  {hidden}" );

            DumpFirstTiles( scr );
        }

        private static void DumpFirstTiles(ScrFile scr)
        {
            Debug.WriteLine( "First few SCR tiles:" );

            int count = 0;

            for ( int y = 0 ; y < scr.HeightTiles && count < 10 ; y++ )
            {
                for ( int x = 0 ; x < scr.WidthTiles && count < 10 ; x++ )
                {
                    var t = scr.Tiles[y, x];

                    Debug.WriteLine(
                        $"  [{x},{y}] raw=0x{t.Raw:X4} idx={t.TileIndex} pal={t.PaletteIndex} " +
                        $"H={t.HFlip} V={t.VFlip} P={t.Priority} vis={t.Visible}"
                    );

                    count++;
                }
            }
        }
    }
}
