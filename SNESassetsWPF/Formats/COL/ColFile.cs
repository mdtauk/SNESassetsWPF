using SNESassetsWPF.Models;
using System;
using System.Windows.Media;

namespace SNESassetsWPF.Formats
{
    /// <summary>
    /// Represents an S‑CG‑CAD .COL file.
    ///
    /// A COL file contains:
    ///   • 256 SNES BGR555 colours (16 rows × 16 colours)
    ///   • Optional editor metadata (layout varies by version)
    ///
    /// The palette layout is fixed and matches the SNES PPU:
    ///   Row = palette row (0–15)
    ///   Col = colour index within row (0–15)
    ///
    /// CGX files reference these colours directly by 8‑bit index:
    ///   flatIndex = (paletteRow * 16) + colourIndex
    ///
    /// This class stores both the raw SNES values and decoded RGB.
    /// A 256‑entry flat cache is also built for fast renderer lookup.
    /// </summary>
    public class ColFile
    {
        /// <summary>
        /// Raw SNES BGR555 values exactly as stored in the COL file.
        /// Indexed as [paletteRow, colourIndex].
        /// </summary>
        public SnesColor[,] RawColors { get; set; }

        /// <summary>
        /// Decoded RGB colours (8‑bit per channel).
        /// Same layout as RawColors.
        /// </summary>
        public Color[,] RgbColors { get; set; }

        /// <summary>
        /// Editor metadata block from the COL file.
        /// S‑CG‑CAD stores UI flags, BG/OBJ mode, etc.
        /// </summary>
        public byte[] Metadata { get; set; } = Array.Empty<byte>();

        /// <summary>
        /// Flat 256‑colour cache.
        /// Mirrors the SNES palette memory layout:
        ///   index = paletteRow * 16 + colourIndex
        ///
        /// This matches how 8bpp CGX tiles reference colours.
        /// </summary>
        public Color[] CachedColors { get; private set; } = new Color[256];

        public ColFile()
        {
            RawColors = new SnesColor[16 , 16];
            RgbColors = new Color[16 , 16];
        }

        /// <summary>
        /// Builds the flat 256‑colour cache from RgbColors.
        /// Must be called once after parsing the COL file.
        ///
        /// This does NOT modify the underlying format — it simply
        /// mirrors the SNES palette layout for fast renderer access.
        /// </summary>
        public void BuildCachedColors()
        {
            int k = 0;
            for ( int row = 0 ; row < 16 ; row++ )
            {
                for ( int col = 0 ; col < 16 ; col++ )
                {
                    CachedColors[k++] = RgbColors[row , col];
                }
            }
        }

        /// <summary>
        /// Fast lookup using the SNES flat palette index (0–255).
        /// This is the same index used by 8bpp CGX tiles.
        /// </summary>
        public Color GetColor(int index)
        {
            if ( index < 0 || index >= 256 )
                return Colors.Magenta;

            return CachedColors[index];
        }

        /// <summary>
        /// Row/column lookup (rarely used by renderers).
        /// Matches the physical layout of the COL file.
        /// </summary>
        public Color GetColor(int row , int col)
        {
            return RgbColors[row , col];
        }

        /// <summary>
        /// Returns one palette row (16 colours).
        /// Useful for UI palette grids.
        /// </summary>
        public Color[] GetPaletteRow(int row)
        {
            if ( row < 0 || row >= 16 )
                row = 0;

            var result = new Color[16];
            for ( int i = 0 ; i < 16 ; i++ )
                result[i] = RgbColors[row , i];

            return result;
        }
    }
}
