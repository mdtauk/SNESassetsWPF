namespace SNESassetsWPF.Formats
{
    /// <summary>
    /// One 8×8 CGX tile.
    /// Contains ONLY pixel indices decoded from bitplane data.
    /// </summary>
    public class CgxTile
    {
        /// <summary>
        /// Decoded 8×8 pixel indices (0–255 for 8bpp).
        /// These are palette indices, not RGB colours.
        /// </summary>
        public byte[,] Pixels { get; set; } = new byte[8 , 8];


        /// <summary>
        /// The 4‑bit palette row (0–15) assigned to this tile, as stored in the CGX
        /// tile prefix table. This value selects which 16‑colour palette row from the
        /// COL file should be applied when rendering the tile. Pixel values inside the
        /// tile are 0–15 (colour‑within‑row); the final colour index is computed as
        /// (PaletteRow * 16) + PixelValue.
        /// </summary>
        public int PaletteRow { get; set; }

    }
}
