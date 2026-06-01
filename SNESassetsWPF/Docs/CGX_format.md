# CGX format — tile graphics (SNES / S-CG-CAD)
### Based on RetroReversing’s S‑CG‑CAD documentation

This document describes the CGX file family as used by SNES development tools
and the S-CG-CAD editor workflow. It focuses on the on-disk layout that this
viewer expects and practical decoding rules for rendering tiles with a COL
palette. The aim is to explain the raw bytes so a non-expert programmer can
implement a decoder.

**Summary**
- Tile cell shape: 8×8 pixels
- Common bit depths: 2bpp, 4bpp, sometimes 8bpp
- Encoding: SNES planar bitplanes (little-endian within rows)
- S-CG-CAD uses a few fixed-size variants that append a small header and
  per-tile metadata after the raw tile data; those are documented below.

**Terminology**
- tile: an 8×8 pixel cell
- plane: one bitplane in SNES planar format (planes are combined into pixel indices)
- tile_table / per-tile table: editor metadata that affects how S-CG-CAD maps
  tile-level palette selection into an 8-bit preview index

  ---

## Basic SNES planar layout

Each tile is stored as a contiguous block of bytes. The number of bytes per
tile depends on the bit depth:

| Bit depth | Bytes per tile | Notes |
| ---: | ---: | --- |
| 2bpp | 16 | 2 bytes per row × 8 rows (planes 0..1) |
| 4bpp | 32 | 16 bytes (planes 0–1) + 16 bytes (planes 2–3) |
| 8bpp | 64 | 8 planes (0–7) stored in 2-byte-per-row groups |

4bpp tile organization (per tile)

| Offset | Size | Meaning |
| ---: | ---: | --- |
| 0x00 - 0x0F | 16 | bitplanes 0 and 1 (two bytes per row for rows 0..7) |
| 0x10 - 0x1F | 16 | bitplanes 2 and 3 (two bytes per row for rows 0..7) |


### ASCII diagram (4bpp tile, bytes shown grouped by row):

```
Tile (32 bytes total):

 bytes 0..1   : row0 plane0 (b0)  | row0 plane1 (b1)
 bytes 2..3   : row1 plane0        | row1 plane1
  ...
 bytes 14..15 : row7 plane0        | row7 plane1

 bytes 16..17 : row0 plane2        | row0 plane3
 bytes 18..19 : row1 plane2        | row1 plane3
  ...
 bytes 30..31 : row7 plane2        | row7 plane3
```

### Decoding a single pixel (row y, column x):

1. bit = 7 - x
2. read bit'th bit from the two bytes in the first block (planes 0 & 1)
3. read bit'th bit from the corresponding two bytes in the second block (planes 2 & 3)
4. assemble: index = (plane3 << 3) | (plane2 << 2) | (plane1 << 1) | (plane0)

Put another way, each plane contributes one bit to the final pixel index.

Simple pseudocode (row decoding):

```
for x in 0..7:
    bit = 7 - x
    p0 = (rowBytesPlane0[y] >> bit) & 1
    p1 = (rowBytesPlane1[y] >> bit) & 1
    p2 = (rowBytesPlane2[y] >> bit) & 1
    p3 = (rowBytesPlane3[y] >> bit) & 1
    index = (p3 << 3) | (p2 << 2) | (p1 << 1) | p0
```

The index is then used to look up a colour in the loaded COL palette (or in the
per-tile colormap when S-CG-CAD metadata applies).

---

## S-CG-CAD container variants

S-CG-CAD commonly stores a raw record region (tile data) followed by a 0x100
editor header and sometimes a per-tile table. The table below summarises the
common observed variants:

| File size | record region | bit depth | tiles | extra | notes |
| ---: | ---: | ---: | ---: | ---: | --- |
| 0x4500 | 0x4000 | 2bpp | 1024 | 0x100 header + 0x400 table | final index = (table[t] << 2) | pixel_2bpp |
| 0x8500 | 0x8000 | 4bpp | 1024 | 0x100 header + 0x400 table | final index = (table[t] << 4) | pixel_4bpp |
| 0x10100 | 0x10000 | 8bpp | 1024 | 0x100 header | no per-tile table |

Important: in these variants the first `record region` bytes are standard
planar tile data and should be decoded the same way as raw CGX tiles.

Per-tile table (visual explanation)

The per-tile table contains one byte per tile. Conceptually it acts like a
prefix that selects which palette group a tile should use in the editor
preview. For example:

