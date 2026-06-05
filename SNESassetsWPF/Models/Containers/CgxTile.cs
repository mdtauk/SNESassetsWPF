namespace SNESassetsWPF.Models
{
    /// <summary>
    /// Represents a single CGX tile (8×8 pixels).
    /// Contains raw bytes, decoded pixels, and optional prefix attributes.
    /// </summary>
    public class CgxTile
    {
        /// <summary>
        /// Index of this tile in the CGX file (0–1023).
        /// </summary>
        public int TileIndex { get; set; }

        /// <summary>
        /// Bit depth of this tile (2, 4, or 8 bpp).
        /// </summary>
        public int BitDepth { get; set; }

        /// <summary>
        /// Raw tile bytes (16, 32, or 64 bytes depending on bit depth).
        /// Essential for debugging and verification.
        /// </summary>
        public byte[] RawBytes { get; set; } = Array.Empty<byte>();

        /// <summary>
        /// Fully decoded 8×8 pixel indices.
        /// </summary>
        public byte[,] Pixels { get; set; } = new byte[8 , 8];

        /// <summary>
        /// Returns a byte index
        /// </summary>
        /// <param name="y"></param>
        /// <param name="x"></param>
        /// <returns></returns>
        public byte this[int y , int x]
        {
            get => Pixels[y , x];
            set => Pixels[y , x] = value;
        }


        /// <summary>
        /// Editor-side palette group from prefix table (0–15).
        /// </summary>
        public int PaletteGroup { get; set; }

        /// <summary>
        /// Editor-side horizontal flip flag (if prefix encodes it).
        /// </summary>
        public bool FlipX { get; set; }

        /// <summary>
        /// Editor-side vertical flip flag (if prefix encodes it).
        /// </summary>
        public bool FlipY { get; set; }

        /// <summary>
        /// Editor-side priority flag (if prefix encodes it).
        /// </summary>
        public int Priority { get; set; }


        /// <summary>
        /// Pixel accessor for renderers.
        /// Returns the decoded palette index at (x,y).
        /// </summary>
        public byte GetPixel(int x , int y)
        {
            // Optional safety bounds
            if ( x < 0 || x >= 8 || y < 0 || y >= 8 )
                return 0;

            return Pixels[y , x];
        }

    }
}
