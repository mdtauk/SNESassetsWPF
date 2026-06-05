namespace SNESassetsWPF.Formats
{
    /// <summary>
    /// Result of reading a PNL file from disk.
    /// Contains the raw bytes and any error information.
    /// </summary>
    public class PnlFileReadResult
    {
        /// <summary>
        /// True if the file was successfully read.
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Raw PNL file bytes (header + tile table + flag table).
        /// </summary>
        public byte[] Data { get; set; }

        /// <summary>
        /// Error message if reading failed.
        /// </summary>
        public string ErrorMessage { get; set; }
    }
}
