using System;

namespace SNESassetsWPF.Formats
{
    /// <summary>
    /// Parses an S‑CG‑CAD PNL file.
    ///
    /// Binary layout:
    ///   0x0000–0x00FF : 256‑byte header
    ///   0x0100–0x80FF : 0x8000‑byte attribute table (16384 × 2 bytes)
    ///   0x8100–0x100FF: 0x8000‑byte flag table      (16384 × 2 bytes)
    ///
    /// All 16‑bit values are BIG‑ENDIAN.
    /// Tilemap is always 32×512 tiles.
    /// </summary>
    public static class PnlFileParser
    {
        private const int HeaderSize      = 0x100;
        private const int AttributeOffset = 0x100;
        private const int AttributeSize   = 0x8000;   // 32768 bytes
        private const int FlagOffset      = AttributeOffset + AttributeSize;
        private const int FlagSize        = 0x8000;   // 32768 bytes

        public static PnlFile Parse(byte[] raw)
        {
            if ( raw == null )
                throw new ArgumentNullException( nameof( raw ) );

            if ( raw.Length < HeaderSize + AttributeSize + FlagSize )
                throw new InvalidOperationException( "PNL file is too small or truncated." );

            var pnl = new PnlFile
            {
                RawFile = raw
            };

            // ───────────────────────────────────────────────
            // 1. Copy header
            // ───────────────────────────────────────────────
            Buffer.BlockCopy( raw , 0 , pnl.Header , 0 , HeaderSize );

            // ───────────────────────────────────────────────
            // 2. Parse all 16384 tiles
            // ───────────────────────────────────────────────
            for ( int i = 0 ; i < PnlFile.EntryCount ; i++ )
            {
                var tile = new PnlEntry();

                // -----------------------------
                // Read attribute word (big‑endian)
                // -----------------------------
                int attrPos = AttributeOffset + (i * 2);
                ushort rawAttr = (ushort)((raw[attrPos] << 8) | raw[attrPos + 1]);
                tile.RawAttributeWord = rawAttr;

                // -----------------------------
                // Read flag word (big‑endian)
                // -----------------------------
                int flagPos = FlagOffset + (i * 2);
                ushort rawFlag = (ushort)((raw[flagPos] << 8) | raw[flagPos + 1]);
                tile.RawFlagWord = rawFlag;

                pnl.Entries[i] = tile;
            }

            return pnl;
        }
    }
}
