using System;

namespace SNESassetsWPF.Formats
{
    /// <summary>
    /// Result wrapper for MAP file loading.
    /// Contains raw bytes, parsed MapFile, and error state.
    /// </summary>
    public class MapFileReadResult
    {
        /// <summary>
        /// Raw MAP file bytes as read from disk.
        /// Useful for debugging and round‑tripping.
        /// </summary>
        public byte[] RawFile { get; set; } = Array.Empty<byte>();

        /// <summary>
        /// Parsed MAP file (tile grid + header info).
        /// </summary>
        public MapFile Map { get; set; }

        /// <summary>
        /// True if the MAP file was successfully parsed.
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Error message if Success == false.
        /// </summary>
        public string ErrorMessage { get; set; } = string.Empty;

        /// <summary>
        /// Convenience helper for failure cases.
        /// </summary>
        public static MapFileReadResult Fail(string msg)
            => new MapFileReadResult { Success = false , ErrorMessage = msg };

        /// <summary>
        /// Convenience helper for success cases.
        /// </summary>
        public static MapFileReadResult Ok(MapFile map , byte[] raw)
            => new MapFileReadResult { Success = true , Map = map , RawFile = raw };
    }
}
