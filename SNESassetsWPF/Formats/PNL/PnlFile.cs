using SNESassetsWPF.Models;

namespace SNESassetsWPF.Formats
{
    public class PnlFile
    {
        /// <summary>
        /// 16384 tiles (0x4000), each one 8×8 pixels.
        /// </summary>
        public PnlTile[] Tiles { get; set; } = new PnlTile[0x4000];


        /// <summary>
        /// Palette block selector (header[0x65]).
        /// </summary>
        public byte ColHalf { get; set; }


        /// <summary>
        /// Base palette cell (header[0x66]).
        /// </summary>
        public byte ColCell { get; set; }


        /// <summary>
        /// Width of a single group of Tiles.
        /// </summary>
        public int GroupWidth { get; set; }


        /// <summary>
        /// Height of a single group of Tiles.
        /// </summary>
        public int GroupHeight { get; set; }
    }

}