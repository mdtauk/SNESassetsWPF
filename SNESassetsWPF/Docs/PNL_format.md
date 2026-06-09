# S‑CG‑CAD `.PNL` Format — Complete Specification  
Covers **all known behaviour** of PNL files as used by S‑CG‑CAD and H‑CG‑CAD.

PNL files represent the **panel editor state**: a large 32×512 grid of tile slots, each optionally containing a CGX tile with attributes (palette row, flip, priority).  
PNL is the **source of truth** for tile attributes that later propagate into SCR and MAP.

This document is code‑agnostic and describes the real format behaviour.

---

# 1. Purpose of PNL in the S‑CG‑CAD toolchain

PNL is an **editor‑side meta‑tile panel**, not a SNES runtime format.

Hierarchy:

```
COL  →  colours
CGX  →  tile graphics
PNL  →  panel of tiles (editor state)
MAP  →  map using PNL tiles
SCR  →  final flat tilemap (SNES‑ready)
```

PNL stores:

- Which CGX tile appears in each panel slot  
- Which palette row it uses  
- Flip flags  
- Priority flag  
- Whether the tile slot is “present” or “empty”  
- Editor preview settings (colormap mode, bank selectors, meta‑tile size)

PNL does **not** store:

- Pixel data  
- SNES palette data  
- SNES tilemaps  

---

# 2. File Layout (Fixed Size)

A PNL file is always:

```
0x100 header
0x4000 attribute words
0x4000 flag words
```

Total size: **0x8100 bytes**

```
+------------------------------+ 0x0000
| Header (0x100 bytes)         |
+------------------------------+ 0x0100
| Tile attribute table         | 0x4000 × 2 bytes
+------------------------------+ 0x4100
| Tile flag table              | 0x4000 × 2 bytes
+------------------------------+ 0x8100 EOF
```

The panel always contains **16384 tile slots** arranged as:

```
Width  = 32 tiles
Height = 512 tiles
Total  = 32 × 512 = 16384 = 0x4000
```

Tile index = `y * 32 + x`.

---

# 3. Header (0x100 bytes)

The header contains:

- Provenance string  
- Editor preview settings  
- Meta‑tile size exponents  
- CGX/colour bank selectors  

Most fields are **editor metadata**, not SNES runtime data.

## 3.1 Provenance (0x00–0x1F)

ASCII string:

```
"NAK1989 S-CG-CADVer...."
```

Purely informational.

## 3.2 Editor settings (0x60–0x6A)

These bytes control how the editor previews the panel:

| Offset | Name | Meaning |
|--------|------|---------|
| 0x60 | colormap mode | 32‑colour / 128‑colour / 256‑colour preview mode |
| 0x61 | mode7 flag | Toggles “Mode 7” label in UI |
| 0x62 | panel graphics mode | Small mode selector; only bit 1 used |
| 0x63 | referenced CGX bank | Which CGX bank to sample in some ops |
| 0x64 | colormap bank index | Colour bank selector |
| 0x65 | colormap selector A | Selects colormap slice |
| 0x66 | colormap selector B | Secondary selector |
| 0x67–0x68 | base tile index | Last selected CGX tile |
| 0x69 | width exponent | Meta‑tile width = 1 << (value & 0x1F) |
| 0x6A | height exponent | Meta‑tile height = 1 << (value & 0x1F) |

### 3.3 Meta‑tile size

These exponents do **not** change the file structure.  
They change how the editor interprets a “tile” in the UI:

```
metaWidth  = 1 << (header[0x69] & 0x1F)
metaHeight = 1 << (header[0x6A] & 0x1F)
```

Examples:

- 1×1 → normal 8×8 tiles  
- 2×2 → 16×16 meta‑tiles  
- 4×4 → 32×32 meta‑tiles  

### 3.4 Preservation rule

**The entire 0x100‑byte header must be preserved exactly.**  
Unknown bytes exist and are used by some editor builds.

---

# 4. Tile Attribute Table (0x4000 words)

Each entry is a **16‑bit big‑endian word** describing one tile slot.

### 4.1 Bit layout (confirmed from H‑CG‑CAD)

```
bit 15   : vertical flip
bit 14   : horizontal flip
bit 13   : priority
bits 12–10 : palette row (0–7)
bits 9–0 : CGX tile index (0–1023)
```

ASCII:

```
15  14  13  12 11 10   9 ... 0
+---+---+---+----------+------+
| V | H | P | Palette  | Tile |
+---+---+---+----------+------+
```

### 4.2 Meaning

| Field | Meaning |
|-------|---------|
| Tile index | Which CGX tile to draw |
| Palette row | Which COL palette row to use |
| H/V flip | Mirror tile |
| Priority | Editor‑side priority flag |

### 4.3 Endianness

Stored as **big‑endian**.  
Do not byte‑swap when round‑tripping.

---

# 5. Tile Flag Table (0x4000 words)

Each entry is a **16‑bit big‑endian word**.

### 5.1 Meaning of bit 15

```
bit 15 = 1 → tile is present
bit 15 = 0 → tile is empty
```

This is the **only bit used** by H‑CG‑CAD.

### 5.2 Lower 15 bits

- Often non‑zero  
- Not used by H‑CG‑CAD  
- Must be preserved  
- Likely editor metadata or unused legacy bits  

### 5.3 Behaviour

If bit 15 = 0:

- The tile is treated as **empty**  
- Attribute word is ignored  
- Editor tools skip the tile  

---

# 6. Panel Dimensions and Indexing

The panel is always:

```
32 tiles wide
512 tiles tall
```

Tile index:

```
index = y * 32 + x
```

This matches MAP’s coordinate system:

- panelX = 0–31  
- panelY = 0–511  

---

# 7. How PNL Attributes Propagate into SCR

When converting PNL → MAP → SCR:

| PNL field | SCR field |
|-----------|-----------|
| vflip | SCR bit 15 |
| hflip | SCR bit 14 |
| priority | SCR bit 13 |
| palette row | SCR bits 12–10 |
| tile index | SCR bits 0–9 (depending on MAP attribute source flag) |

PNL is the **source of truth** for SCR attributes.

---

# 8. Relationship to CGX and COL

PNL does **not** contain pixel or palette data.

It references:

- **CGX tile index** → which tile to draw  
- **Palette row** → which COL row to use  

The editor preview uses:

- colormap mode  
- col_half  
- col_bank  
- selectors A/B  

…but these do not affect the on‑disk tile attributes.

---

# 9. Summary for Non‑Programmers

A PNL file is:

- A **32×512 grid** of tile slots  
- Each slot may be empty or contain a CGX tile  
- Each tile has:
  - Palette row  
  - Flip flags  
  - Priority flag  
- A header describes how the editor previews colours and meta‑tiles  
- PNL → MAP → SCR conversion copies these attributes into the final SNES tilemap  

Conceptually:

```
CGX + COL → PNL (panel)
PNL → MAP (layout)
MAP + PNL → SCR (final tilemap)
```

---

# 10. Variant Summary

Unlike SCR and CGX, **PNL has only one known variant**:

| Size | Meaning |
|------|---------|
| **0x8100 bytes** | Standard S‑CG‑CAD PNL |

No alternate formats have been observed.

---

# 11. Preservation Rules

To maintain compatibility with S‑CG‑CAD:

- Preserve the **entire header**  
- Preserve **all 16 bits** of the flag table  
- Only modify:
  - Attribute words  
  - Flag bit 15  

Everything else must be round‑tripped exactly.

---

This is the complete, authoritative PNL specification.
