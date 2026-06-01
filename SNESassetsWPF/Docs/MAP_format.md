# MAP format — screen map / metatile grid (SNES / S-CG-CAD)

This document describes the MAP (map) file format used in S-CG-CAD for defining
a game screen layout. Non-expert programmers can use this to understand the on-disk
layout and implement a basic map parser or editor.

## Summary

- **Purpose**: A grid of metatile (composite tile) references that form a playable game screen
- **Typical size**: 32 × 27 metatiles (16,384 entries) = 32,768 bytes, plus optional header
- **Structure**: 0x100 optional header + metatile data table
- **Endianness**: 16-bit words are stored big-endian on disk
- **Content**: metatile IDs, attributes (priority, palette bank) per metatile

## File layout

| Offset | Size | Meaning |
| ---: | ---: | --- |
| 0x0000 - 0x00FF | 0x100 | optional editor header |
| 0x0100 - ... | variable | metatile table (16-bit big-endian words) |

ASCII overview:

```
MAP file (0x8000+ bytes):

+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+
| 0x00       CAD signature and header          | 0xFF
+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+
|   Metatile table: 32×27 metatiles            |
|   (16-bit big-endian words)                  |
|   One row = 0x40 bytes (32 entries)          |
|   27 rows = 0x420 bytes total metatile data  |
+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+
```

## Editor header (0x100 bytes)

The first 0x100 bytes are CAD-side metadata. Common fields:

| Offset | Size | Meaning | Notes |
| ---: | ---: | --- | --- |
| 0x00 - 0x1F | 0x20 | ASCII provenance string | Often begins with `NAK1989 S-CG-CADVer...` |
| 0x60 | 1 | colormap mode | Controls palette preview |
| 0x61 | 1 | Mode 7 enable flag | 0/1 toggle for Mode 7 UI |
| 0x63 | 1 | referenced PNL bank | `header[0x63] & 0x03` — which PNL to sample metatiles from |
| 0x64 | 1 | palette bank | `header[0x64] & 0x03` — color bank selector |
| 0x69 | 1 | screen width exponent | Shift factor: `screen_width = 1 << (header[0x69] & 0x1F)` tiles |
| 0x6A | 1 | screen height exponent | Shift factor: `screen_height = 1 << (header[0x6A] & 0x1F)` tiles |
| 0x6B - 0x6C | 2 | scroll X offset | 16-bit big-endian starting X in pixels |
| 0x6D - 0x6E | 2 | scroll Y offset | 16-bit big-endian starting Y in pixels |

Standard SNES game screens are 32 × 27 metatiles (256 × 216 pixels at 8×8 per metatile).
The exponents default to 5 for both (2^5 = 32, 2^5 = 32); but may differ for Mode 7 or
special layouts.

## Metatile table

The metatile table consists of 16-bit big-endian words, one per metatile entry.

Each word encodes:

| Bits | Mask | Meaning |
| ---: | ---: | --- |
| 15 | 0x8000 | priority (pri) |
| 14-12 | 0x7000 | palette bank (0-7) |
| 11-0 | 0x0FFF | metatile ID (0-4095) |

