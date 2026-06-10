namespace SNESassetsWPF.Formats
{
    /// <summary>
    /// Result wrapper for SCR file loading.
    /// </summary>
    public class ScrFileReadResult
    {
        public enum ScrFormatType
        {
            Strict,
            Partial,
            Unreadable
        }



        public bool Success { get; set; }
        public ScrFormatType Format { get; set; }

        public string ErrorMessage { get; set; } = string.Empty;
        public string WarningMessage { get; set; } = string.Empty;
        public List<string> Warnings { get; set; } = new();


        public byte[] RawFile { get; set; } = Array.Empty<byte>();



        public static ScrFileReadResult Fail(string msg)
        => new ScrFileReadResult
        {
            Success = false ,
            Format = ScrFormatType.Unreadable ,
            ErrorMessage = msg
        };

        public static ScrFileReadResult Ok(byte[] raw)
        => new ScrFileReadResult
        {
            Success = true ,
            RawFile = raw
        };
    }
}
