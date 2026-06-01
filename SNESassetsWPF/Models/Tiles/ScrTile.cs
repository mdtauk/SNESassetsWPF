using System;

namespace SNESassetsWPF.Models
{
    /// <summary>
    /// Represents a single tile entry inside an SCR tilemap.
    /// This is a pure model class used by renderers and the UI.
    /// </summary>
    public class ScrTile
    {
        /// <summary>
        /// Tile index (0–1023). Points to a tile in the CGX file.
        /// </summary>
        public int TileIndex { get; set; }

        /// <summary>
        /// Palette row index (0–7 for 4bpp, 0–15 for 8bpp).
        /// </summary>
        public int PaletteIndex { get; set; }

        /// <summary>
        /// Horizontal flip flag.
        /// </summary>
        public bool HFlip { get; set; }

        /// <summary>
        /// Vertical flip flag.
        /// </summary>
        public bool VFlip { get; set; }

        /// <summary>
        /// Priority bit (0 or 1). Used by SNES PPU for layer priority.
        /// </summary>
        public bool Priority { get; set; }

        public override string ToString()
        {
            return $"Tile={TileIndex}, Pal={PaletteIndex}, H={HFlip}, V={VFlip}, P={Priority}";
        }
    }
}
