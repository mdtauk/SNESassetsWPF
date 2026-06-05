namespace SNESassetsWPF.Models
{
    /// <summary>
    /// Represents a single 16‑bit S‑CG‑CAD SCR tile entry.
    /// Raw is the original word from the SCR file.
    /// Other fields are decoded from Raw according to the spec.
    /// </summary>
    public class ScrTile
    {
        /// <summary>
        /// The original 16‑bit SCR word (big‑endian in file).
        /// </summary>
        public ushort Raw { get; set; }

        /// <summary>
        /// Decoded tile index (points into the CGX tile bank).
        /// </summary>
        public int TileIndex { get; set; }

        /// <summary>
        /// Decoded palette index / palette group.
        /// Exact meaning depends on S‑CG‑CAD SCR spec.
        /// </summary>
        public int PaletteIndex { get; set; }

        /// <summary>
        /// Priority bit (if supported by this SCR variant).
        /// </summary>
        public bool Priority { get; set; }

        /// <summary>
        /// Horizontal flip flag.
        /// </summary>
        public bool HFlip { get; set; }

        /// <summary>
        /// Vertical flip flag.
        /// </summary>
        public bool VFlip { get; set; }

        /// <summary>
        /// Visibility flag.
        /// </summary>
        public bool Visible { get; set; }
    }
}
