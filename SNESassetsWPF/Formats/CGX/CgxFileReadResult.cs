namespace SNESassetsWPF.Formats
{
    /// <summary>
    /// Result wrapper for CGX file loading.
    /// </summary>
    public class CgxFileReadResult
    {
        public bool Success { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
        public byte[] RawFile { get; set; } = Array.Empty<byte>();

        public static CgxFileReadResult Fail(string msg)
            => new CgxFileReadResult { Success = false , ErrorMessage = msg };

        public static CgxFileReadResult Ok(byte[] raw)
            => new CgxFileReadResult { Success = true , RawFile = raw };
    }
}
