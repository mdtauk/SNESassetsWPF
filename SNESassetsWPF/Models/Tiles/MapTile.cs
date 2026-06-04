namespace SNESassetsWPF.Models
{
    /// <summary>
    /// One MAP cell.  
    /// References a *meta‑tile* in the PNL, not a CGX tile.
    /// </summary>
    public class MapTile
    {
        /// <summary>
        /// Index of the meta‑tile inside the PNL.
        /// </summary>
        public int MetaTileIndex { get; set; }

        /// <summary>
        /// Flip the entire meta‑tile horizontally.
        /// </summary>
        public bool HFlip { get; set; }

        /// <summary>
        /// Flip the entire meta‑tile vertically.
        /// </summary>
        public bool VFlip { get; set; }

        /// <summary>
        /// Optional palette override for the whole meta‑tile (0–7).
        /// </summary>
        public int PaletteRowOverride { get; set; }

        /// <summary>
        /// Priority bit applied to the whole meta‑tile.
        /// </summary>
        public bool Priority { get; set; }

        /// <summary>
        /// Raw 16‑bit MAP attribute word (for round‑tripping/debug).
        /// </summary>
        public ushort RawAttributeWord { get; set; }
    }
}
