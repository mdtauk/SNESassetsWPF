using System;
using System.Collections.Generic;

namespace SNESassetsWPF.Formats
{
    /// <summary>
    /// Result wrapper for CGX file loading and structural parsing.
    /// Pure format representation stays in CgxFile.
    /// </summary>
    public class CgxFileReadResult
    {
        public enum CgxFormatType
        {
            Valid,
            Warn,
            Fail
        }

        public class CgxWarning
        {
            public string Long { get; set; } = string.Empty;
            public string Short { get; set; } = string.Empty;
        }

        public bool Success { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;

        /// <summary>
        /// Full raw file bytes (for debugging).
        /// </summary>
        public byte[] RawFile { get; set; } = Array.Empty<byte>();

        /// <summary>
        /// High‑level classification of the CGX structure.
        /// </summary>
        public CgxFormatType Format { get; set; } = CgxFormatType.Valid;

        /// <summary>
        /// Structural warnings (missing/incomplete metadata, truncated tiles, etc.).
        /// </summary>
        public List<CgxWarning> Warnings { get; set; } = new();

        // Split regions (may be partial)
        public byte[] RawTileData { get; set; } = Array.Empty<byte>();
        public byte[] RawPrefixTable { get; set; } = Array.Empty<byte>();
        public byte[] RawMetadata { get; set; } = Array.Empty<byte>();

        /// <summary>
        /// Parsed CGX file (pure format object).
        /// May be null if parsing failed.
        /// </summary>
        public CgxFile? Parsed { get; set; }

        public static CgxFileReadResult Fail(string msg)
            => new CgxFileReadResult { Success = false , ErrorMessage = msg , Format = CgxFormatType.Fail };

        public static CgxFileReadResult Ok(byte[] raw)
            => new CgxFileReadResult { Success = true , RawFile = raw };
    }
}
