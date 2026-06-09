using System.Diagnostics;
using SNESassetsWPF.Formats;

public static class PnlVerify
{
    /// <summary>
    /// Dumps a detailed summary of the parsed PNL file to Debug Output.
    /// </summary>
    public static void DumpSummary(PnlFile pnl)
    {
        if ( pnl == null )
        {
            Debug.WriteLine( "PNL is null." );
            return;
        }

        Debug.WriteLine( "──────────────────────────────────────────────" );
        Debug.WriteLine( "  PNL SUMMARY" );
        Debug.WriteLine( "──────────────────────────────────────────────" );

        Debug.WriteLine( $"RawFile.Length     = {pnl.RawFile.Length}" );
        Debug.WriteLine( $"HeaderSize         = {PnlFile.HeaderSize} (0x{PnlFile.HeaderSize:X})" );
        Debug.WriteLine( $"EntryCount          = {PnlFile.EntryCount}" );
        Debug.WriteLine( $"Width × Height     = {PnlFile.Width} × {PnlFile.Height}" );
        Debug.WriteLine( "" );

        // Validate file size
        int expectedSize = PnlFile.HeaderSize + 0x8000 + 0x8000;

        Debug.WriteLine( $"Expected Size      = {expectedSize} bytes" );
        Debug.WriteLine( $"Actual Size        = {pnl.RawFile.Length} bytes" );
        Debug.WriteLine( $"Size OK            = {pnl.RawFile.Length == expectedSize}" );
        Debug.WriteLine( "" );

        // Show first few tiles
        Debug.WriteLine( "First 8 tiles:" );
        for ( int i = 0 ; i < 8 ; i++ )
        {
            var t = pnl.Entries[i];
            Debug.WriteLine(
                $"[{i:D4}] Attr=0x{t.RawAttributeWord:X4}  " +
                $"Flag=0x{t.RawFlagWord:X4}  " +
                $"Idx={t.TileIndex:D4}  Pal={t.PaletteRow}  " +
                $"H={t.HFlip} V={t.VFlip} P={t.Priority}  " +
                $"Present={t.IsPresent}"
            );
        }

        Debug.WriteLine( "" );

        // Show last few tiles
        Debug.WriteLine( "Last 8 tiles:" );
        for ( int i = PnlFile.EntryCount - 8 ; i < PnlFile.EntryCount ; i++ )
        {
            var t = pnl.Entries[i];
            Debug.WriteLine(
                $"[{i:D4}] Attr=0x{t.RawAttributeWord:X4}  " +
                $"Flag=0x{t.RawFlagWord:X4}  " +
                $"Idx={t.TileIndex:D4}  Pal={t.PaletteRow}  " +
                $"H={t.HFlip} V={t.VFlip} P={t.Priority}  " +
                $"Present={t.IsPresent}"
            );
        }

        Debug.WriteLine( "" );

        // Count present tiles
        int presentCount = 0;
        foreach ( var t in pnl.Entries )
            if ( t.IsPresent )
                presentCount++;

        Debug.WriteLine( $"Present tiles      = {presentCount} / {PnlFile.EntryCount}" );
        Debug.WriteLine( "──────────────────────────────────────────────" );
        Debug.WriteLine( "" );
    }
}
