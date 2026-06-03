using System.Collections.Generic;
using SNESassetsWPF.Models;

namespace SNESassetsWPF.Formats
{
    /// <summary>
    /// Represents a full PNL file as used by S‑CG‑CAD.
    /// Contains the header, the 32×512 tile attribute table,
    /// the tile flag table, and computed metadata such as
    /// meta‑tile width and height.
    /// </summary>
    public class PnlFile
    {
        /// <summary>
        /// Raw 0x100‑byte header block.
        /// Must be preserved exactly when saving.
        /// </summary>
        public byte[] Header { get; set; }

        /// <summary>
        /// The full 32×512 grid of tile entries.
        /// Each entry corresponds to one attribute word and one flag word.
        /// </summary>
        public PnlTile[,] Tiles { get; set; }

        /// <summary>
        /// Meta‑tile width in tiles, computed from header[0x69].
        /// metaWidth = 1 << (header[0x69] & 0x1F)
        /// </summary>
        public int MetaWidth { get; set; }

        /// <summary>
        /// Meta‑tile height in tiles, computed from header[0x6A].
        /// metaHeight = 1 << (header[0x6A] & 0x1F)
        /// </summary>
        public int MetaHeight { get; set; }

        /// <summary>
        /// True if the Mode 7 UI flag is set (header[0x61] != 0).
        /// This is an editor‑side toggle and does not affect tile data.
        /// </summary>
        public bool Mode7Enabled { get; set; }

        /// <summary>
        /// Optional: extracted patterns for convenience.
        /// These are computed from the tile grid and meta‑tile size.
        /// </summary>
        public List<PnlPattern> Patterns { get; set; } = new List<PnlPattern>();

        /// <summary>
        /// Width of the PNL panel in tiles (always 32).
        /// </summary>
        public const int PanelWidth = 32;

        /// <summary>
        /// Height of the PNL panel in tiles (always 512).
        /// </summary>
        public const int PanelHeight = 512;
    }
}
