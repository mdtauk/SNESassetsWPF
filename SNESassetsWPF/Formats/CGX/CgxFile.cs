using SNESassetsWPF.Models;
using System;

namespace SNESassetsWPF.Formats
{
    /// <summary>
    /// Represents an S‑CG‑CAD CGX file (designer‑side intermediate format).
    /// Contains raw tile data, prefix table, metadata, and decoded tiles.
    /// </summary>
    public class CgxFile
    {
        /// <summary>
        /// Full raw CGX file bytes (for debugging and verification).
        /// </summary>
        public byte[] RawFile { get; set; } = Array.Empty<byte>();

        /// <summary>
        /// Bit depth of the tile data (2, 4, or 8 bpp).
        /// </summary>
        public int BitDepth { get; set; }

        /// <summary>
        /// Number of tiles in RawTileData.
        /// </summary>
        public int TileCount { get; set; }

        /// <summary>
        /// Bytes per tile (16, 32, or 64 depending on BitDepth).
        /// </summary>
        public int BytesPerTile { get; set; }

        /// <summary>
        /// Layout for rendering ONLY.
        /// CGX does NOT store this — it must be chosen externally.
        /// </summary>
        public int TilesX { get; set; }

        /// <summary>
        /// Layout for rendering ONLY.
        /// CGX does NOT store this — it must be chosen externally.
        /// </summary>
        public int TilesY { get; set; }

        /// <summary>
        /// Trailing S‑CG‑CAD metadata block (ASCII text).
        /// Not used by SNES; editor‑side only.
        /// </summary>
        public byte[] Metadata { get; set; } = Array.Empty<byte>();

        /// <summary>
        /// Optional prefix table (palette group / tile attributes).
        /// Length varies (commonly 0x400 for 1024 tiles).
        /// </summary>
        public byte[] TilePrefixTable { get; set; } = Array.Empty<byte>();

        /// <summary>
        /// Raw tile data starting at byte 0 of the CGX file.
        /// </summary>
        public byte[] RawTileData { get; set; } = Array.Empty<byte>();

        /// <summary>
        /// Decoded tiles (8×8 pixel indices + editor‑side attributes).
        /// </summary>
        public CgxTile[] Tiles { get; set; } = Array.Empty<CgxTile>();

        // ------------------------------------------------------------
        // Tile inspection helpers
        // ------------------------------------------------------------

        public CgxTile GetTile(int index) => Tiles[index];

        public byte[] GetRawTileBytes(int index)
        {
            int offset = index * BytesPerTile;
            return RawTileData.AsSpan( offset , BytesPerTile ).ToArray();
        }

        public (int X , int Y) GetTileSheetPosition(int index)
        {
            int x = index % TilesX;
            int y = index / TilesX;
            return (x , y);
        }
    }
}
