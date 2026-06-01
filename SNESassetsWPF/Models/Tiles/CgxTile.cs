using System;
using System.Collections.Generic;
using System.Text;

namespace SNESassetsWPF.Models
{
    public class CgxTile
    {
        public byte[,] Pixels { get; set; } = new byte[8 , 8];
        public int PaletteGroup { get; set; }   // from prefix table (0–15)
        public bool FlipX { get; set; }         // if prefix encodes it
        public bool FlipY { get; set; }         // if prefix encodes it
        public int Priority { get; set; }       // if prefix encodes it
    }
}
