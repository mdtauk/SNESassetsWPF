using System;

namespace SNESassetsWPF.Formats
{
    /// <summary>
    /// Result wrapper for MAP file loading.
    /// Contains:
    ///   • Success flag
    ///   • Error message (if any)
    ///   • Raw MAP bytes (0x100 header + N*2 cell data)
    ///
    /// Parsing is performed separately by MapParser.
    /// </summary>
    public class MapFileReadResult
    {
        /// <summary>
        /// True if the MAP file was successfully read from disk.
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Error message if Success == false.
        /// </summary>
        public string ErrorMessage { get; set; } = string.Empty;

        /// <summary>
        /// Raw MAP file bytes as read from disk.
        /// Used for round‑tripping and parsing.
        /// </summary>
        public byte[] RawFile { get; set; } = Array.Empty<byte>();

        /// <summary>
        /// Convenience helper for failure cases.
        /// </summary>
        public static MapFileReadResult Fail(string msg)
            => new MapFileReadResult { Success = false , ErrorMessage = msg };

        /// <summary>
        /// Convenience helper for success cases.
        /// </summary>
        public static MapFileReadResult Ok(byte[] raw)
            => new MapFileReadResult { Success = true , RawFile = raw };
    }
}
