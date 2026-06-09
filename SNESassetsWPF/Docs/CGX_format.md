# S‑CG‑CAD `.CGX` Format — Complete Specification  
Covers **all known CGX variants** used by S‑CG‑CAD and H‑CG‑CAD.

Variants included:

1. **2bpp CGX Container** — 0x4500 bytes  
2. **4bpp CGX Container** — 0x8500 bytes  
3. **8bpp CGX Container** — 0x10100 bytes  
4. **Raw CGX (no header/table)** — arbitrary size, pure tile data  

Each variant is documented separately to avoid confusion.

---

# 1. CGX Fundamentals (All Variants)

Regardless of variant:

- Tiles are always **8×8 pixels**
- SNES planar encoding is always used
- Tile count is always **1024 tiles per bank**
- Tile order is always linear (tile 0, tile 1, … tile 1023)
- Footer always begins immediately after the tile data region
- Footer always contains:
  - `"NAK1989 S-CG-CAD"`
  - Version string
  - Date string
  - Bitmode
  - Palette bank selectors

The only differences between variants are:

- **Bit depth (2/4/8 bpp)**
- **Tile byte size (16/32/64 bytes)**
- **Presence/absence of per‑tile attribute table**
- **Footer offset (depends on bit depth)**

---

# 2. Tile Encoding (All Variants)

| Bit depth | Bytes per tile | Planes | Notes |
|----------:|----------------|--------|-------|
| **2bpp** | 16 | 2 planes | 2 bytes per row |
| **4bpp** | 32 | 4 planes | 4 bytes per row |
| **8bpp** | 64 | 8 planes | 8 bytes per row |

Tiles are always stored as:

```
tile[0], tile[1], tile[2], ... tile[1023]
```

Each tile is stored in SNES planar format:

- Bit 0 = plane 0  
- Bit 1 = plane 1  
- Bit 2 = plane 2  
- Bit 3 = plane 3  
- etc.

Pixel index is assembled from plane bits:

```
index = (p7<<7)|(p6<<6)|...|(p1<<1)|p0
```

---

# 3. CGX Container Variants

## 3.1 2bpp CGX Container (0x4500 bytes)

```
+----------------------+ 0x0000
| Tile data (2bpp)    | 0x4000 bytes
+----------------------+ 0x4000
| Footer / header     | 0x0100 bytes
+----------------------+ 0x4100
| Per‑tile table      | 0x0400 bytes
+----------------------+ 0x4500 EOF
```

### Tile data
- 0x4000 bytes = 1024 tiles × 16 bytes

### Footer
- `"NAK1989 S-CG-CAD"`
- Version, date
- bitmode, col_bank, col_half, col_cell

### Per‑tile table (0x400 bytes)
- 1024 entries (1 byte per tile)
- Provides **palette prefix** for editor preview
- Final palette index:

```
finalIndex = (prefix << 2) | pixelIndex2bpp
```

---

## 3.2 4bpp CGX Container (0x8500 bytes)

```
+----------------------+ 0x0000
| Tile data (4bpp)    | 0x8000 bytes
+----------------------+ 0x8000
| Footer / header     | 0x0100 bytes
+----------------------+ 0x8100
| Per‑tile table      | 0x0400 bytes
+----------------------+ 0x8500 EOF
```

### Tile data
- 0x8000 bytes = 1024 tiles × 32 bytes

### Footer
Same structure as 2bpp.

### Per‑tile table
- 1024 entries
- Final palette index:

```
finalIndex = (prefix << 4) | pixelIndex4bpp
```

---

## 3.3 8bpp CGX Container (0x10100 bytes)

```
+----------------------+ 0x0000
| Tile data (8bpp)    | 0x10000 bytes
+----------------------+ 0x10000
| Footer / header     | 0x0100 bytes
+----------------------+ 0x10100 EOF
```

### Tile data
- 0x10000 bytes = 1024 tiles × 64 bytes

### Footer
Same structure as other variants.

### No per‑tile table
8bpp tiles already produce full 0–255 indices, so no prefix is needed.

---

# 4. Raw CGX (No Header/Table)

If the file size does **not** match any container size:

- The entire file is treated as tile data
- Bit depth is inferred from bytes per tile:
  - divisible by 16 → 2bpp
  - divisible by 32 → 4bpp
  - divisible by 64 → 8bpp
- No footer
- No per‑tile table

This is common for ROM‑extracted graphics.

---

# 5. Footer Structure (All Container Variants)

Footer begins at:

```
offset = tileDataSize
tileDataSize = 0x4000 << fmt
```

Where:

| fmt | Meaning | tileDataSize |
|-----|---------|--------------|
| 0 | 2bpp | 0x4000 |
| 1 | 4bpp | 0x8000 |
| 2 | 8bpp | 0x10000 |

Footer layout:

```
0x00–0x0F : "NAK1989 S-CG-CAD"
0x10–0x17 : Version string
0x18–0x1F : Date string
0x20      : bitmode
0x21      : col_bank
0x22      : col_half
0x23      : col_cell
...       : other editor metadata
```

These values affect **editor preview**, not tile decoding.

---

# 6. Palette Selection Rules (All Variants)

CGX does not contain palette data.  
It references COL palette rows.

### Palette stepping depends on bit depth:

| BPP | Colors per row | Step size |
|-----|----------------|-----------|
| 2bpp | 4 | `row * 4` |
| 4bpp | 16 | `row * 16` |
| 8bpp | 256 | entire palette |

### Palette half (col_half)
Selects:

```
0 → colors 0–127
1 → colors 128–255
```

### Per‑tile table (2bpp/4bpp only)
Adds a high‑bit prefix:

```
2bpp: prefix << 2
4bpp: prefix << 4
```

---

# 7. Variant Detection Summary

| File Size | Variant | BPP | Per‑tile Table |
|-----------|---------|-----|----------------|
| **0x4500** | 2bpp container | 2 | Yes |
| **0x8500** | 4bpp container | 4 | Yes |
| **0x10100** | 8bpp container | 8 | No |
| Other (multiple of 16/32/64) | Raw CGX | 2/4/8 | No |

---

# 8. Rendering Summary

To render a CGX tile:

1. Determine BPP from variant  
2. Read tile bytes (16/32/64)  
3. Decode SNES planar bitplanes  
4. Compute pixel index  
5. If 2bpp/4bpp and table exists:  
   - Apply prefix  
6. Use COL palette (col_half + palette row)  
7. Draw 8×8 tile  

---

This is the complete, authoritative CGX specification.
