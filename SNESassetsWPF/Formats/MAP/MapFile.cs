using SNESassetsWPF.Models;

namespace SNESassetsWPF.Formats
{
    /// <summary>
    /// Parsed MAP file. Contains a grid of MapCells referencing PNL groups.
    /// </summary>
    public class MapFile
    {
        /// <summary>
        /// Raw 0x100-byte MAP header.
        /// </summary>
        public byte[] Header { get; set; }

        /// <summary>
        /// Width of the MAP in PNL-groups (always 64).
        /// </summary>
        public int Width { get; set; } = 64;

        /// <summary>
        /// Height of the MAP in PNL-groups.
        /// Computed from file length.
        /// </summary>
        public int Height { get; set; }

        /// <summary>
        /// True if Mode 7 flag is set in the MAP header.
        /// </summary>
        public bool IsMode7Enabled { get; set; }

        /// <summary>
        /// 2D array of MAP cells (Width × Height).
        /// </summary>
        public MapCell[,] Cells { get; set; }

        public MapFile(int height)
        {
            Height = height;
            Cells = new MapCell[Width , Height];
            Header = new byte[0x100];
        }
    }
}
