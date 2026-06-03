using SNESassetsWPF.Models;

namespace SNESassetsWPF.Formats
{
    /// <summary>
    /// Represents a fully parsed S‑CG‑CAD SCR tilemap.
    /// Contains 1, 2, or 4 blocks (32×32 tiles each),
    /// arranged to form a 32×32, 64×32, 32×64, or 64×64 tilemap.
    /// </summary>
    public class ScrFile
    {
        /// <summary>
        /// The raw SCR file bytes.
        /// Needed so we can re-parse when endian mode changes.
        /// </summary>
        public byte[] RawBytes { get; }

        /// <summary>
        /// Width of the full tilemap in tiles.
        /// </summary>
        public int WidthTiles { get; }

        /// <summary>
        /// Height of the full tilemap in tiles.
        /// </summary>
        public int HeightTiles { get; }

        /// <summary>
        /// Number of 32×32 blocks in this SCR (1, 2, or 4).
        /// </summary>
        public int BlockCount { get; }

        /// <summary>
        /// The 32×32 blocks that make up the SCR file.
        /// </summary>
        public ScrBlock[] Blocks { get; }

        /// <summary>
        /// Flattened tilemap for convenience.
        /// Indexed as [row, column] = [y, x].
        /// </summary>
        public ScrTile[,] Tiles { get; }

        /// <summary>
        /// Footer metadata extracted from the SCR file.
        /// </summary>
        public byte[] Footer { get; set; }

        /// <summary>
        /// Visibility stored as an array per block and per tile.
        /// Visibility[block][tileIndex] = true/false.
        /// </summary>
        public bool[][] Visibility { get; set; }

        /// <summary>
        /// Creates a new SCR file container with fixed dimensions.
        /// The parser is responsible for populating Blocks and Tiles.
        /// </summary>
        public ScrFile(byte[] rawBytes , int widthTiles , int heightTiles , int blockCount)
        {
            RawBytes = rawBytes;
            WidthTiles = widthTiles;
            HeightTiles = heightTiles;
            BlockCount = blockCount;

            // Global tilemap
            Tiles = new ScrTile[heightTiles , widthTiles];

            // 32×32 blocks
            Blocks = new ScrBlock[blockCount];
            for ( int i = 0 ; i < blockCount ; i++ )
                Blocks[i] = new ScrBlock( i );
        }

        /// <summary>
        /// Convenience accessor for a tile at (x, y).
        /// </summary>
        public ScrTile GetTile(int x , int y) => Tiles[y , x];
    }
}
