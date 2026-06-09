namespace SNESassetsWPF.Formats
{
    /// <summary>
    /// Result wrapper for SCR file loading.
    /// </summary>
    public class ScrFileReadResult
    {
        public bool Success { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
        public byte[] RawFile { get; set; } = Array.Empty<byte>();

        public static ScrFileReadResult Fail(string msg)
            => new ScrFileReadResult { Success = false , ErrorMessage = msg };

        public static ScrFileReadResult Ok(byte[] raw)
            => new ScrFileReadResult { Success = true , RawFile = raw };
    }
}
