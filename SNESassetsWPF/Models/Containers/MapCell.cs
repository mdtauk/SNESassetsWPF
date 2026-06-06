namespace SNESassetsWPF.Models
{
    /// <summary>
    /// One cell in a MAP file. Each cell references a PNL tile-group.
    /// </summary>
    public class MapCell
    {
        /// <summary>
        /// X position in the MAP grid (0–63).
        /// </summary>
        public int X { get; set; }

        /// <summary>
        /// Y position in the MAP grid.
        /// </summary>
        public int Y { get; set; }

        /// <summary>
        /// Raw 16-bit MAP entry.
        /// </summary>
        public ushort RawValue { get; set; }

        /// <summary>
        /// Index of the PNL tile-group (0–1023).
        /// </summary>
        public int PnlGroupIndex => RawValue & 0x03FF;

        /// <summary>
        /// Palette row override (0–7).
        /// </summary>
        public int PaletteOverride => ( RawValue >> 10 ) & 0x07;

        /// <summary>
        /// Horizontal flip flag.
        /// </summary>
        public bool HFlip => ( RawValue & 0x2000 ) != 0;

        /// <summary>
        /// Vertical flip flag.
        /// </summary>
        public bool VFlip => ( RawValue & 0x4000 ) != 0;

        /// <summary>
        /// Priority flag.
        /// </summary>
        public bool Priority => ( RawValue & 0x8000 ) != 0;
    }
}
