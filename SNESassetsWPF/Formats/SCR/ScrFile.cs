namespace SNESassetsWPF.Models
{
    /// <summary>
    /// Represents an S‑CG‑CAD / H‑CG‑CAD SCR file.
    ///
    /// SCR files are *editor‑side* SNES tilemap containers used by S‑CG‑CAD.
    /// They store 1, 2, or 4 blocks of SNES tilemap data.
    ///
    /// A single SCR block is always:
    ///     • 32 × 32 tilemap entries
    ///     • 1024 entries per block
    ///     • each entry = 16‑bit SNES tilemap word
    ///     • block size = 2048 bytes
    ///
    /// Valid SCR tilemap sizes (after trimming editor metadata):
    ///     0x0800 = 1 block  (32×32)
    ///     0x1000 = 2 blocks (32×64 or 64×32)
    ///     0x2000 = 4 blocks (64×64)
    ///
    /// Notes:
    ///   • S‑CG‑CAD often appends editor metadata after the tilemap.
    ///     H‑CG‑CAD strips this footer and loads only the tilemap portion.
    ///
    ///   • SCR files do NOT contain CGX tile graphics.
    ///     TileIndex refers to a PNL entry, not VRAM.
    ///
    ///   • SCR files do NOT contain width/height metadata.
    ///     BlockCount determines the final dimensions.
    /// </summary>
    public class ScrFile
    {
        /// <summary>
        /// Raw SCR tilemap bytes (footer removed if present).
        /// </summary>
        public byte[] RawFile { get; set; } = Array.Empty<byte>();

        /// <summary>
        /// Number of 32×32 blocks in the tilemap.
        /// Valid values: 1, 2, or 4.
        /// </summary>
        public int BlockCount { get; set; }

        /// <summary>
        /// Parsed 32×32 blocks.
        /// Each block contains exactly 1024 ScrEntry objects.
        /// </summary>
        public ScrBlock[] Blocks { get; set; } = Array.Empty<ScrBlock>();
    }
}
