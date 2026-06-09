namespace SNESassetsWPF.Models
{
    /// <summary>
    /// One SNES tilemap entry from an SCR block.
    ///
    /// SCR entries use the standard SNES BG tilemap format:
    ///
    ///     Bits  0–9   = TileIndex   (0–1023)
    ///     Bits 10–12  = PaletteRow  (0–7)
    ///     Bit     13  = Priority    (BG priority flag)
    ///     Bit     14  = HFlip       (horizontal flip)
    ///     Bit     15  = VFlip       (vertical flip)
    ///
    /// Notes:
    ///   • TileIndex refers to a PNL tile index, not CGX tile index.
    ///     S‑CG‑CAD uses PNL as the tile source for SCR.
    ///
    ///   • PaletteRow selects one of the 8 rows in the COL file.
    ///
    ///   • Priority is stored but not used by S‑CG‑CAD’s editor view.
    ///
    ///   • HFlip/VFlip match SNES hardware semantics exactly.
    /// </summary>
    public class ScrEntry
    {
        /// <summary>
        /// Raw 16‑bit SNES tilemap word.
        /// </summary>
        public ushort RawValue { get; set; }

        /// <summary>Tile index (0–1023) referencing a PNL tile.</summary>
        public int TileIndex => RawValue & 0x03FF;

        /// <summary>Palette row (0–7) selecting a row in the COL file.</summary>
        public int PaletteRow => ( RawValue >> 10 ) & 0x07;

        /// <summary>SNES BG priority flag.</summary>
        public bool Priority => ( RawValue & 0x2000 ) != 0;

        /// <summary>Horizontal flip flag.</summary>
        public bool HFlip => ( RawValue & 0x4000 ) != 0;

        /// <summary>Vertical flip flag.</summary>
        public bool VFlip => ( RawValue & 0x8000 ) != 0;

        /// <summary>
        /// Visibility flag.
        /// </summary>
        public bool IsVisible { get; set; } = true;

    }
}
