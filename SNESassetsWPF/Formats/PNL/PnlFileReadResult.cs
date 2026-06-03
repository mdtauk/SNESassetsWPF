namespace SNESassetsWPF.Formats
{
    /// <summary>
    /// Standard result wrapper for loading a PNL file.
    /// Contains success state, error message, and the parsed PnlFile.
    /// </summary>
    public class PnlFileReadResult
    {
        /// <summary>
        /// True if the PNL file was successfully parsed.
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Error message if Success == false.
        /// </summary>
        public string ErrorMessage { get; set; }

        /// <summary>
        /// Parsed PNL file data, including header, tile tables,
        /// meta‑tile size, and optional extracted patterns.
        /// </summary>
        public PnlFile Pnl { get; set; }

        /// <summary>
        /// Creates a failure result with the given error message.
        /// </summary>
        public static PnlFileReadResult Fail(string message)
            => new PnlFileReadResult { Success = false , ErrorMessage = message };

        /// <summary>
        /// Creates a success result with the parsed PnlFile.
        /// </summary>
        public static PnlFileReadResult Ok(PnlFile pnl)
            => new PnlFileReadResult { Success = true , Pnl = pnl };
    }
}
