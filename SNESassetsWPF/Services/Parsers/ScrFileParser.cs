using SNESassetsWPF.Formats;
using SNESassetsWPF.Models;
using System;
using static SNESassetsWPF.Formats.ColFileReadResult;
using static SNESassetsWPF.Formats.ScrFileReadResult;

public static class ScrFileParser
{
    private const int TilesPerBlock  = 32 * 32;             // 1024 tiles
    private const int BlockSizeBytes = TilesPerBlock * 2;   // 2048 bytes
    private const int TilemapSize    = 0x2000;            // 8192 bytes (4 blocks)

    private const int TileEntrySize  = 2;                   // Size in bytes

    private const int VisibilityMaskOffset = 0x2100;
    private const int VisibilityMaskSize   = 0x200;       // 4 × 0x80 bytes
    private const int FooterOffset         = 0x2000;
    private const int FooterSize           = 0x100;       // 0x2000–0x20FF





    public static void ClassifyScrStructure(byte[] data , ScrFileReadResult result)
    {
        int size = data.Length;

        // ---------------------------------------------
        // 1. Too small to be anything
        // ---------------------------------------------
        if ( size < 2 )
        {
            result.Format = ScrFileReadResult.ScrFormatType.Unreadable;

            result.Warnings.Add( new ScrWarning
            {
                Long = "File is too small to contain any Cgx tile reference data." ,
                Short = "Too small to contain tiles."
            } );

            return;
        }

        // ---------------------------------------------
        // 2. Strict size ranges (Normal, NoClear, F-Format)
        // ---------------------------------------------
        bool isStrictRange =
        (size >= 0x2000 && size <= 0x2300) ||   // Normal
        (size >= 0x2000 && size <= 0x2100) ||   // NoClearData
        (size >= 0x2000 && size <= 0x4100);     // F-Format

        if ( isStrictRange )
        {
            // Check tilemap completeness (0x0000–0x1FFF must exist)
            if ( size < 0x2000 )
            {
                // Should never happen because of the range check,
                // but we guard anyway.
                result.Format = ScrFileReadResult.ScrFormatType.Partial;

                result.Warnings.Add( new ScrWarning
                {
                    Long = "Tilemap region is incomplete." ,
                    Short = "Tilemap incomplete."
                } );

                return;
            }

            // Validate tile entries in the tilemap region
            bool validTilemap = true;

            for ( int i = 0 ; i < 0x2000 ; i += 2 )
            {
                int low = data[i];
                int high = data[i + 1];
                int tileIndex = ((high & 0x03) << 8) | low;

                if ( tileIndex > 1023 )
                {
                    validTilemap = false;
                    break;
                }
            }

            if ( validTilemap )
            {
                result.Format = ScrFileReadResult.ScrFormatType.Strict;

                // Missing footer?
                if ( size < 0x2100 )
                    result.Warnings.Add( new ScrWarning
                    {
                        Long = "File metadata footer is missing." ,
                        Short = "Metadata missing."
                    } );

                // Missing visibility mask?
                if ( size < 0x2300 )
                    result.Warnings.Add( new ScrWarning
                    {
                        Long = "Visibility mask is missing; all tiles will be visible." ,
                        Short = "File doesn't set tile visibility."
                    } );

                return;
            }

            // If tilemap is invalid but size is strict-range → treat as partial
            result.Format = ScrFileReadResult.ScrFormatType.Partial;

            result.Warnings.Add( new ScrWarning
            {
                Long = "Tilemap contains invalid entries; treating as partial." ,
                Short = "File is incomplete."
            } );

            return;
        }

        // ---------------------------------------------
        // 3. Not strict-range → scan for ANY valid tile entry
        // ---------------------------------------------
        bool foundValidTile = false;

        for ( int i = 0 ; i < size - 1 ; i += 2 )
        {
            int low = data[i];
            int high = data[i + 1];
            int tileIndex = ((high & 0x03) << 8) | low;

            if ( tileIndex <= 1023 )
            {
                foundValidTile = true;
                break;
            }
        }

        if ( foundValidTile )
        {
            result.Format = ScrFileReadResult.ScrFormatType.Partial;

            result.Warnings.Add( new ScrWarning
            {
                Long = "Partial SCR detected (non-standard size or structure)." ,
                Short = "File is incomplete."
            } );

            return;
        }

        // ---------------------------------------------
        // 4. No valid tile entries → unreadable
        // ---------------------------------------------
        result.Format = ScrFileReadResult.ScrFormatType.Unreadable;

        result.Warnings.Add( new ScrWarning
        {
            Long = "File contains no valid tile entries and cannot be parsed." ,
            Short = "File cannot be opened."
        } );
    }