```
if file is 4bpp and table present:
    final_index = (table[tile] << 4) | pixel_4bpp_index
```

So table[tile] provides the high nibble of the final 8-bit index, and the tile
planes provide the low nibble.

### Per-tile table semantics (editor preview)

- The per-tile table contains one byte per tile (1024 entries for the common
  bank sizes). It acts like a palette-group prefix in the editor’s preview
  pipeline; the low-bit pixel value from the tile planes is OR'ed with the
  per-tile prefix shifted into the high bits of the final 8-bit index.
- For 2bpp files the prefix is shifted left 2 bits and produces indices 0..255
  after combination; for 4bpp it is shifted left 4 bits.
- Some variants store a small bank-wide constant inside the header which is
  combined with parts of the per-tile table to yield the final prefix.

### Common header bytes (S-CG-CAD)

Observed header bytes inside the 0x100 editor header (offsets relative to the
two-byte header start):

- 0x20: `cgbank` — palette mode selector (affects how indices are mapped in the
  editor preview and how COL palettes are interpreted)
- 0x21: colormap bank index — a small selector used in preview colormap
- 0x22: BG/OBJ palette toggle — affects preview configuration
- 0x23: cell index / small selector — used by some preview paths and by the
  2bpp per-bank prefix calculation

These header bytes are tool metadata. If you want round-trip fidelity with
S-CG-CAD, preserve the entire 0x100 header exactly.

### Decoding strategy for this viewer

- Detect file size. If size matches one of the known S-CG-CAD container
  variants, use the known layout (record region, header, optional tile table).
- Otherwise treat the file as raw concatenated tiles with implicit bytes-per-tile
  determined from the user / heuristics (commonly 4bpp in S-CG-CAD assets).
- Decode each tile using planar rules and produce an array of palette indices
  per pixel.
- When a per-tile table is present, combine the per-tile prefix with the
  tile-local pixel index to form final indices. Apply the currently selected
  COL palette (or the editor bank mapping) to convert indices into Colors.

**Notes and edge cases**

- Some CGX containers are legacy or project-specific. Do not assume a fixed
  tile count; compute it from the record region: tiles = record_region / bytes_per_tile.
- When rebuilding images for preview, allow the user to choose the bit depth
  (2/4/8 bpp) and the COL palette to apply. The S-CG-CAD header contains hints
  (cgbank, colormap bank) but not definitive runtime mappings for a ROM.
- Preserve extra header and per-tile table bytes when rewriting files to
  maintain compatibility with the original editor’s metadata.

**References**
- Practical reverse-engineering of SNES CAD assets and observed S-CG-CAD
  container shapes (internal lab notes and news-archive traces).

## File detection and variant selection

### Detecting S-CG-CAD container variants by file size

```csharp
public enum CGXVariant {
  Raw2bpp,       // Raw tile data (multiple of 16 bytes)
  Raw4bpp,       // Raw tile data (multiple of 32 bytes)
  Raw8bpp,       // Raw tile data (multiple of 64 bytes)
  Container2bpp, // 0x4500 bytes: 0x4000 data + 0x100 header + 0x400 table
  Container4bpp, // 0x8500 bytes: 0x8000 data + 0x100 header + 0x400 table
  Container8bpp  // 0x10100 bytes: 0x10000 data + 0x100 header (no table)
}

public CGXVariant DetectCGXVariant(long fileSize) {
  if (fileSize == 0x4500) return CGXVariant.Container2bpp;
  if (fileSize == 0x8500) return CGXVariant.Container4bpp;
  if (fileSize == 0x10100) return CGXVariant.Container8bpp;

  // Raw format detection (no header/table)
  if (fileSize % 32 == 0) return CGXVariant.Raw4bpp;  // Most common
  if (fileSize % 16 == 0) return CGXVariant.Raw2bpp;
  if (fileSize % 64 == 0) return CGXVariant.Raw8bpp;

  return null;  // Unknown format
}
```

### Extracting tile data by variant

