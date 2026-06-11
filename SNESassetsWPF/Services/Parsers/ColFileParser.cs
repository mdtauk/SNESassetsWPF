using MS.WindowsAPICodePack.Internal;
using SNESassetsWPF.Models;
using System;
using System.IO;
using System.Windows.Media;
using static SNESassetsWPF.Formats.ColFileReadResult;

namespace SNESassetsWPF.Formats
{
    public static class ColFileParser
    {
        // ============================================================
        // CONSTANTS (no magic numbers)
        // ============================================================
        private const int PaletteBytes = 512;          // 256 colours × 2 bytes
        private const int ColoursPerRow = 16;          // SNES palette width
        private const int TotalRows = 16;              // SNES palette rows
        private const int MaxColours = 256;            // Total palette entries
        private const int BytesPerColour = 2;          // SNES BGR555




        // ============================================================
        // CLASSIFY INCOMING COL FILE
        // ============================================================
        public static void ClassifyColStructure(ColFileReadResult result)
        {
            const int BytesPerColour = 2;
            const int MaxColours = 256;
            const int PaletteBytes = MaxColours * BytesPerColour;

            byte[] data = result.RawFile;
            int length = data.Length;

            result.RawColorData = Array.Empty<byte>();
            result.RawMetadata = Array.Empty<byte>();
            result.Warnings.Clear();


            // ------------------------------------------------------------
            // 1. Detect valid colours and clusters
            // ------------------------------------------------------------
            int validCount = 0;
            int clusterCount = 0;
            bool inCluster = false;

            int lastValidByteIndex = -1;

            for ( int i = 0 ; i + 1 < length ; i += BytesPerColour )
            {
                ushort value = (ushort)(data[i] | (data[i + 1] << 8));

                bool isValid =
                    ((value & 0x8000) == 0) &&             // bit 15 must be 0
                    ((value & 0x1F) <= 31) &&                // R
                    (((value >> 5) & 0x1F) <= 31) &&         // G
                    (((value >> 10) & 0x1F) <= 31);          // B

                if ( isValid )
                {
                    validCount++;
                    lastValidByteIndex = i + 1;

                    if ( !inCluster )
                    {
                        inCluster = true;
                        clusterCount++;
                    }

                    if ( validCount > MaxColours )
                        result.Warnings.Add( new ColWarning
                        {
                            Long = "File contains more than 256 valid colours — extra colours ignored." ,
                            Short = "Has more than 256 colours."
                        } );

                }
                else
                {
                    inCluster = false;
                }
            }

            // ------------------------------------------------------------
            // 2. FAIL classification
            // ------------------------------------------------------------
            if ( validCount == 0 )
            {
                result.Format = ColFormatType.Fail;
                result.Warnings.Add( new ColWarning
                {
                    Long = "No valid colour values were found in this file." ,
                    Short = "No valid colours."
                } );
                result.RawColorData = Array.Empty<byte>();
                result.RawMetadata = data;
                return;
            }

            // ------------------------------------------------------------
            // 3. VALID classification
            // ------------------------------------------------------------
            if ( validCount == MaxColours && clusterCount == 1 && length >= PaletteBytes )
            {
                // Perfect or headerless full COL
                result.Format = ColFormatType.Valid;

                result.RawColorData = new byte[PaletteBytes];
                Buffer.BlockCopy( data , 0 , result.RawColorData , 0 , PaletteBytes );

                if ( length > PaletteBytes )
                {
                    result.RawMetadata = new byte[length - PaletteBytes];
                    Buffer.BlockCopy( data , PaletteBytes , result.RawMetadata , 0 , length - PaletteBytes );
                }
                else
                {
                    result.RawMetadata = Array.Empty<byte>();
                }

                return;
            }

            // ------------------------------------------------------------
            // 4. WARN classification
            // ------------------------------------------------------------
            result.Format = ColFormatType.Warn;

            if ( validCount < MaxColours )
                result.Warnings.Add( new ColWarning
                {
                    Long = "Less than 256 valid colours were detected." ,
                    Short = "Has less than 256 colours."
                } );

            if ( clusterCount > 1 )
                result.Warnings.Add( new ColWarning
                {
                    Long = "Contiguous groups of valid colours detected." ,
                    Short = "Colours appear grouped."
                } );

            if ( clusterCount > 16 )
                result.Warnings.Add( new ColWarning
                {
                    Long = "More than 16 groups of colours detected." ,
                    Short = "Colour order will not match."
                } );

            if ( length % 2 != 0 )
                result.Warnings.Add( new ColWarning
                {
                    Long = "Odd number of bytes — last byte ignored." ,
                    Short = "Final bytes were not a colour."
                } );

            if ( length > PaletteBytes )
                result.Warnings.Add( new ColWarning
                {
                    Long = "File contains extra data beyond expected palette region." ,
                    Short = "Junk data after colours."
                } );

            // Extract colour region = up to last valid colour
            int colourBytes = Math.Min(lastValidByteIndex + 1, length);
            result.RawColorData = new byte[colourBytes];
            Buffer.BlockCopy( data , 0 , result.RawColorData , 0 , colourBytes );

            // Metadata = everything after last valid colour
            if ( colourBytes < length )
            {
                int metaLength = length - colourBytes;
                result.RawMetadata = new byte[metaLength];
                Buffer.BlockCopy( data , colourBytes , result.RawMetadata , 0 , metaLength );
            }
            else
            {
                result.RawMetadata = Array.Empty<byte>();
            }
        }




