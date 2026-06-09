using System;
using SNESassetsWPF.Models;

public static class ScrFileParser
{
    private const int TilesPerBlock  = 32 * 32;           // 1024 tiles
    private const int BlockSizeBytes = TilesPerBlock * 2; // 2048 bytes
    private const int TilemapSize    = 0x2000;            // 8192 bytes (4 blocks)

    public static ScrFile Parse(byte[] data)
    {
        if ( data == null || data.Length < TilemapSize )
            throw new ArgumentException( "SCR data is too small." , nameof( data ) );

        // This matches S‑CG‑CAD SCR layout:
        // 0x0000–0x1FFF = 4 × 0x800 tilemap blocks
        // 0x2000–0x20FF = footer/extra
        // 0x2100–0x22FF = clear mask (visibility), 4 × 0x80 bytes
        // total size 0x2300 for “Normal” format

        int blockCount = TilemapSize / BlockSizeBytes; // 4

        var scr = new ScrFile
        {
            Blocks     = new ScrBlock[blockCount],
            BlockCount = blockCount
        };

        // Allocate blocks
        for ( int b = 0 ; b < blockCount ; b++ )
        {
            scr.Blocks[b] = new ScrBlock
            {
                Entries = new ScrEntry[TilesPerBlock] ,
                VisibilityMask = new bool[TilesPerBlock]
            };
        }

        // ---------------------------------------------
        // Extract visibility mask from SCR footer
        // (equivalent to CAD.SCR clear[][] logic)
        // ---------------------------------------------
        bool[][] scrClearMask = new bool[4][];

        for ( int s = 0 ; s < 4 ; s++ )
        {
            // 0x80 bytes per screen, interleaved in a weird pattern
            byte[] tmp = new byte[0x80];

            for ( int j = 0 ; j < 0x80 ; j++ )
            {
                int srcOffset =
                    0x2100
                    + ((s & 2) * 0x80)
                    + ((s & 1) * 4)
                    + (j % 4)
                    + ((j / 4) * 8);

                tmp[j] = data[srcOffset];
            }

            // Convert 0x80 bytes → 1024 bits (32×32 tiles), reversed bit order
            scrClearMask[s] = ToBitStreamReverse( tmp , 1024 );
        }

        // ---------------------------------------------
        // Decode tilemap entries + apply visibility mask
        // ---------------------------------------------
        for ( int b = 0 ; b < blockCount ; b++ )
        {
            int      blockOffset = b * BlockSizeBytes;
            ScrBlock block       = scr.Blocks[b];

            for ( int i = 0 ; i < TilesPerBlock ; i++ )
            {
                int entryOffset = blockOffset + (i * 2);

                ushort raw = (ushort)(
                    data[entryOffset] |
                    (data[entryOffset + 1] << 8)
                );

                bool isVisible = scrClearMask[b][i];

                block.VisibilityMask[i] = isVisible;

                block.Entries[i] = new ScrEntry
                {
                    RawValue = raw ,
                    IsVisible = isVisible
                };
            }
        }

        return scr;
    }

    /// <summary>
    /// Convert bytes to a bitstream, using the same “reverse” bit order
    /// that H‑CG‑CAD uses for SCR clear data.
    /// </summary>
    private static bool[] ToBitStreamReverse(byte[] bytes , int bitCount)
    {
        bool[] bits = new bool[bitCount];

        for ( int i = 0 ; i < bitCount ; i++ )
        {
            // Same as hcgcadviewer.Utility.ToBitStreamReverse:
            // bit i comes from (bytes[i / 8] << (i % 8)) & 0x80
            byte b = bytes[i / 8];
            bits[i] = ( ( b << ( i % 8 ) ) & 0x80 ) != 0;
        }

        return bits;
    }
}
