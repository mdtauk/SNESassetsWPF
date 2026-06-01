namespace SNESassetsWPF.Formats
{
    /// <summary>
    /// Standard result wrapper for SCR file loading.
    /// Matches the pattern used by CGX and COL.
    /// </summary>
    public class ScrFileReadResult
    {
        /// <summary>
        /// True if the SCR file was successfully loaded and parsed.
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Error message if Success == false.
        /// </summary>
        public string ErrorMessage { get; set; }

        /// <summary>
        /// Parsed SCR file data.
        /// </summary>
        public ScrFile Scr { get; set; }

        public static ScrFileReadResult Fail(string msg)
            => new ScrFileReadResult { Success = false , ErrorMessage = msg };

        public static ScrFileReadResult Ok(ScrFile scr)
            => new ScrFileReadResult { Success = true , Scr = scr };
    }
}