        // ============================================================
        // STRICT PARSER
        // ============================================================
        public static ColFile ParseStrict(ColFileReadResult result)
        {
            if ( !result.Success )
                return null;

            if ( result.RawFile == null || result.RawFile.Length < PaletteBytes )
                return null;

            var col = new ColFile();

            // Palette = first 512 bytes
            byte[] palette = new byte[PaletteBytes];
            Buffer.BlockCopy( result.RawFile , 0 , palette , 0 , PaletteBytes );

            for ( int i = 0 ; i < PaletteBytes ; i += BytesPerColour )
            {
                byte low = palette[i];
                byte high = palette[i + 1];

                var snes = new SnesColor(low, high);
                int value = snes.Value;

                int r = (value & 0x1F) << 3;
                int g = ((value >> 5) & 0x1F) << 3;
                int b = ((value >> 10) & 0x1F) << 3;

                var rgb = Color.FromArgb(255 , (byte)r, (byte)g, (byte)b);

                int index = i / BytesPerColour;
                int row = index / ColoursPerRow;
                int colIndex = index % ColoursPerRow;

                col.RawColors[row , colIndex] = snes;
                col.RgbColors[row , colIndex] = rgb;
            }

            // Metadata = everything after 512 bytes
            if ( result.RawFile.Length > PaletteBytes )
            {
                int metaLength = result.RawFile.Length - PaletteBytes;
                col.Metadata = new byte[metaLength];
                Buffer.BlockCopy( result.RawFile , PaletteBytes , col.Metadata , 0 , metaLength );
            }

            col.BuildCachedColors();
            return col;
        }




        // ============================================================
        // PARTIAL PARSER
        // ============================================================
        public static ColFile ParsePartial(ColFileReadResult result)
        {
            var col = new ColFile();

            byte[] data = result.RawFile;
            int length = data.Length;

            int colourCount = 0;
            int row = 0;
            int colIndex = 0;

            // Fill everything with placeholders first
            for ( int r = 0 ; r < TotalRows ; r++ )
                for ( int c = 0 ; c < ColoursPerRow ; c++ )
                    col.RgbColors[r , c] = Color.FromArgb(0 , 128 , 128 , 128);

            bool inCluster = false;

            // Walk through file 2 bytes at a time
            for ( int i = 0 ; i + 1 < length ; i += BytesPerColour )
            {
                if ( colourCount >= MaxColours )
                {
                    result.Warnings.Add( new ColWarning
                    {
                        Long = "File contains more than 256 valid colours — extra colours ignored." ,
                        Short = "First 256 colours loaded."
                    } );
                    break;
                }

                byte low = data[i];
                byte high = data[i + 1];

                var snes = new SnesColor(low, high);
                int value = snes.Value;

                // VALID SNES COLOUR?
                bool isValid =
                    ((value & 0x8000) == 0) &&             // bit 15 must be 0
                    ((value & 0x1F) <= 31) &&                // R
                    (((value >> 5) & 0x1F) <= 31) &&         // G
                    (((value >> 10) & 0x1F) <= 31);          // B

                if ( !isValid )
                {
                    // Cluster break → insert ONE placeholder if we were in a cluster
                    if ( inCluster )
                    {
                        colIndex++;

                        if ( colIndex >= ColoursPerRow )
                        {
                            colIndex = 0;
                            row++;

                            if ( row >= TotalRows )
                            {
                                result.Warnings.Add( new ColWarning
                                {
                                    Long = "More than 16 palette rows detected — extra rows ignored." ,
                                    Short = "First 16 rows of colours loaded."
                                } );

                                break;
                            }
                        }
                    }

                    inCluster = false;
                    continue;
                }

                // VALID COLOUR
                inCluster = true;

                int r8 = (value & 0x1F) << 3;
                int g8 = ((value >> 5) & 0x1F) << 3;
                int b8 = ((value >> 10) & 0x1F) << 3;

                col.RawColors[row , colIndex] = snes;
                col.RgbColors[row , colIndex] = Color.FromArgb( 255 , (byte)r8 , (byte)g8 , (byte)b8 );

                colourCount++;
                colIndex++;

                // Row full?
                if ( colIndex >= ColoursPerRow )
                {
                    colIndex = 0;
                    row++;

                    if ( row >= TotalRows )
                    {
                        result.Warnings.Add( new ColWarning
                        {
                            Long = "More than 16 palette rows detected — extra rows ignored." ,
                            Short = "First 16 rows of colours loaded."
                        } );

                        break;
                    }
                }
            }

            // Metadata = everything after the last processed byte
            int processedBytes = colourCount * BytesPerColour;
            if ( processedBytes < length )
            {
                int metaLength = length - processedBytes;
                col.Metadata = new byte[metaLength];
                Buffer.BlockCopy( data , processedBytes , col.Metadata , 0 , metaLength );
            }

            if ( colourCount == 0 )
                result.Warnings.Add( new ColWarning
                {
                    Long = "No valid colours found — palette filled with placeholders." ,
                    Short = "No colours found."
                } );

            col.BuildCachedColors();
            return col;
        }

    }
}
