namespace SNESassetsWPF.Formats
{
    /// <summary>
    /// Represents a PNL file exactly as stored by S‑CG‑CAD.
    ///
    /// A PNL file has a FIXED binary layout:
    ///
    ///   0x0000–0x00FF : 256‑byte header (editor metadata only)
    ///   0x0100–0x80FF : 0x8000‑byte attribute table (16384 entries × 2 bytes)
    ///   0x8100–0x100FF: 0x8000‑byte flag table      (16384 entries × 2 bytes)
    ///
    /// Total tile count is ALWAYS 16384.
    /// Tiles are arranged in a FIXED 32×512 grid.
    ///
    /// None of these dimensions are stored in the file — they are implicit
    /// to the S‑CG‑CAD format and must be enforced by the parser.
    /// </summary>
    public class PnlFile
    {
        /// <summary>
        /// Size of the header block at the start of the file.
        /// </summary>
        public const int HeaderSize = 0x100;

        /// <summary>
        /// Number of tiles in every S‑CG‑CAD PNL file.
        /// </summary>
        public const int EntryCount = 16384;

        /// <summary>
        /// Logical width of the PNL tilemap (in tiles).
        /// This is a fixed property of the file format.
        /// </summary>
        public const int Width = 32;

        /// <summary>
        /// Logical height of the PNL tilemap (in tiles).
        /// This is a fixed property of the file format.
        /// </summary>
        public const int Height = 512;

        /// <summary>
        /// Raw file bytes exactly as loaded from disk.
        /// Stored for round‑tripping and debugging.
        /// </summary>
        public byte[] RawFile { get; set; } = Array.Empty<byte>();

        /// <summary>
        /// The 0x100‑byte header block.
        /// Contains editor metadata (not used by SNES hardware).
        /// </summary>
        public byte[] Header { get; set; } = new byte[HeaderSize];

        /// <summary>
        /// Parsed entries.
        /// Each entry contains:
        ///   • Raw attribute word (from attribute table)
        ///   • Raw flag word      (from flag table)
        ///   • Decoded fields (TileIndex, PaletteRow, flips, IsPresent)
        ///
        /// Length is ALWAYS 16384.
        /// </summary>
        public PnlEntry[] Entries { get; set; } = new PnlEntry[EntryCount];

        /// <summary>
        /// Safe accessor for an entry at (x,y).
        /// Returns null if out of bounds.
        /// </summary>
        public PnlEntry? GetEntry(int x , int y)
        {
            if ( x < 0 || y < 0 || x >= Width || y >= Height )
                return null;

            return Entries[y * Width + x];
        }

    }
}
