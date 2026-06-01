using SNESassetsWPF.Models;

namespace SNESassetsWPF.Formats
{
    public class ColFileReadResult
    {
        public byte[] RawColorData { get; set; } = Array.Empty<byte>();
        public byte[] RawMetadata { get; set; } = Array.Empty<byte>();

        public bool IsValid { get; set; }
        public string ErrorMessage { get; set; } = "";
    }

}
