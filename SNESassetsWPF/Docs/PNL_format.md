# PNL format — panel / tile selection bank (SNES / S-CG-CAD)

This document describes the PNL (panel) file format used in S-CG-CAD for managing
a large tile selection surface. Non-expert programmers can use this to understand
the on-disk layout and implement a basic parser or viewer.

## Summary

- **Purpose**: A large 32×512 tile grid used as a source for stamping tiles into maps and screens
- **File size**: Typically 0x10100 bytes (65,792 bytes)
- **Structure**: 0x100 header + two 0x4000-word tables
- **Endianness**: 16-bit words are stored big-endian on disk
- **Content**: tile IDs + attributes (flip, priority, palette row) per tile, plus a "present" flag table

## File layout

| Offset | Size | Meaning |
| ---: | ---: | --- |
| 0x0000 - 0x00FF | 0x100 | editor header (CAD metadata) |
| 0x0100 - 0x80FF | 0x8000 | tile table (0x4000 words, 16-bit big-endian) |
| 0x8100 - 0x100FF | 0x8000 | flag table (0x4000 words, 16-bit big-endian) |

ASCII overview:

```
PNL file (0x10100 bytes):

+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+
| 0x00       CAD signature and header          | 0xFF
+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+
|       Tile table: 32 tiles/row × 512 rows    | 0x80FF
| (16-bit big-endian words, 0x8000 bytes)      |
+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+
| Flag table: matching size, "present" bits    | 0x100FF
| (16-bit big-endian words, 0x8000 bytes)      |
+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+
```

## Editor header (0x100 bytes)

The first 0x100 bytes are CAD-side metadata. Important offsets:

| Offset | Size | Meaning | Notes |
| ---: | ---: | --- | --- |
| 0x00 - 0x1F | 0x20 | ASCII provenance string | Often begins with `NAK1989 S-CG-CADVer...` |
| 0x60 | 1 | colormap mode | `plbank`: controls palette preview upload path (32 / 128 / 256 colors) |
| 0x61 | 1 | Mode 7 enable flag | 0/1 toggle for Mode 7 UI label |
| 0x62 | 1 | panel graphics mode | Small mode selector; S-CG-CAD uses `header[0x62] & 0x02` |
| 0x63 | 1 | referenced CGX bank | `header[0x63] & 0x03` — which CGX bank to sample tile graphics from |
| 0x64 | 1 | panel colormap bank | `header[0x64] & 0x03` — color bank selector for preview |
| 0x65 | 1 | colormap selector A | Per-bank preview colormap selector |
| 0x66 | 1 | colormap selector B | Secondary selector (used in some modes) |
| 0x67 - 0x68 | 2 | base tile index | 16-bit value, displayed as 3-nibble hex in CAD UI |
| 0x69 | 1 | panel tile width exponent | Shift factor: `metatile_width = 1 << (header[0x69] & 0x1F)` |
| 0x6A | 1 | panel tile height exponent | Shift factor: `metatile_height = 1 << (header[0x6A] & 0x1F)` |

When metatile exponents are > 0, one logical tile selection is treated as a block
of multiple 8×8 tiles (e.g., 1 << 1 = 2 means 16×16 in the UI but still stored as
8×8).

## Tile table (0x4000 words)

The tile table is 0x8000 bytes = 0x4000 16-bit words stored as big-endian.

Each word encodes one tile entry:

| Bits | Mask | Meaning |
| ---: | ---: | --- |
| 15 | 0x8000 | vertical flip (vflip) |
| 14 | 0x4000 | horizontal flip (hflip) |
| 13 | 0x2000 | priority (pri) |
| 12-10 | 0x1C00 | palette row (pal, 0-7) |
| 9-0 | 0x03FF | tile ID (0-1023) |

