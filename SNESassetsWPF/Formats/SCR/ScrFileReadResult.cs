using static SNESassetsWPF.Formats.ColFileReadResult;

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




        public class ScrWarning
        {
            public string Long { get; set; }
            public string Short { get; set; }
        }




        public bool Success { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
        public byte[] RawFile { get; set; } = Array.Empty<byte>();
        public ScrFormatType Format { get; set; }
        public List<ScrWarning> Warnings { get; set; } = new();



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
