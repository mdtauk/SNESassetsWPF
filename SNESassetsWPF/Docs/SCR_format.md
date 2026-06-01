# SCR format — screen layout / background state (SNES / S-CG-CAD)

This document describes the SCR (screen) file format used in S-CG-CAD for storing
screen-specific render settings, layer configuration, and sprite/object placement.
Non-expert programmers can use this to understand the on-disk layout and implement
a basic parser.

## Summary

- **Purpose**: Store per-screen metadata (layer visibility, scroll behavior, collision data, object placement)
- **Typical size**: Highly variable; commonly 0x200–0x1000 bytes depending on content
- **Structure**: Header block + layer definitions + object list + optional collision/priority data
- **Endianness**: 16-bit words are stored big-endian on disk
- **Content**: bit flags, layer indices, tile dimensions, object spawns, collision types

## File layout (canonical)

| Offset | Size | Meaning |
| ---: | ---: | --- |
| 0x0000 - 0x00FF | 0x100 | screen header and metadata |
| 0x0100 - 0x013F | 0x40 | layer configuration (4 layers × 16 bytes) |
| 0x0140 - ... | variable | object / sprite list |
| ... | variable | optional collision / effect data |

ASCII overview:

```
SCR file (variable size):

+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+
| 0x00    Screen header and metadata           | 0xFF
| Global scale, layer flags, scroll behavior   |
+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+
| 0x0100  Layer 0 config (MAP, PNL refs, ...)  | 0x010F
+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+
| 0x0110  Layer 1 config                       | 0x011F
+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+
| 0x0120  Layer 2 config                       | 0x012F
+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+
| 0x0130  Layer 3 config                       | 0x013F
+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+
| 0x0140  Object / sprite list (variable)      |
+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+
```

## Screen header (0x100 bytes)

Common fields in the SCR header:

| Offset | Size | Meaning | Notes |
| ---: | ---: | --- | --- |
| 0x00 - 0x1F | 0x20 | ASCII provenance string | Often begins with `NAK1989 S-CG-CADVer...` |
| 0x40 | 1 | BG mode (0–7) | SNES BG mode selector (affects layer count, tile size, etc.) |
| 0x41 | 1 | global mosaic | Applies 16×16 or larger pixelation effect globally (0 = off) |
| 0x42 | 1 | color math enable | 0/1 toggle for color addition/subtraction effects |
| 0x43 | 1 | interlace mode | 0/1 toggle for interlaced rendering |
| 0x44 | 1 | reserved | typically 0x00 |
| 0x60 | 1 | number of layers | typically 2–4 (max 4 on SNES) |
| 0x61 | 1 | layer 0 enable | bit 0 = visible, bits 1–7 = reserved |
| 0x62 | 1 | layer 1 enable | bit 0 = visible |
| 0x63 | 1 | layer 2 enable | bit 0 = visible |
| 0x64 | 1 | layer 3 enable | bit 0 = visible |
| 0x70 | 1 | layer 0 priority | 0–3 (affects depth ordering) |
| 0x71 | 1 | layer 1 priority | 0–3 |
| 0x72 | 1 | layer 2 priority | 0–3 |
| 0x73 | 1 | layer 3 priority | 0–3 |
| 0x7E - 0x7F | 2 | screen width pixels | 16-bit big-endian (typically 256) |
| 0x80 - 0x81 | 2 | screen height pixels | 16-bit big-endian (typically 224) |

## Layer configuration (0x40 bytes total, 0x10 bytes per layer)

Each of 4 layers occupies 16 bytes at offsets 0x0100 + N×0x10:

| Offset within layer block | Size | Meaning | Notes |
| ---: | ---: | --- | --- |
| +0x00 - 0x01 | 2 | MAP bank reference | 16-bit big-endian: which MAP file index to use |
| +0x02 - 0x03 | 2 | PNL bank reference | 16-bit big-endian: which PNL (metatile panel) to use |
| +0x04 | 1 | layer scroll mode | 0 = fixed, 1 = scrolls with camera, 2 = parallax |
| +0x05 | 1 | parallax offset X | If scroll mode = 2, scale factor for X (0.5× to 2×) |
| +0x06 | 1 | parallax offset Y | If scroll mode = 2, scale factor for Y (0.5× to 2×) |
| +0x07 | 1 | layer alpha / opacity | 0–255 (0 = transparent, 255 = opaque) |
| +0x08 | 1 | mosaic enable | 0 = off, 1 = apply global mosaic |
| +0x09 | 1 | window enable | 0 = no clipping, 1 = use window for layer (see window section) |
| +0x0A - 0x0B | 2 | scroll X offset | 16-bit big-endian (pixels, applied on top of camera) |
| +0x0C - 0x0D | 2 | scroll Y offset | 16-bit big-endian (pixels) |
| +0x0E - 0x0F | 2 | reserved | typically 0x0000 |

