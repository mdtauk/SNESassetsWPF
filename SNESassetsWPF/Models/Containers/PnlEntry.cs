namespace SNESassetsWPF.Formats
{
    /// <summary>
    /// Represents a single tile entry in an S‑CG‑CAD PNL file.
    ///
    /// Each tile has two 16‑bit words:
    ///
    ///   • Attribute word (from attribute table)
    ///   • Flag word      (from flag table)
    ///
    /// Both words are stored in BIG‑ENDIAN format in the file.
    ///
    /// Attribute word bit layout:
    ///   Bits  0–9   : TileIndex   (0–1023)
    ///   Bits 10–12  : PaletteRow  (0–7)
    ///   Bit     13  : Priority
    ///   Bit     14  : HFlip
    ///   Bit     15  : VFlip
    ///
    /// Flag word bit layout:
    ///   Bit     15  : IsPresent (1 = tile visible)
    ///   Bits 0–14   : Editor flags (unused by SNES)
    /// </summary>
    public class PnlEntry
    {
        /// <summary>
        /// Raw 16‑bit attribute word (big‑endian in file).
        /// </summary>
        public ushort RawAttributeWord { get; set; }

        /// <summary>
        /// Raw 16‑bit flag word (big‑endian in file).
        /// </summary>
        public ushort RawFlagWord { get; set; }

        // ───────────────────────────────────────────────
        // Decoded attribute fields
        // ───────────────────────────────────────────────

        /// <summary>
        /// Tile index (0–1023).
        /// </summary>
        public int TileIndex => RawAttributeWord & 0x03FF;

        /// <summary>
        /// Palette row (0–7).
        /// </summary>
        public int PaletteRow => ( RawAttributeWord >> 10 ) & 0x07;

        /// <summary>
        /// Priority bit.
        /// </summary>
        public bool Priority => ( RawAttributeWord & 0x2000 ) != 0;

        /// <summary>
        /// Horizontal flip.
        /// </summary>
        public bool HFlip => ( RawAttributeWord & 0x4000 ) != 0;

        /// <summary>
        /// Vertical flip.
        /// </summary>
        public bool VFlip => ( RawAttributeWord & 0x8000 ) != 0;

        // ───────────────────────────────────────────────
        // Decoded flag fields
        // ───────────────────────────────────────────────

        /// <summary>
        /// True if the tile is marked present/visible.
        /// This is bit 15 of the flag word.
        /// </summary>
        public bool IsPresent => ( RawFlagWord & 0x8000 ) != 0;
    }
}
