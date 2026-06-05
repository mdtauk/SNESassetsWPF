using SNESassetsWPF.Models;

namespace SNESassetsWPF.Formats
{
    /// <summary>
    /// Parsed MAP file.
    /// Contains a grid of MapCells holding references to PNL Tiles.
    /// </summary>
    public class MapFile
    {
        /// <summary>
        /// Raw 0x100‑byte MAP header.
        /// </summary>
        public byte[] Header { get; set; }


        /// <summary>
        /// Width of the MAP in MapCells.
        /// </summary>
        public int Width { get; set; }


        /// <summary>
        /// Height of the MAP in MapCells.
        /// </summary>
        public int Height { get; set; }


        /// <summary>
        /// True if Mode 7 flag is set in the MAP header.
        /// (Not currently decoded, but reserved for future use.)
        /// </summary>
        public bool IsMode7Enabled { get; set; }


        /// <summary>
        /// 2D array of MAP cells (Width × Height).
        /// Each cell stores coordinates into the PNL panel.
        /// </summary>
        public MapCell[,] Cells { get; set; }





        /// <summary>
        /// A Map File created as we Parse a MAP file
        /// </summary>
        /// <param name="width"></param>
        /// <param name="height"></param>
        public MapFile(int width , int height)
        {
            Width = width;
            Height = height;

            Cells = new MapCell[width , height];

            // Allocate header to correct size (0x100)
            Header = new byte[0x100];
        }
    }
}