Complete layer parsing example (C#-like pseudocode):

```csharp
public class LayerConfig {
  public int MapBank { get; set; }
  public int PnlBank { get; set; }
  public byte ScrollMode { get; set; }  // 0=fixed, 1=scrolls, 2=parallax
  public byte ParallaxX { get; set; }
  public byte ParallaxY { get; set; }
  public byte Alpha { get; set; }
  public bool MosaicEnabled { get; set; }
  public bool WindowEnabled { get; set; }
  public short ScrollX { get; set; }
  public short ScrollY { get; set; }
}

public LayerConfig[] LoadLayers(byte[] buffer) {
  LayerConfig[] layers = new LayerConfig[4];

  for (int i = 0; i < 4; i++) {
    int baseOffset = 0x0100 + (i * 0x10);
    layers[i] = new LayerConfig {
      MapBank = ReadBigEndian16(buffer, baseOffset + 0x00),
      PnlBank = ReadBigEndian16(buffer, baseOffset + 0x02),
      ScrollMode = buffer[baseOffset + 0x04],
      ParallaxX = buffer[baseOffset + 0x05],
      ParallaxY = buffer[baseOffset + 0x06],
      Alpha = buffer[baseOffset + 0x07],
      MosaicEnabled = buffer[baseOffset + 0x08] != 0,
      WindowEnabled = buffer[baseOffset + 0x09] != 0,
      ScrollX = (short)ReadBigEndian16(buffer, baseOffset + 0x0A),
      ScrollY = (short)ReadBigEndian16(buffer, baseOffset + 0x0C)
    };
  }

  return layers;
}
```

## Window clipping (when layer window enable = 1)

If a layer has `WindowEnabled = true`, it uses an SNES window region for clipping.
Window definitions typically appear after the object list or in a dedicated section.

Common window structure (if present):

| Offset (variable) | Size | Meaning |
| ---: | ---: | --- |
| +0x00 | 1 | window 0 enable | 0 = disabled, 1 = enabled |
| +0x01 | 1 | window 1 enable | second window (OR / AND logic) |
| +0x02 | 1 | logic mode | 0 = OR, 1 = AND, 2 = XOR |
| +0x03 - 0x04 | 2 | window 0 left edge (X) | 16-bit value |
| +0x05 - 0x06 | 2 | window 0 right edge (X) | 16-bit value |
| +0x07 - 0x08 | 2 | window 0 top edge (Y) | 16-bit value |
| +0x09 - 0x0A | 2 | window 0 bottom edge (Y) | 16-bit value |

To find window data, scan for a section marker or check file size. Window clipping is rarely
used in early CAD projects; safely skip if no clear window structure is found.

## Decoding example

```csharp
int offset = 0x0100;
while (true) {
  byte typeId = buffer[offset];
  if (typeId == 0xFF) break;  // end-of-list marker

  byte flags = buffer[offset + 0x01];
  short x = (short)ReadBigEndian16(offset + 0x02);
  short y = (short)ReadBigEndian16(offset + 0x04);
  ushort data = ReadBigEndian16(offset + 0x06);

  // Process object (render sprite, check collision, etc.)
  objects.Add(new GameObject { TypeId = typeId, X = x, Y = y, ... });

  offset += 8;  // advance to next object entry
}
```

## Optional collision / effect data

If present, after the object list:

- **Collision map**: per-tile (8×8) collision type (solid, spike, water, etc.)
- **Priority map**: per-tile priority override
- **Effect map**: per-tile special effect (lava damage, ice slip, etc.)

Each is typically stored as a 2D array matching the MAP dimensions
(32 × 27 bytes for standard screens), or compressed.

Offset varies depending on object count; look for a section marker (e.g., 0x0200)
to locate collision data reliably.

## Practical editor usage

To load and edit a SCR:

1. Read the 0x100 header to determine BG mode, layer count, and screen dimensions.
2. Parse the 4 layer configs (0x0100 - 0x013F) to determine which MAP/PNL banks are active.
3. Read the object list starting at 0x0140, stopping at 0xFF marker.
4. If window enable is set on any layer, search for window definitions (usually after object list).
5. If collision data exists, find its offset (often 0x0200 or align to next page boundary).
6. On save, regenerate object list (recalculate length and re-encode entries) and preserve collision/window data.

## File validation

When reading an SCR:

1. **Check file size**: minimum 0x200 bytes (header + minimal layer data)
2. **Validate layer counts**: max 4 layers; unused should have all zeros or repeat last active layer
3. **Validate object list**: should terminate with 0xFF marker within file bounds
4. **Detect window/collision**: scan for recognizable markers after object list

## Common variations

- **Variable object entry size**: Some SCR files use 16-byte objects instead of 8-byte.
- **Compressed object list**: Rare; RLE-encoded object blocks may be used.
- **No collision data**: Minimal SCR files contain only header + layers + objects.
- **Multiple screens**: Some projects pack multiple SCR files into a single container (e.g., world.scr with chapter/room metadata).
- **Per-layer windows**: Advanced SCR may define separate windows for each layer (rarely used).

## Notes and edge cases

- SNES BG mode (0–7) affects how many layers are available and their tile sizes (8×8, 16×16, etc.).
- Layer priority determines depth order (foreground vs. background).
- Parallax scrolling requires careful scale/offset calculation per frame (parallax factors may be 0.5×, 1×, 1.5×, 2×).
- Object type IDs are game-specific; the same ID may mean "Goomba" in one game and "Spring" in another.
- Window masking is advanced and rarely used in early CAD projects; safely ignore if `window enable = 0`.
- Always preserve unknown header bytes when round-tripping.

---

Document prepared to aid implementing a SCR parser or level editor in this project.
