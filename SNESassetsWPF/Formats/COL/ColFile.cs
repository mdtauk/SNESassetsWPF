using SNESassetsWPF.Models;
using System;
using System.Windows.Media;

namespace SNESassetsWPF.Formats
{
    public class ColFile
    {
        /// <summary>
        /// 16 Rows of 16 COlours in SNES 15bit
        /// </summary>
        public SnesColor[,] RawColors { get; set; }

        /// <summary>
        /// Returns an array of RGB Colours
        /// </summary>
        public Color[,] RgbColors { get; set; }


        /// <summary>
        /// Metadata block as bytes from COL file
        /// </summary>
        public byte[] Metadata { get; set; } = Array.Empty<byte>();

        public ColFile()
        {
            RawColors = new SnesColor[16 , 16];
            RgbColors = new Color[16 , 16];
        }


        /// <summary>
        /// Returns a colour index.
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public Color GetColor(int index)
        {
            int palette = index / 16;
            int color = index % 16;
            return RgbColors[palette , color];
        }


        /// <summary>
        /// Returns an RGB colour from a Row and col index
        /// </summary>
        /// <param name="row"></param>
        /// <param name="col"></param>
        /// <returns></returns>
        public Color GetColor(int row , int col)
        {
            return RgbColors[row , col];
        }


        /// <summary>
        /// Gets the Palette Row
        /// </summary>
        /// <param name="row"></param>
        /// <returns></returns>
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
