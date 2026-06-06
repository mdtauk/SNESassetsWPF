using SNESassetsWPF.Formats;

public class PnlFileParser
{
    public PnlFile Parse(byte[] data)
    {
        if ( data == null || data.Length < 0x10100 )
            throw new ArgumentException( "Invalid PNL data: wrong size." );

        var pnl = new PnlFile();

        // ---------------------------------------------------------
        // 1. Read header (first 0x100 bytes)
        // ---------------------------------------------------------
        byte[] header = new byte[0x100];
        Buffer.BlockCopy( data , 0 , header , 0 , 0x100 );

        // Palette block selectors
        pnl.ColHalf = header[0x65];
        pnl.ColCell = header[0x66];

        // Group size (pattern size)
        pnl.GroupWidth = 1 << ( header[0x69] & 0x1F );
        pnl.GroupHeight = 1 << ( header[0x6A] & 0x1F );

        // ---------------------------------------------------------
        // 2. Allocate tiles
        // ---------------------------------------------------------
        pnl.Tiles = new PnlTile[0x4000]; // 16384 tiles

        int attrBase  = 0x100;
        int clearBase = 0x8100;

        // ---------------------------------------------------------
        // 3. Parse all 16384 tiles
        // ---------------------------------------------------------
        for ( int i = 0 ; i < 0x4000 ; i++ )
        {
            int pos = attrBase + (i * 2);
            ushort rawAttr = (ushort)((data[pos] << 8) | data[pos + 1]);

            bool visible = data[clearBase + (i * 2)] != 0;

            pnl.Tiles[i] = new PnlTile
            {
                RawAttributeWord = rawAttr ,
                IsVisible = visible
            };
        }

        return pnl;
    }
}
