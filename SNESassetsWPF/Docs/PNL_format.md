# S‑CG‑CAD PNL File Format  
**Meta‑Tile Definition File**  
Version: 2025‑06 (based on RetroReversing + H‑CG‑CAD reverse engineering)

---

# Overview

A **PNL file** defines a set of **meta‑tiles** (also called “patterns” or “brushes”) used by S‑CG‑CAD.

A meta‑tile is a **grid of 8×8 CGX tiles**, each with:

- Tile index (into CGX)
- Palette row (0–7)
- Flip X / Flip Y
- Priority bit
- Optional editor flags

PNL files do **not** contain pixel data or palette data.  
They reference **CGX** (tile graphics) and **COL** (palette) files.

---

# File Layout (High‑Level)

```
+---------------------------+
| Header (0x20 bytes)       |
+---------------------------+
| Meta‑Tile Table           |
|  (variable size)          |
+---------------------------+
| Meta‑Tile Entries         |
|  (tile attribute grids)   |
+---------------------------+
| Footer / Metadata         |
|  (0x100 bytes, optional)  |
+---------------------------+
```

---

# 1. Header (0x20 bytes)

| Offset | Size | Description |
|--------|------|-------------|
| 0x00   | 4    | Magic `"PNL "` (ASCII) |
| 0x04   | 2    | Meta‑tile count |
| 0x06   | 2    | Unknown (always 0) |
| 0x08   | 2    | Meta‑tile width in tiles (8×8 tiles) |
| 0x0A   | 2    | Meta‑tile height in tiles |
| 0x0C   | 4    | Offset to Meta‑Tile Table |
| 0x10   | 4    | Offset to Meta‑Tile Entries |
| 0x14   | 4    | Offset to Footer (optional) |
| 0x18   | 8    | Reserved / editor metadata |

### ASCII Diagram

```
0x00  +-------------------------------+
      |  'P' 'N' 'L' ' '              |
0x04  +-------------------------------+
      |  MetaTileCount (ushort)       |
0x06  +-------------------------------+
      |  Reserved                     |
0x08  +-------------------------------+
      |  MetaTileWidth (tiles)        |
0x0A  +-------------------------------+
      |  MetaTileHeight (tiles)       |
0x0C  +-------------------------------+
      |  Offset: MetaTileTable        |
0x10  +-------------------------------+
      |  Offset: MetaTileEntries      |
0x14  +-------------------------------+
      |  Offset: Footer               |
0x18  +-------------------------------+
      |  Reserved (8 bytes)           |
      +-------------------------------+
```

---

# 2. Meta‑Tile Table

A list of **meta‑tile descriptors**, one per meta‑tile.

Each entry:

| Offset | Size | Description |
|--------|------|-------------|
| 0x00   | 2    | Meta‑tile ID |
| 0x02   | 2    | Width in tiles |
| 0x04   | 2    | Height in tiles |
| 0x06   | 4    | Offset to tile attribute grid |
| 0x0A   | 6    | Reserved |

### ASCII Diagram

```
MetaTileTableEntry:
+---------------------------+
| ID (ushort)               |
+---------------------------+
| Width (tiles)             |
+---------------------------+
| Height (tiles)            |
+---------------------------+
| OffsetToTileGrid (uint32) |
+---------------------------+
| Reserved (6 bytes)        |
+---------------------------+
```

---

# 3. Meta‑Tile Entries (Tile Attribute Grids)

Each meta‑tile contains a **Width × Height** grid of **tile attribute words**.

Each tile attribute is a **16‑bit SNES‑style tilemap entry**:

```
v h p PPP tttttttttt
```

Where:

| Bits | Meaning |
|------|---------|
| 15   | V‑flip |
| 14   | H‑flip |
| 13   | Priority |
| 12–10 | Palette row (0–7) |
| 9–0  | Tile index (0–1023) |

### ASCII Diagram (per tile)

```
15 14 13 12 11 10  9 ... 0
+--+--+--+-----------+-----+
|V |H |P | Palette   |Tile |
+--+--+--+-----------+-----+
```

### Meta‑Tile Grid Example (16×16 meta‑tile)

```
+------+------+------+------+
| t00 | t01 | t02 | t03 | ...
+------+------+------+------+
| t10 | t11 | t12 | t13 | ...
+------+------+------+------+
| ...                          |
```

---

# 4. Footer (optional, 0x100 bytes)

Editor metadata:

- S‑CG‑CAD version
- Notes
- Tile preview settings
- Unknown flags

Not required for rendering.

---

# Summary

PNL files define **meta‑tiles** made of **8×8 CGX tiles**.  
They store tile attributes but **no pixel data**.  
They are used by MAP files to build larger tilemaps.

