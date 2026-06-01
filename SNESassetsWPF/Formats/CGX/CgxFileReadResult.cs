namespace SNESassetsWPF.Formats
{
    public class CgxFileReadResult
    {
        /// <summary>
        /// Raw tile data from byte 0.
        /// </summary>
        public byte[] TileData { get; set; } = Array.Empty<byte>();

        /// <summary>
        /// Optional per‑tile prefix table (palette grouping / attributes).
        /// Usually 0x400 bytes for 1024 tiles.
        /// </summary>
        public byte[] PrefixTable { get; set; } = Array.Empty<byte>();

        /// <summary>
        /// Trailing S‑CG‑CAD metadata block (ASCII "NAK1989 S‑CG‑CAD Ver...").
        /// Always 0x100 bytes when present.
        /// </summary>
        public byte[] Metadata { get; set; } = Array.Empty<byte>();

        /// <summary>
        /// Bit depth of the tile data (2, 4, or 8 bpp).
        /// </summary>
        public int BitDepth { get; set; } = 4;

        public bool IsValid { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
    }
}
