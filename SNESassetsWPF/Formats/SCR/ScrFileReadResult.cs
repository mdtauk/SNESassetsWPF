namespace SNESassetsWPF.Formats
{
    /// <summary>
    /// Result wrapper for SCR file loading.
    /// Contains raw file bytes, inferred dimensions,
    /// and the parsed ScrFile object.
    /// </summary>
    public class ScrFileReadResult
    {
        /// <summary>
        /// Full raw SCR file bytes as read from disk.
        /// Useful for debugging, hex inspection, and verifying block layout.
        /// </summary>
        public byte[] RawFile { get; set; } = Array.Empty<byte>();

        /// <summary>
        /// Width of the tilemap in tiles (32 or 64).
        /// Determined by ScrFileReader based on block count.
        /// </summary>
        public int WidthTiles { get; set; }

        /// <summary>
        /// Height of the tilemap in tiles (32 or 64).
        /// Determined by ScrFileReader based on block count.
        /// </summary>
        public int HeightTiles { get; set; }

        /// <summary>
        /// Number of 32×32 blocks detected in the SCR file.
        /// Typically 1 or 4 for S‑CAD SCR files.
        /// </summary>
        public int BlockCount { get; set; }

        /// <summary>
        /// Parsed SCR file data (tilemap + blocks + tiles).
        /// </summary>
        public ScrFile Scr { get; set; }

        /// <summary>
        /// True if the SCR file was successfully loaded and parsed.
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Error message if Success == false.
        /// </summary>
        public string ErrorMessage { get; set; } = string.Empty;

        public static ScrFileReadResult Fail(string msg)
            => new ScrFileReadResult { Success = false , ErrorMessage = msg };

        public static ScrFileReadResult Ok(ScrFile scr)
            => new ScrFileReadResult { Success = true , Scr = scr };
    }
}
