using MS.WindowsAPICodePack.Internal;
using SNESassetsWPF.Models;
using System;
using System.IO;
using System.Text;
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
        // CLASSIFY INCOMING COL FILE (RetroReversing definition)
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

            int validCount = 0;
            int clusterCount = 0;
            bool inCluster = false;
            int lastValidByteIndex = -1;

            for ( int i = 0 ; i + 1 < length && validCount < MaxColours ; i += BytesPerColour )
            {
                ushort value = (ushort)(data[i] | (data[i + 1] << 8));
                bool isValid = (value & 0x8000) == 0;

                if ( isValid )
                {
                    validCount++;
                    lastValidByteIndex = i + 1;
                    if ( !inCluster )
                    {
                        inCluster = true;
                        clusterCount++;
                    }
                }
                else
                {
                    inCluster = false;
                }
            }

            // --- RetroReversing definition: 512 palette bytes + footer + metadata ---
            bool hasFooter = length >= 0x220 &&
                     Encoding.ASCII.GetString(data, 0x200, 0x20)
                     .StartsWith("NAK1989", StringComparison.OrdinalIgnoreCase);

            if ( validCount == MaxColours && hasFooter )
            {
                result.Format = ColFormatType.Valid;
                result.RawColorData = data.Take( PaletteBytes ).ToArray();
                result.RawMetadata = data.Skip( PaletteBytes ).ToArray();
                return;
            }

            // --- Warn classification ---
            result.Format = ColFormatType.Warn;

            if ( validCount < MaxColours )
                result.Warnings.Add( new ColWarning { Short = "Has less than 256 colours." , Long = "Less than 256 valid colours detected." } );

            if ( clusterCount > 1 )
                result.Warnings.Add( new ColWarning { Short = "Colours appear grouped." , Long = "Contiguous groups of valid colours detected before footer." } );

            if ( !hasFooter )
                result.Warnings.Add( new ColWarning { Short = "Junk data after colours." , Long = "Footer string missing or malformed after palette region." } );

            result.RawColorData = data.Take( Math.Min( lastValidByteIndex + 1 , length ) ).ToArray();
            result.RawMetadata = data.Skip( result.RawColorData.Length ).ToArray();
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

            int colourCount = 0, row = 0, colIndex = 0;
            bool inCluster = false;

            for ( int i = 0 ; i + 1 < length ; i += BytesPerColour )
            {
                if ( colourCount >= MaxColours ) break;

                ushort value = (ushort)(data[i] | (data[i + 1] << 8));
                bool isValid = (value & 0x8000) == 0;

                if ( !isValid )
                {
                    inCluster = false;
                    continue;
                }

                inCluster = true;
                int r8 = (value & 0x1F) << 3;
                int g8 = ((value >> 5) & 0x1F) << 3;
                int b8 = ((value >> 10) & 0x1F) << 3;

                col.RawColors[row , colIndex] = new SnesColor( data[i] , data[i + 1] );
                col.RgbColors[row , colIndex] = Color.FromArgb( 255 , (byte)r8 , (byte)g8 , (byte)b8 );

                colourCount++;
                if ( ++colIndex >= ColoursPerRow )
                {
                    colIndex = 0;
                    if ( ++row >= TotalRows ) break;
                }
            }

            // Metadata = everything after palette + footer
            int metaStart = Math.Min(length, PaletteBytes);
            col.Metadata = data.Skip( metaStart ).ToArray();

            if ( colourCount == 0 )
                result.Warnings.Add( new ColWarning { Short = "No colours found." , Long = "No valid colours found — palette filled with placeholders." } );

            col.BuildCachedColors();
            return col;
        }


    }
}
