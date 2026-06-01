using SNESassetsWPF.Models;
using System;
using System.Windows.Media;

namespace SNESassetsWPF.Formats
{
    public class ColFile
    {
        // 16 palettes × 16 colours
        public SnesColor[,] RawColors { get; set; }
        public Color[,] RgbColors { get; set; }

        // Optional metadata (32 rows)
        public byte[] Metadata { get; set; } = Array.Empty<byte>();

        public ColFile()
        {
            RawColors = new SnesColor[16 , 16];
            RgbColors = new Color[16 , 16];
        }

        // Existing method — DO NOT CHANGE
        public Color GetColor(int index)
        {
            int palette = index / 16;
            int color = index % 16;
            return RgbColors[palette , color];
        }

        // New overload — SAFE, NON‑BREAKING
        public Color GetColor(int row , int col)
        {
            return RgbColors[row , col];
        }
    }
}
