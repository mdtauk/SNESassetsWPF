using SNESassetsWPF.Models;

namespace SNESassetsWPF.Formats
{
    /// <summary>
    /// Represents a fully parsed S‑CG‑CAD SCR tilemap.
    /// Contains 4 blocks (32×32 tiles each) arranged in a 2×2 grid,
    /// forming a full 64×64 tilemap.
    /// </summary>
    public class ScrFile
    {
        /// <summary>
        /// Width of the full tilemap in tiles (always 64 for S‑CG‑CAD SCR).
        /// </summary>
        public int WidthTiles { get; }

        /// <summary>
        /// Height of the full tilemap in tiles (always 64 for S‑CG‑CAD SCR).
        /// </summary>
        public int HeightTiles { get; }

        /// <summary>
        /// The four 32×32 blocks that make up the SCR file.
        /// Block order is always:
        /// 0 = top-left, 1 = top-right,
        /// 2 = bottom-left, 3 = bottom-right.
        /// </summary>
        public ScrBlock[] Blocks { get; }

        /// <summary>
        /// Flattened 64×64 tilemap for convenience.
        /// Indexed as [row, column] = [y, x].
        /// </summary>
        public ScrTile[,] Tiles { get; }

        /// <summary>
        /// Creates a new SCR file container with fixed dimensions.
        /// The parser is responsible for populating Blocks and Tiles.
        /// </summary>
        public ScrFile(int widthTiles , int heightTiles)
        {
            WidthTiles = widthTiles;
            HeightTiles = heightTiles;

            // Global 64×64 tilemap
            Tiles = new ScrTile[heightTiles , widthTiles];

            // Four 32×32 blocks
            Blocks = new ScrBlock[4]
            {
                new ScrBlock(0), // top-left
                new ScrBlock(1), // top-right
                new ScrBlock(2), // bottom-left
                new ScrBlock(3)  // bottom-right
            };
        }

        /// <summary>
        /// Convenience accessor for a tile at (x, y).
        /// </summary>
        public ScrTile GetTile(int x , int y) => Tiles[y , x];
    }
}
