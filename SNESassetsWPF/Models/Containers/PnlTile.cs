namespace SNESassetsWPF.Models
{
    /// <summary>
    /// Represents a single tile entry inside a PNL panel.
    /// This corresponds to one 16‑bit attribute word and one 16‑bit flag word.
    /// </summary>
    public class PnlTile
    {
        /// <summary>
        /// CGX tile index (0–1023). Extracted from bits 0–9 of the attribute word.
        /// </summary>
        public int TileId { get; set; }

        /// <summary>
        /// Palette row (0–7). Extracted from bits 10–12 of the attribute word.
        /// </summary>
        public int PaletteRow { get; set; }

        /// <summary>
        /// Horizontal flip flag. Extracted from bit 14 of the attribute word.
        /// </summary>
        public bool HFlip { get; set; }

        /// <summary>
        /// Vertical flip flag. Extracted from bit 15 of the attribute word.
        /// </summary>
        public bool VFlip { get; set; }

        /// <summary>
        /// Priority flag. Extracted from bit 13 of the attribute word.
        /// </summary>
        public bool Priority { get; set; }

        /// <summary>
        /// True if the tile is present/active. Extracted from bit 15 of the flag word.
        /// </summary>
        public bool IsPresent { get; set; }

        /// <summary>
        /// The raw 16‑bit attribute word exactly as stored in the PNL file.
        /// This contains the tile ID, palette row, priority, and flip bits.
        /// Preserved for round‑tripping and debugging.
        /// </summary>
        public ushort RawAttributeWord { get; set; }

        /// <summary>
        /// The raw 16‑bit flag word exactly as stored in the PNL file.
        /// Only bit 15 (present flag) is used by S‑CG‑CAD; all other bits must be preserved.
        /// </summary>
        public ushort RawFlagWord { get; set; }
    }
}
