namespace SNESassetsWPF.Models
{
    /// <summary>
    /// Represents one 32×32 tile block inside an S‑CG‑CAD SCR file.
    /// Each block contains 1024 tile entries (16-bit words).
    /// </summary>
    public class ScrBlock
    {
        /// <summary>
        /// Block index in file order.
        /// </summary>
        public int BlockIndex { get; }

        /// <summary>
        /// Width of the block in tiles (always 32).
        /// </summary>
        public int WidthTiles { get; } = 32;

        /// <summary>
        /// Height of the block in tiles (always 32).
        /// </summary>
        public int HeightTiles { get; } = 32;

        /// <summary>
        /// 2D array of tile entries for this block.
        /// Indexed as [row, column] = [y, x].
        /// </summary>
        public ScrTile[,] Tiles { get; }

        public ScrBlock(int blockIndex)
        {
            BlockIndex = blockIndex;
            Tiles = new ScrTile[32 , 32];
        }

        /// <summary>
        /// Convenience accessor for a tile at (x, y) inside this block.
        /// </summary>
        public ScrTile GetTile(int x , int y) => Tiles[y , x];
    }
}
