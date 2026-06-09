using System;

namespace SNESassetsWPF.Models
{
    /// <summary>
    /// Represents an S‑CG‑CAD MAP file exactly as it exists on disk.
    ///
    /// MAP files contain:
    ///   • A 0x100‑byte header (editor metadata only)
    ///   • A flat array of 16‑bit MapCell entries
    ///
    /// MAPs do NOT contain:
    ///   • Width
    ///   • Height
    ///   • Any CGX tile data
    ///   • Any palette data
    ///
    /// Width/Height must be inferred by the parser based on file size.
    ///
    /// Each MapCell references a PNL entry (0–16383).
    /// The PNL entry then references a CGX tile.
    ///
    /// MAP → PNL → CGX is the correct chain.
    /// </summary>
    public class MapFile
    {
        /// <summary>
        /// Raw 0x100‑byte MAP header.
        ///
        /// S‑CG‑CAD stores editor metadata here:
        ///   • Tile width exponent (0x69)
        ///   • Tile height exponent (0x6A)
        ///   • Editor flags
        ///   • Unused bytes
        ///
        /// SNES hardware ignores this entire header.
        /// </summary>
        public byte[] Header { get; set; } = new byte[0x100];

        /// <summary>
        /// Flat array of MapCells.
        ///
        /// Length = Width * Height.
        /// Each MapCell contains:
        ///   • Raw 16‑bit value
        ///   • Extracted PnlIndex (lower 14 bits)
        ///
        /// No other attributes exist in MAP files.
        /// </summary>
        public MapCell[] Cells { get; set; } = Array.Empty<MapCell>();

        /// <summary>
        /// Width of the map in cells.
        /// MAP files do NOT store this.
        /// The parser must infer it from file size.
        /// </summary>
        public int Width { get; set; }

        /// <summary>
        /// Height of the map in cells.
        /// MAP files do NOT store this.
        /// The parser must infer it from file size.
        /// </summary>
        public int Height { get; set; }

        /// <summary>
        /// Meta‑tile width in PNL tiles.
        ///
        /// Defined by S‑CG‑CAD in Header[0x69]:
        ///     metaWidth = 1 << (Header[0x69] & 0x1F)
        ///
        /// This controls how many PNL entries form one MAP cell.
        /// Example:
        ///     exponent = 1 → metaWidth = 2
        ///     exponent = 2 → metaWidth = 4
        /// </summary>
        public int MetaWidth => 1 << ( Header[0x69] & 0x1F );

        /// <summary>
        /// Meta‑tile height in PNL tiles.
        ///
        /// Defined by S‑CG‑CAD in Header[0x6A]:
        ///     metaHeight = 1 << (Header[0x6A] & 0x1F)
        ///
        /// This controls how many PNL entries form one MAP cell.
        /// </summary>
        public int MetaHeight => 1 << ( Header[0x6A] & 0x1F );

        /// <summary>
        /// Returns the MapCell at (x,y).
        /// </summary>
        public MapCell GetCell(int x , int y)
        {
            int index = (y * Width) + x;
            return Cells[index];
        }
    }
}
