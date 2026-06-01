namespace SNESassetsWPF.Formats
{
    /// <summary>
    /// Result of reading a CGX file from disk.
    /// Contains raw sections exactly as they appear in the file,
    /// plus derived properties such as bit depth and tile count.
    /// </summary>
    public class CgxFileReadResult
    {
        /// <summary>
        /// Full raw CGX file bytes.
        /// Useful for debugging, hex inspection, and verifying offsets.
        /// </summary>
        public byte[] RawFile { get; set; } = Array.Empty<byte>();

        /// <summary>
        /// Raw tile data starting at byte 0 of the CGX file.
        /// Length depends on bit depth:
        ///   2bpp = 0x4000 bytes
        ///   4bpp = 0x8000 bytes
        ///   8bpp = 0x10000 bytes
        /// </summary>
        public byte[] TileData { get; set; } = Array.Empty<byte>();

        /// <summary>
        /// Optional prefix table (editor‑side palette group / tile attributes).
        /// Present only in 2bpp and 4bpp CGX files.
        /// Typically 0x400 bytes (1 byte per tile for 1024 tiles).
        /// </summary>
        public byte[] PrefixTable { get; set; } = Array.Empty<byte>();

        /// <summary>
        /// Trailing S‑CG‑CAD metadata block.
        /// Always exactly 0x100 bytes at the end of the file.
        /// Contains ASCII text such as "NAK1989 S‑CG‑CAD Ver...".
        /// Not used by SNES hardware.
        /// </summary>
        public byte[] Metadata { get; set; } = Array.Empty<byte>();

        /// <summary>
        /// Bit depth of the CGX tile data (2, 4, or 8 bits per pixel).
        /// Determined from file size.
        /// </summary>
        public int BitDepth { get; set; }

        /// <summary>
        /// Number of bytes per tile:
        ///   2bpp = 16 bytes
        ///   4bpp = 32 bytes
        ///   8bpp = 64 bytes
        /// </summary>
        public int BytesPerTile { get; set; }

        /// <summary>
        /// Total number of tiles in the CGX file.
        /// Computed as TileData.Length / BytesPerTile.
        /// </summary>
        public int TileCount { get; set; }

        /// <summary>
        /// True if the file was read successfully and all sections parsed.
        /// </summary>
        public bool IsValid { get; set; }

        /// <summary>
        /// Error message if reading or parsing failed.
        /// Empty when IsValid is true.
        /// </summary>
        public string ErrorMessage { get; set; } = string.Empty;
    }
}
