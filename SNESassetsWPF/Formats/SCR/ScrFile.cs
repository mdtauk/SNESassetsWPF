using SNESassetsWPF.Models;

namespace SNESassetsWPF.Formats
{
    /// <summary>
    /// Represents a fully parsed SCR tilemap.
    /// Contains dimensions and a 2D grid of ScrTile objects.
    /// </summary>
    public class ScrFile
    {
        /// <summary>
        /// Width of the tilemap in tiles.
        /// </summary>
        public int WidthTiles { get; set; }

        /// <summary>
        /// Height of the tilemap in tiles.
        /// </summary>
        public int HeightTiles { get; set; }

        /// <summary>
        /// 2D array of tile metadata parsed from the SCR file.
        /// Indexed as [row, column] = [y, x].
        /// </summary>
        public ScrTile[,] Tiles { get; set; }

        /// <summary>
        /// Convenience accessor for a tile at (x, y).
        /// </summary>
        public ScrTile GetTile(int x , int y) => Tiles[y , x];
    }
}