```csharp
public byte[] ExtractTileData(string filePath, CGXVariant variant) {
  byte[] buffer = File.ReadAllBytes(filePath);

  switch (variant) {
    case CGXVariant.Container2bpp:
      // First 0x4000 bytes are tile data
      return buffer.Take(0x4000).ToArray();

    case CGXVariant.Container4bpp:
      // First 0x8000 bytes are tile data
      return buffer.Take(0x8000).ToArray();

    case CGXVariant.Container8bpp:
      // First 0x10000 bytes are tile data
      return buffer.Take(0x10000).ToArray();

    case CGXVariant.Raw2bpp:
    case CGXVariant.Raw4bpp:
    case CGXVariant.Raw8bpp:
      // Entire file is tile data
      return buffer;

    default:
      return null;
  }
}
```

### Reading per-tile table (4bpp example)

```csharp
public byte[] ReadPerTileTable(string filePath, CGXVariant variant) {
  byte[] buffer = File.ReadAllBytes(filePath);

  switch (variant) {
    case CGXVariant.Container2bpp:
      // Table at 0x4100 (0x4000 + 0x100)
      return buffer.Skip(0x4100).Take(0x400).ToArray();

    case CGXVariant.Container4bpp:
      // Table at 0x8100 (0x8000 + 0x100)
      return buffer.Skip(0x8100).Take(0x400).ToArray();

    case CGXVariant.Container8bpp:
      // No per-tile table; return null
      return null;

    default:
      return null;
  }
}
```

## Decoding complete CGX file

Complete CGX reader pseudocode (C#-like):

```csharp
public class CGXBank {
  public int TileCount { get; set; }
  public int BitsPerPixel { get; set; }
  public Color[][] TilePixels { get; set; }  // [tileIndex][pixelIndex]
  public byte[] PerTileTable { get; set; }   // palette prefix per tile
}

public CGXBank LoadCGX(string filePath, Palette[] colorBanks) {
  byte[] buffer = File.ReadAllBytes(filePath);
  CGXVariant variant = DetectCGXVariant(buffer.Length);

  byte[] tileData = ExtractTileData(filePath, variant);
  byte[] perTileTable = ReadPerTileTable(filePath, variant);

  // Determine bytes per tile
  int bytesPerTile;
  int bitsPerPixel = (variant == CGXVariant.Container2bpp || variant == CGXVariant.Raw2bpp) ? 2
                   : (variant == CGXVariant.Container4bpp || variant == CGXVariant.Raw4bpp) ? 4
                   : 8;
  bytesPerTile = bitsPerPixel == 2 ? 16 : bitsPerPixel == 4 ? 32 : 64;

  int tileCount = tileData.Length / bytesPerTile;
  CGXBank bank = new CGXBank { TileCount = tileCount, BitsPerPixel = bitsPerPixel, PerTileTable = perTileTable };
  bank.TilePixels = new Color[tileCount][];

  // Decode each tile
  for (int tileIdx = 0; tileIdx < tileCount; tileIdx++) {
    int tileOffset = tileIdx * bytesPerTile;
    bank.TilePixels[tileIdx] = DecodeTile(tileData, tileOffset, bitsPerPixel, colorBanks, perTileTable?[tileIdx]);
  }

  return bank;
}

private Color[] DecodeTile(byte[] tileData, int offset, int bitsPerPixel, Palette[] colorBanks, byte? prefixByte) {
  Color[] tile = new Color[64];  // 8×8 = 64 pixels

  if (bitsPerPixel == 4) {
    // 4bpp decoding (two 16-byte blocks: planes 0-1, then 2-3)
    for (int y = 0; y < 8; y++) {
      byte plane0 = tileData[offset + y * 2];
      byte plane1 = tileData[offset + y * 2 + 1];
      byte plane2 = tileData[offset + 16 + y * 2];
      byte plane3 = tileData[offset + 16 + y * 2 + 1];

      for (int x = 0; x < 8; x++) {
        int bit = 7 - x;
        int p0 = (plane0 >> bit) & 1;
        int p1 = (plane1 >> bit) & 1;
        int p2 = (plane2 >> bit) & 1;
        int p3 = (plane3 >> bit) & 1;

        int pixelIndex = (p3 << 3) | (p2 << 2) | (p1 << 1) | p0;

        // Apply per-tile prefix if present
        if (prefixByte.HasValue) {
          pixelIndex = (prefixByte.Value << 4) | pixelIndex;  // Merge into 8-bit index
        }

        // Look up color in palette
        tile[y * 8 + x] = colorBanks[0].GetColor(pixelIndex);  // Assumes colorBanks[0] is active
      }
    }
  }

  return tile;
}
```

---

Generated by Co-Pilot from
https://www.retroreversing.com/snes-file-formats
