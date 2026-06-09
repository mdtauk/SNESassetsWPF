public class ColFileReadResult
{
    public bool Success { get; set; }
    public string ErrorMessage { get; set; } = string.Empty;

    // Add these back:
    public byte[] RawColorData { get; set; } = Array.Empty<byte>();
    public byte[] RawMetadata { get; set; } = Array.Empty<byte>();

    public byte[] RawFile { get; set; } = Array.Empty<byte>();

    public static ColFileReadResult Fail(string msg)
        => new ColFileReadResult { Success = false , ErrorMessage = msg };

    public static ColFileReadResult Ok(byte[] raw)
        => new ColFileReadResult { Success = true , RawFile = raw };
}
