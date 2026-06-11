namespace SNESassetsWPF.Formats
{
    /// <summary>
    /// Result wrapper for COL file loading.
    /// </summary>
    public class ColFileReadResult
    {
        public enum ColFormatType
        {
            Valid,
            Warn,
            Fail
        }




        public class ColWarning
        {
            public string Long { get; set; }
            public string Short { get; set; }
        }




        public bool Success { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
        public byte[] RawFile { get; set; } = Array.Empty<byte>();
        public ColFormatType Format { get; set; } = ColFormatType.Valid;
        public List<ColWarning> Warnings { get; set; } = new();




        // Palette region (0x000–0x1FF) — may be partial
        public byte[] RawColorData { get; set; } = Array.Empty<byte>();

        // Footer/metadata region (0x200+) — may be partial
        public byte[] RawMetadata { get; set; } = Array.Empty<byte>();

        public static ColFileReadResult Fail(string msg)
            => new ColFileReadResult { Success = false , ErrorMessage = msg };

        public static ColFileReadResult Ok(byte[] raw)
            => new ColFileReadResult { Success = true , RawFile = raw };
    }
}
