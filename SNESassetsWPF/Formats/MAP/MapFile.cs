using SNESassetsWPF.Models;

namespace SNESassetsWPF.Formats
{
    /// <summary>
    /// Parsed MAP file. 
    /// Contains a grid of MapTile entries referencing PNL meta-tiles.
    /// </summary>
    public class MapFile
    {
        /// <summary>
        /// Width of the MAP in meta-tiles.
        /// </summary>
        public int Width { get; set; }

        /// <summary>
        /// Height of the MAP in meta-tiles.
        /// </summary>
        public int Height { get; set; }

        /// <summary>
        /// True if Mode 7 flag is set in the MAP header.
        /// </summary>
        public bool Mode7Enabled { get; set; }

        /// <summary>
        /// Raw header bytes (for round-tripping/debug).
        /// </summary>
        public byte[] Header { get; set; }

        /// <summary>
        /// 2D array of MAP tiles (Width × Height).
        /// Each entry references a meta-tile in the PNL.
        /// </summary>
        public MapTile[,] Tiles { get; set; }

        public MapFile(int width , int height)
        {
            Width = width;
            Height = height;

            Tiles = new MapTile[width , height];
            Header = new byte[0];
        }
    }
}
