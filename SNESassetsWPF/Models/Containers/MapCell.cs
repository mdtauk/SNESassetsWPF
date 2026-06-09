namespace SNESassetsWPF.Models
{
    /// <summary>
    /// Represents a single 16‑bit MAP entry.
    ///
    /// RawValue:
    ///   • Lower 14 bits = PNL index (0–16383)
    ///   • Upper 2 bits are reserved (always 0 in S‑CG‑CAD output)
    ///
    /// PnlIndex:
    ///   • Extracted from RawValue & 0x3FFF
    ///   • Points into the 32×512 PNL workspace (16384 entries)
    /// </summary>
    public class MapCell
    {
        /// <summary>
        /// Raw 16‑bit MAP entry as stored in the file.
        /// </summary>
        public ushort RawValue { get; set; }

        /// <summary>
        /// Index into the PNL workspace (0–16383).
        /// Extracted from RawValue & 0x3FFF.
        /// </summary>
        public int PnlIndex { get; set; }
    }
}