    public static ScrFile ParseStrict(byte[] data)
    {
        // Error message if malformed file ends up parsed as strict
        if ( data == null || data.Length < TilemapSize )
        {
            return null;
        }

        // Tilemap is always 4 blocks for strict SCR
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
        // ---------------------------------------------
        bool[][] scrClearMask = new bool[blockCount][];

        // Each block has 0x80 bytes of mask data
        const int MaskBytesPerBlock = VisibilityMaskSize / 4; // 0x80

        for ( int s = 0 ; s < blockCount ; s++ )
        {
            byte[] tmp = new byte[MaskBytesPerBlock];

            for ( int j = 0 ; j < MaskBytesPerBlock ; j++ )
            {
                int srcOffset =
                VisibilityMaskOffset
                + ((s & 2) * MaskBytesPerBlock)
                + ((s & 1) * 4)
                + (j % 4)
                + ((j / 4) * 8);

                tmp[j] = data[srcOffset];
            }

            scrClearMask[s] = ToBitStreamReverse( tmp , TilesPerBlock );
        }

        // ---------------------------------------------
        // Decode tilemap entries + apply visibility mask
        // ---------------------------------------------
        for ( int b = 0 ; b < blockCount ; b++ )
        {
            int blockOffset = b * BlockSizeBytes;
            ScrBlock block  = scr.Blocks[b];

            for ( int i = 0 ; i < TilesPerBlock ; i++ )
            {
                int entryOffset = blockOffset + (i * TileEntrySize);

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




    public static ScrFile ParsePartial(byte[] data)
    {
        int size = data.Length;

        // ---------------------------------------------
        // 1. Determine how many complete tile entries exist
        // ---------------------------------------------
        int tileEntryCount = size / TileEntrySize; // floor division
        int maxPossibleTiles = tileEntryCount;

        // Clamp to maximum possible tiles (4 blocks)
        if ( maxPossibleTiles > TilesPerBlock * 4 )
            maxPossibleTiles = TilesPerBlock * 4;

        // ---------------------------------------------
        // 2. Determine block count (1, 2, or 4)
        // ---------------------------------------------
        int blockCount;

        if ( maxPossibleTiles <= TilesPerBlock )
            blockCount = 1; // 32×32
        else if ( maxPossibleTiles <= TilesPerBlock * 2 )
            blockCount = 2; // 32×64 or 64×32
        else
            blockCount = 4; // 64×64

        // ---------------------------------------------
        // 3. Allocate ScrFile with padded blocks
        // ---------------------------------------------
        var scr = new ScrFile
        {
            BlockCount = blockCount,
            Blocks     = new ScrBlock[blockCount],
            RawFile    = data
        };

        for ( int b = 0 ; b < blockCount ; b++ )
        {
            scr.Blocks[b] = new ScrBlock
            {
                Entries = new ScrEntry[TilesPerBlock] ,
                VisibilityMask = new bool[TilesPerBlock]
            };

            // Default: all tiles visible
            for ( int i = 0 ; i < TilesPerBlock ; i++ )
                scr.Blocks[b].VisibilityMask[i] = true;
        }

        // ---------------------------------------------
        // 4. Copy whatever tile entries exist into blocks
        // ---------------------------------------------
        int tilesToCopy = maxPossibleTiles;
        int tileIndex = 0;

        for ( int b = 0 ; b < blockCount ; b++ )
        {
            ScrBlock block = scr.Blocks[b];

            for ( int i = 0 ; i < TilesPerBlock ; i++ )
            {
                if ( tileIndex < tilesToCopy )
                {
                    int offset = tileIndex * TileEntrySize;

                    ushort raw = (ushort)(
                    data[offset] |
                    (data[offset + 1] << 8)
                );

                    block.Entries[i] = new ScrEntry
                    {
                        RawValue = raw ,
                        IsVisible = true
                    };

                    tileIndex++;
                }
                else
                {
                    // Pad missing tiles with 0x0000
                    block.Entries[i] = new ScrEntry
                    {
                        RawValue = 0 ,
                        IsVisible = true
                    };
                }
            }
        }

        return scr;
    }


}
