using System;

namespace SNESassetsWPF.Models
{
    /// <summary>
    /// Represents a single SNES BGR555 colour stored as two bytes
    /// exactly as they appear in the COL file.
    /// </summary>
    public struct SnesColor
    {
        /// <summary>
        /// Low byte from the COL file.
        /// </summary>
        public byte Low { get; }

        /// <summary>
        /// High byte from the COL file.
        /// </summary>
        public byte High { get; }

        /// <summary>
        /// Combined 16-bit value (High:Low).
        /// </summary>
        public ushort Value => (ushort)( Low | ( High << 8 ) );

        public SnesColor(byte low , byte high)
        {
            Low = low;
            High = high;
        }

        /// <summary>
        /// Returns the raw bytes as a hex pair, e.g. "1F 03".
        /// </summary>
        public string ToHexPair() => $"{Low:X2} {High:X2}";

        /// <summary>
        /// True if the raw SNES colour is valid (bit 15 must be 0).
        /// </summary>
        public bool IsValidRaw => ( Value & 0x8000 ) == 0;
    }
}
