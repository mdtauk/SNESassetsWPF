# S‑CG‑CAD MAP File Format  
**Meta‑Tile Tilemap File**  
Version: 2025‑06 (based on RetroReversing + H‑CG‑CAD reverse engineering)

---

# Overview

A **MAP file** is a tilemap made of **meta‑tiles** defined in a PNL file.

MAP files do **not** contain:

- Pixel data  
- Palette data  
- Raw 8×8 tile indices  

They contain:

- A grid of **meta‑tile references**
- Flip flags
- Priority flags
- Optional palette group override

To render a MAP, you must load the **PNL** it references.

---

# File Layout (High‑Level)

```
+---------------------------+
| Header (0x20 bytes)       |
+---------------------------+
| Tilemap Data              |
|  (Width × Height entries) |
+---------------------------+
| Footer / Metadata         |
|  (0x100 bytes, optional)  |
+---------------------------+
```

---

# 1. Header (0x20 bytes)

| Offset | Size | Description |
|--------|------|-------------|
| 0x00   | 4    | Magic `"MAP "` |
| 0x04   | 2    | Width in meta‑tiles |
| 0x06   | 2    | Height in meta‑tiles |
| 0x08   | 4    | Offset to tilemap |
| 0x0C   | 4    | Offset to footer |
| 0x10   | 16   | Reserved |

### ASCII Diagram

```
0x00  +-------------------------------+
      |  'M' 'A' 'P' ' '              |
0x04  +-------------------------------+
      |  Width (meta-tiles)           |
0x06  +-------------------------------+
      |  Height (meta-tiles)          |
0x08  +-------------------------------+
      |  Offset: Tilemap              |
0x0C  +-------------------------------+
      |  Offset: Footer               |
0x10  +-------------------------------+
      |  Reserved (16 bytes)          |
      +-------------------------------+
```

---

# 2. Tilemap Data

Each tilemap entry is a **16‑bit meta‑tile reference word**:

```
v h p PPP iiiiiiii
```

Where:

| Bits | Meaning |
|------|---------|
| 15   | V‑flip (applied to entire meta‑tile) |
| 14   | H‑flip |
| 13   | Priority |
| 12–10 | Palette group override (optional) |
| 9–0  | Meta‑tile index (0–1023) |

### ASCII Diagram

```
15 14 13 12 11 10  9 ... 0
+--+--+--+-----------+-----+
|V |H |P | Palette   |Index|
+--+--+--+-----------+-----+
```

### Tilemap Grid Example (4×4 MAP)

```
+--------+--------+--------+--------+
|  MT00  |  MT01  |  MT02  |  MT03  |
+--------+--------+--------+--------+
|  MT10  |  MT11  |  MT12  |  MT13  |
+--------+--------+--------+--------+
|  MT20  |  MT21  |  MT22  |  MT23  |
+--------+--------+--------+--------+
|  MT30  |  MT31  |  MT32  |  MT33  |
+--------+--------+--------+--------+
```

---

# 3. Footer (optional, 0x100 bytes)

Editor metadata:

- S‑CG‑CAD version
- Notes
- Tile preview settings
- Unknown flags

Not required for rendering.

---

# Rendering Notes

### MAP → SCR conversion (flattening)

To render a MAP:

1. For each MAP cell:
   - Read meta‑tile index
   - Apply flip flags
   - Apply palette override (if present)
2. Fetch meta‑tile from PNL
3. Expand meta‑tile into raw 8×8 tiles
4. Apply flips to the expanded tile grid
5. Output a full SCR‑style tilemap

### Missing PNL file
If PNL is missing:

- MAP cannot be rendered normally  
- Debug renderers should show:
  - Meta‑tile index
  - Bounding boxes
  - Placeholder tiles

---

# Summary

MAP files are **tilemaps of meta‑tiles**.  
They reference PNL files, which reference CGX+COL.  
They contain no pixel data and must be flattened to SCR for rendering.

