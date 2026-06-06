using SNESassetsWPF.Models;

namespace SNESassetsWPF.Formats
{
    public class PnlTile

    {
        /// <summary>
        /// Raw 16‑bit attribute word from the PNL file.
        /// </summary>
        public ushort RawAttributeWord { get; set; }

        /// <summary>
        /// True if this tile is visible (from the clear table).
        /// </summary>
        public bool IsVisible { get; set; }

        /// <summary>
        /// Tile index (0–1023).
        /// </summary>
        public int TileIndex => RawAttributeWord & 0x03FF;

        /// <summary>
        /// Palette row (0–7).
        /// </summary>
        public int PaletteRow => ( RawAttributeWord >> 10 ) & 0x07;

        /// <summary>
        /// Horizontal flip.
        /// </summary>
        public bool HFlip => ( RawAttributeWord & 0x4000 ) != 0;

        /// <summary>
        /// Vertical flip.
        /// </summary>
        public bool VFlip => ( RawAttributeWord & 0x8000 ) != 0;
    }

}
