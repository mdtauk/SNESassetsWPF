using System.Diagnostics;
using SNESassetsWPF.Formats;
using SNESassetsWPF.Models;

namespace SNESassetsWPF.Services
{
    public static class CgxVerify
    {
        public static void DumpSummary(CgxFile cgx)
        {
            Debug.WriteLine( "CGX SUMMARY" );
            Debug.WriteLine( "-----------" );

            if ( cgx == null )
            {
                Debug.WriteLine( "CGX is null — nothing to verify." );
                return;
            }

            // --- Basic file info ---
            Debug.WriteLine( $"Raw file size:     {cgx.RawFile?.Length ?? 0} bytes" );
            Debug.WriteLine( $"Bit depth:         {cgx.BitDepth} bpp" );
            Debug.WriteLine( $"Bytes per tile:    {cgx.BytesPerTile}" );
            Debug.WriteLine( $"Tile count:        {cgx.TileCount}" );
            Debug.WriteLine( $"Tiles decoded:     {cgx.Tiles?.Length ?? 0}" );
            Debug.WriteLine( $"Tile sheet layout: {cgx.TilesX} × {cgx.TilesY}" );
            Debug.WriteLine( $"Prefix table:      {cgx.TilePrefixTable?.Length ?? 0} bytes" );
            Debug.WriteLine( $"Metadata block:    {cgx.Metadata?.Length ?? 0} bytes" );
            Debug.WriteLine( $"Raw tile data:     {cgx.RawTileData?.Length ?? 0} bytes" );

            if ( cgx.Tiles == null || cgx.Tiles.Length == 0 )
            {
                Debug.WriteLine( "No tiles decoded — aborting further analysis." );
                return;
            }

            // --- First 10 tiles ---
            DumpFirstTiles( cgx );

            // --- Tile 0 pixel indices ---
            Debug.Write( "Tile 0 indices: " );
            for ( int y = 0 ; y < 8 ; y++ )
            {
                for ( int x = 0 ; x < 8 ; x++ )
                    Debug.Write( $"{cgx.Tiles[0].Pixels[y , x]} " );
                Debug.Write( " | " );
            }
            Debug.WriteLine( "" );

            // --- BG/OBJ toggle (print once) ---
            if ( cgx.Metadata != null && cgx.Metadata.Length >= 0x23 )
            {
                byte toggle = cgx.Metadata[0x22];
                bool useBg = (toggle & 1) != 0;

                Debug.WriteLine( "" );
                Debug.WriteLine( "Palette mode:" );
                Debug.WriteLine( $"  metadata[0x22] = 0x{toggle:X2}" );
                Debug.WriteLine( $"  Using {( useBg ? "BG rows (8–15)" : "OBJ rows (0–7)" )}" );
            }
            else
            {
                Debug.WriteLine( "Metadata too small to contain BG/OBJ toggle." );
            }

            // --- Palette row usage per 128‑tile block ---
            Debug.WriteLine( "" );
            Debug.WriteLine( "Palette row usage per 128‑tile block:" );

            int tilesPerBlock = 128;
            int blocks = cgx.TileCount / tilesPerBlock;

            for ( int b = 0 ; b < blocks ; b++ )
            {
                int start = b * tilesPerBlock;
                int end = start + tilesPerBlock;

                bool[] used = new bool[16];

                for ( int t = start ; t < end && t < cgx.Tiles.Length ; t++ )
                {
                    int row = cgx.Tiles[t].PaletteRow;
                    if ( row >= 0 && row < 16 )
                        used[row] = true;
                }

                string rows = string.Join(", ",
                    Enumerable.Range(0, 16).Where(r => used[r]));

                if ( string.IsNullOrWhiteSpace( rows ) )
                    rows = "(none)";

                Debug.WriteLine( $"  Tiles {start:D4}–{end - 1:D4}: rows {rows}" );
            }
        }

        private static void DumpFirstTiles(CgxFile cgx)
        {
            Debug.WriteLine( "" );
            Debug.WriteLine( "First 10 tiles:" );

            for ( int i = 0 ; i < cgx.Tiles.Length && i < 10 ; i++ )
            {
                var tile = cgx.Tiles[i];
                if ( tile == null )
                {
                    Debug.WriteLine( $"  Tile {i}: NULL" );
                    continue;
                }

                int nonZero = CountNonZero(tile);
                Debug.WriteLine( $"  Tile {i}: nonzero={nonZero}" );
            }
        }

        private static int CountNonZero(CgxTile tile)
        {
            int nz = 0;
            for ( int y = 0 ; y < 8 ; y++ )
                for ( int x = 0 ; x < 8 ; x++ )
                    if ( tile.Pixels[y , x] != 0 )
                        nz++;
            return nz;
        }
    }
}
