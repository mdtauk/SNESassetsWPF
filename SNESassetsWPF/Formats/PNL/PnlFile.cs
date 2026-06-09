namespace SNESassetsWPF.Formats
{
    /// <summary>
    /// Represents a PNL file exactly as stored by S‑CG‑CAD.
    ///
    /// A PNL file has a FIXED binary layout:
    ///   0x0000–0x00FF : 256‑byte header
    ///   0x0100–0x80FF : 0x8000‑byte attribute table
    ///   0x8100–0x100FF: 0x8000‑byte flag table
    ///
    /// Total tile count is ALWAYS 16384.
    /// Tiles are arranged in a FIXED 32×512 grid.
    /// </summary>
    public class PnlFile
    {
        public const int HeaderSize = 0x100;
        public const int EntryCount = 16384;
        public const int Width = 32;
        public const int Height = 512;

        /// <summary>
        /// Meta‑tile width (decoded from header[0x69]).
        /// </summary>
        public int MetaWidth { get; set; } = 1;

        /// <summary>
        /// Meta‑tile height (decoded from header[0x6A]).
        /// </summary>
        public int MetaHeight { get; set; } = 1;

        public byte[] RawFile { get; set; } = Array.Empty<byte>();
        public byte[] Header { get; set; } = new byte[HeaderSize];
        public PnlEntry[] Entries { get; set; } = new PnlEntry[EntryCount];

        public PnlEntry? GetEntry(int x , int y)
        {
            if ( x < 0 || y < 0 || x >= Width || y >= Height )
                return null;

            return Entries[y * Width + x];
        }
    }
}