Decode example (C#-like pseudocode):

```csharp
ushort word = ReadBigEndianWord(offset);
bool vflip = (word >> 15) & 1 == 1;
bool hflip = (word >> 14) & 1 == 1;
bool priority = (word >> 13) & 1 == 1;
int palRow = (word >> 10) & 0x07;
int tileId = word & 0x03FF;
```

The tile grid is logically 32 tiles per row, 512 rows deep:

```csharp
int panelIndex = panelY * 32 + panelX;  // panelX: 0-31, panelY: 0-511
ushort word = tileTable[panelIndex];
```

## Flag table (0x4000 words)

The flag table is also 0x8000 bytes of 16-bit big-endian words. In S-CG-CAD's
parsed code paths, only bit 15 is consumed:

```csharp
ushort word2 = ReadBigEndianWord(offset + 0x8000);  // second table
bool isPresent = (word2 >> 15) & 1 == 1;
```

When `isPresent` is 0, the tile slot is treated as empty/inactive. The lower
15 bits are preserved but not clearly interpreted by S-CG-CAD.

## Practical viewer usage

To display or export a PNL:

1. Read the 0x100 header and extract useful metadata (CGX bank, colormap settings).
2. Read the 0x4000 tile words as big-endian 16-bit values.
3. Read the matching 0x4000 flag words; extract the "present" bit for each.
4. For each tile in the grid:
   - Check if `isPresent`. If not, skip or show as empty.
   - Decode the tile word to get flip, priority, palette row, and tile ID.
   - Look up the tile ID in the referenced CGX bank.
   - Render the tile with the specified palette row from the current COL.
   - Apply flip/priority as needed.

Complete PNL decoder pseudocode (C#-like):

```csharp
public void LoadPNL(string filePath, CGXBank cgx, Palette[] colorBanks) {
  byte[] buffer = File.ReadAllBytes(filePath);

  // Read header
  int cgxBank = buffer[0x63] & 0x03;
  int colormapBank = buffer[0x64] & 0x03;

  // Read tile table (0x4000 words at 0x0100)
  for (int y = 0; y < 512; y++) {
    for (int x = 0; x < 32; x++) {
      int index = y * 32 + x;
      int offset = 0x0100 + (index * 2);
      ushort tileWord = ReadBigEndian16(buffer, offset);

      bool vflip = (tileWord >> 15) & 1 == 1;
      bool hflip = (tileWord >> 14) & 1 == 1;
      bool priority = (tileWord >> 13) & 1 == 1;
      int palRow = (tileWord >> 10) & 0x07;
      int tileId = tileWord & 0x03FF;

      // Read flag
      ushort flagWord = ReadBigEndian16(buffer, 0x8100 + (index * 2));
      bool isPresent = (flagWord >> 15) & 1 == 1;

      if (isPresent) {
        // Render tile tileId from cgx[cgxBank] using palette row palRow
        // Apply flips and priority
      }
    }
  }
}
```

ASCII pixel grid example (2×2 tile selection):

```
+-------+-------+
| tileA | tileB |
| palA  | palB  |
| hflip |       |
+-------+-------+
| tileC | tileD |
| palC  | palD  |
+-------+-------+
```

## File validation and edge cases

- **Header preservation**: Always preserve the full 0x100 header when round-tripping; other CAD builds may use bytes we haven't decoded yet.
- **Metatile grouping**: The full panel is 32×512 = 16,384 tiles, but the UI may group them into "metatiles" based on the width/height exponent (e.g., 2×2 = 4 tiles per metatile).
- **Tile ID references**: Tile IDs reference the CGX bank specified in header 0x63 (0–3).
- **Truncated files**: If file size < 0x10100, treat missing tiles as `isPresent = false`.
- **Workstation artifact**: The PNL is a workstation-side tool file, not a SNES ROM asset; use for editor preview only.

## Notes and edge cases

- The full panel is 32×512 = 16,384 tiles, but the UI may group them into
  "metatiles" based on the width/height exponent.
- Tile IDs reference the referenced CGX bank (set in header 0x63).
- Always preserve the full 0x100 header when round-tripping; other CAD builds
  may use bytes we haven't decoded yet.
- The panel is a workstation-side tool artifact, not a SNES ROM asset.

---

Document prepared to aid implementing a PNL viewer in this project.
