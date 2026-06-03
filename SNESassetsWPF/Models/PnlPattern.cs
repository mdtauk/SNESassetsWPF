using SNESassetsWPF.Models;

namespace SNESassetsWPF.Models
{
    /// <summary>
    /// Represents a logical meta‑tile (Pattern) extracted from a PNL file.
    /// A Pattern is a rectangular region of the PNL panel grid, whose size
    /// is defined by the PNL header's width/height exponents.
    /// </summary>
    public class PnlPattern
    {
        /// <summary>
        /// X coordinate (0–31) of the pattern's origin in the PNL panel grid.
        /// </summary>
        public int PanelX { get; set; }

        /// <summary>
        /// Y coordinate (0–511) of the pattern's origin in the PNL panel grid.
        /// </summary>
        public int PanelY { get; set; }

        /// <summary>
        /// Width of the pattern in tiles (computed from the PNL header).
        /// </summary>
        public int WidthInTiles { get; set; }

        /// <summary>
        /// Height of the pattern in tiles (computed from the PNL header).
        /// </summary>
        public int HeightInTiles { get; set; }

        /// <summary>
        /// The tiles that make up this pattern. Each entry contains tile ID,
        /// palette row, flip flags, priority, and present/empty state.
        /// </summary>
        public PnlTile[,] Tiles { get; set; }

        /// <summary>
        /// Optional index for UI/debugging when enumerating patterns.
        /// </summary>
        public int PatternIndex { get; set; }
    }
}
