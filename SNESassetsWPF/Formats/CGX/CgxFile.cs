using SNESassetsWPF.Models;

namespace SNESassetsWPF.Formats
{
    /// <summary>
    /// Represents an S‑CG‑CAD CGX file.
    /// A CGX file contains ONLY tile graphics:
    ///  • raw bitplane data
    ///  • optional prefix table (editor‑side)
    ///  • optional metadata block (ASCII)
    ///
    /// It does NOT contain:
    ///  • flips
    ///  • palette row
    ///  • priority
    ///  • visibility
    ///
    /// H‑CG‑CAD reads CGX exactly this way.
    /// </summary>
    public class CgxFile
    {
        /// <summary>
        /// Full raw file bytes (for debugging).
        /// </summary>
        public byte[] RawFile { get; set; } = Array.Empty<byte>();

        /// <summary>
        /// Bit depth of the tile data.
        /// CGX supports 2bpp, 4bpp, or 8bpp.
        /// Determines bytes per tile:
        ///  • 2bpp = 16 bytes
        ///  • 4bpp = 32 bytes
        ///  • 8bpp = 64 bytes
        /// </summary>
        public int BitDepth { get; set; }

        /// <summary>
        /// Number of tiles in the CGX file.
        /// H‑CG‑CAD assumes 1024 tiles for full sheets.
        /// </summary>
        public int TileCount { get; set; }

        /// <summary>
        /// Bytes per tile (derived from BitDepth).
        /// </summary>
        public int BytesPerTile { get; set; }

        /// <summary>
        /// Rendering layout ONLY.
        /// CGX files do NOT store tile sheet width/height.
        /// H‑CG‑CAD chooses this dynamically.
        /// </summary>
        public int TilesX { get; set; }
        public int TilesY { get; set; }

        /// <summary>
        /// Optional trailing ASCII metadata block.
        /// Present in S‑CG‑CAD output.
        /// Ignored by SNES hardware.
        /// </summary>
        public byte[] Metadata { get; set; } = Array.Empty<byte>();

        /// <summary>
        /// Optional prefix table.
        /// Size varies (commonly 0x400 bytes for 1024 tiles).
        /// Editor‑side only; SNES does not use this.
        /// </summary>
        public byte[] TilePrefixTable { get; set; } = Array.Empty<byte>();

        /// <summary>
        /// Raw bitplane tile data.
        /// Starts at offset 0 of the CGX file.
        /// </summary>
        public byte[] RawTileData { get; set; } = Array.Empty<byte>();

        /// <summary>
        /// Decoded tiles.
        /// Each tile is 8×8 pixel indices (0–255).
        /// H‑CG‑CAD decodes tiles exactly this way.
        /// </summary>
        public CgxTile[] Tiles { get; set; } = Array.Empty<CgxTile>();

        /// <summary>
        /// Get decoded tile by index.
        /// </summary>
        public CgxTile GetTile(int index) => Tiles[index];

        /// <summary>
        /// Get raw bitplane bytes for a tile.
        /// Useful for debugging and verification.
        /// </summary>
        public byte[] GetRawTileBytes(int index)
        {
            int offset = index * BytesPerTile;
            return RawTileData.AsSpan( offset , BytesPerTile ).ToArray();
        }

        /// <summary>
        /// Convert tile index to sheet position.
        /// Rendering helper only.
        /// </summary>
        public (int X , int Y) GetTileSheetPosition(int index)
        {
            int x = index % TilesX;
            int y = index / TilesX;
            return (x , y);
        }
    }
}