Decode example (C#-like pseudocode):

```csharp
ushort word = ReadBigEndianWord(offset);
bool priority = (word >> 15) & 1 == 1;
int paletteBank = (word >> 12) & 0x07;
int metatileId = word & 0x0FFF;
```

Layout: the metatile grid is row-major:

```csharp
int mapWidth = 1 << (header[0x69] & 0x1F);   // typically 32
int mapHeight = 1 << (header[0x6A] & 0x1F);  // typically 27

int mapIndex = mapY * mapWidth + mapX;       // mapX: 0-(mapWidth-1), mapY: 0-(mapHeight-1)
ushort word = metatileTable[mapIndex];
```

For a standard 32-wide screen, row offsets are 0x00, 0x40, 0x80, ... bytes.

## Metatile rendering

To render a MAP screen:

1. Read the optional 0x100 header to determine dimensions and referenced PNL bank.
2. Read the metatile table (starting at 0x100 or 0x0000 if no header).
3. For each metatile in the grid:
   - Decode the metatile word to extract ID, palette bank, and priority.
   - Look up the metatile in the referenced PNL (which tile blocks it contains).
   - For each constituent 8×8 tile in the metatile block:
     - Look up the tile ID in the referenced CGX bank.
     - Apply the specified palette bank (or metatile-level palette override).
     - Render with priority, flips, and other attributes from the PNL entry.

Example 32×27 screen rendering flow:

```
for (int y = 0; y < 27; y++) {
  for (int x = 0; x < 32; x++) {
    int mapIndex = y * 32 + x;
    ushort word = metatileTable[mapIndex];

    int metatileId = word & 0x0FFF;
    int paletteBank = (word >> 12) & 0x07;
    bool priority = (word >> 15) & 1 == 1;

    // Look up metatile in PNL (e.g., a 2×2 tile block)
    // For each constituent tile, render at (x*8 + tx*8, y*8 + ty*8)
    // Apply palette row and other attributes from PNL
  }
}
```

## Practical editor usage

To load, edit, or export a MAP:

1. Detect if the file begins with a CAD header (check offset 0x00 for ASCII signature).
2. Parse the header (if present) to get dimensions and palette info.
3. Read metatile words starting at offset 0x100 (or 0x0000 if no header).
4. Store metatiles in a 2D array or list for editing.
5. On save, preserve the 0x100 header (or write 0x100 zero bytes if headerless) and re-encode metatile words.

## Detecting headerless vs. header-based files

Use file size and content to determine structure:

```csharp
// Detect MAP header and dimensions
if (fileSize >= 0x8420) {  // 0x8420 = 0x100 + (32 * 27 * 2)
  // Likely has 0x100 header + metatile data
  int headerOffset = 0x100;

  // Verify header signature
  string signature = Encoding.ASCII.GetString(buffer, 0, 32);
  if (signature.Contains("NAK1989") || signature.Contains("S-CG-CAD")) {
    // Confirmed header present
    mapWidth = 1 << (buffer[0x69] & 0x1F);
    mapHeight = 1 << (buffer[0x6A] & 0x1F);
  }
} else if (fileSize >= 0x8000) {
  // Likely headerless metatile data (32 * 27 * 2 = 0x420 bytes or 32×256 = 0x4000 bytes)
  // Check metatile validity: most high bits should be 0 or 7 (palette 0-7)
  int headerOffset = 0x0000;
  // Infer dimensions from file size
  int tileCount = fileSize / 2;
  mapWidth = 32;
  mapHeight = tileCount / 32;  // variable height
}
```

Complete MAP decoder example:

```csharp
public class MapData {
  public int Width { get; set; }
  public int Height { get; set; }
  public ushort[] Metatiles { get; set; }
  public bool HasHeader { get; set; }
}

public MapData LoadMap(string filePath) {
  byte[] buffer = File.ReadAllBytes(filePath);
  MapData map = new MapData();

  // Heuristic: check first 32 bytes for ASCII provenance string
  string header = Encoding.ASCII.GetString(buffer, 0, 32);
  map.HasHeader = header.Contains("NAK1989") || header.Contains("S-CG-CAD");

  int dataOffset = map.HasHeader ? 0x100 : 0x0000;

  if (map.HasHeader) {
    // Read dimensions from header
    map.Width = 1 << (buffer[0x69] & 0x1F);
    map.Height = 1 << (buffer[0x6A] & 0x1F);
  } else {
    // Infer dimensions
    map.Width = 32;
    map.Height = (buffer.Length - dataOffset) / (map.Width * 2);
  }

  // Read metatiles
  int metatileCount = map.Width * map.Height;
  map.Metatiles = new ushort[metatileCount];

  for (int i = 0; i < metatileCount; i++) {
    int offset = dataOffset + (i * 2);
    if (offset + 1 < buffer.Length) {
      map.Metatiles[i] = ReadBigEndian16(buffer, offset);
    }
  }

  return map;
}
```

## Common variations

- **Headerless MAP**: File is all metatile data (size = mapWidth × mapHeight × 2 bytes).
- **With header**: Full 0x100-byte header + metatile data; size = 0x100 + mapWidth × mapHeight × 2 bytes.
- **Overscan / Mode 7**: Non-standard dimensions; read exponent fields in header. Typical: 32×30 (overscan) or 64×64 (Mode 7).
- **Compressed**: Some ROMs use RLE or LZ77 compression; S-CG-CAD typically stores uncompressed.

## File validation

When reading a MAP:

1. **Check file size**: minimum 0x420 bytes (32×27 metatiles uncompressed)
2. **Detect header**: check bytes 0x00-0x1F for ASCII provenance string
3. **Validate dimensions**: exponent fields should yield sensible width/height (4-256)
4. **Validate metatile words**: palette bits (14-12) should be 0-7; ID bits (11-0) should be 0-4095
5. **Handle truncation**: partial metatile data is valid; may represent partial map load

## Notes and edge cases

- Metatile IDs reference the PNL bank set in the header (offset 0x63).
- Palette bank in the metatile word can override or extend a base palette; interpretation depends on the game engine.
- Priority bits may control rendering order (behind / in front of sprites, water, etc.).
- The map can be scrolled by altering the scroll X/Y offset fields in the header.
- Always preserve the full 0x100 header when round-tripping; other CAD builds may use bytes we haven't decoded yet.

---

Document prepared to aid implementing a MAP editor or viewer in this project.
