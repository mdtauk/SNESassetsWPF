namespace SNESassetsWPF.Formats
{
    /// <summary>
    /// Result wrapper for PNL file loading.
    /// </summary>
    public class PnlFileReadResult
    {
        public bool Success { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
        public byte[] RawFile { get; set; } = Array.Empty<byte>();

        public static PnlFileReadResult Fail(string msg)
            => new PnlFileReadResult { Success = false , ErrorMessage = msg };

        public static PnlFileReadResult Ok(byte[] raw)
            => new PnlFileReadResult { Success = true , RawFile = raw };
    }
}
