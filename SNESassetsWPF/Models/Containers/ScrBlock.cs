namespace SNESassetsWPF.Models
{
    /// <summary>
    /// One 32×32 SNES tilemap block from an SCR file.
    ///
    /// S‑CG‑CAD stores tilemaps in fixed 32×32 blocks.
    /// Larger maps (32×64, 64×32, 64×64) are composed of 2 or 4 blocks.
    ///
    /// Block layout is always:
    ///     row-major order
    ///     index = y * 32 + x
    ///
    /// Each entry is a 16‑bit SNES tilemap word (ScrEntry).
    /// </summary>
    public class ScrBlock
    {
        /// <summary>
        /// 1024 tilemap entries (32×32).
        /// </summary>
        public ScrEntry[] Entries { get; set; } = Array.Empty<ScrEntry>();

        /// <summary>
        /// 1024 visibility bits (1 per tile), decoded from SCR footer.
        /// </summary>
        public bool[] VisibilityMask { get; set; } = Array.Empty<bool>();

        /// <summary>
        /// Returns the tilemap entry at (x, y) within this 32×32 block.
        /// </summary>
        public ScrEntry GetEntry(int x , int y)
        {
            return Entries[y * 32 + x];
        }
    }
}
