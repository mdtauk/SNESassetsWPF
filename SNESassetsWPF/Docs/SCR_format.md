# S‑CG‑CAD `.SCR` Format (0x2300‑byte Variant)
A complete, code‑agnostic description of the S‑CG‑CAD screen layout file format.  
This describes the 0x2300‑byte variant where the metadata/footer begins at offset 0x2000.

This document explains:
- The structure of the SCR file
- How tilemaps are stored
- How visibility data works
- How SCR references CGX graphics tiles
- How SCR references COL palette rows
- How tile size and palette selection are determined

No code, no tool references, no implementation details.

---

## 1. File Overview

Total size: 0x2300 bytes (8960 bytes)

Layout:

    +----------------------+ 0x0000
    | Block 0 tilemap     | 0x0800 bytes
    +----------------------+ 0x0800
    | Block 1 tilemap     | 0x0800 bytes
    +----------------------+ 0x1000
    | Block 2 tilemap     | 0x0800 bytes
    +----------------------+ 0x1800
    | Block 3 tilemap     | 0x0800 bytes
    +----------------------+ 0x2000
    | Footer / metadata   | 0x0100 bytes
    +----------------------+ 0x2100
    | Visibility data     | 0x0200 bytes
    +----------------------+ 0x2300 (EOF)

The screen is composed of **four 32×32 tilemaps**, arranged:

    +---------+---------+
    | Block 0 | Block 1 |
    +---------+---------+
    | Block 2 | Block 3 |
    +---------+---------+

Each block is 32×32 tiles = 1024 tiles = 0x800 bytes.

---

## 2. Tilemap Data (Blocks 0–3)

Each block contains 1024 tile entries.  
Each tile entry is **2 bytes**, little‑endian.

### 2.1 Tile Entry Bit Layout

    Bits 0–9   : Tile index (0–1023)
    Bits 10–12 : Palette row index (0–7)
    Bit 14     : X flip (1 = flip horizontally)
    Bit 15     : Y flip (1 = flip vertically)

Bits 13 and 14 are unused except for flip.

### 2.2 Tile Order

Tiles are stored row‑by‑row:

    tileIndex = (row * 32) + column

    row:    0..31
    column: 0..31

---

## 3. Footer / Metadata (0x2000–0x20FF)

The footer begins at offset 0x2000 and is 256 bytes long.

### 3.1 ASCII Identification

    0x2000–0x200F : "NAK1989 S-CG-CAD"
    0x2010–0x2017 : Version string (ASCII)
    0x2018–0x201F : Date string (ASCII)

### 3.2 Metadata Fields

Offsets below are relative to 0x2000:

    0x40 : bitmode     (bit depth indicator)
    0x41 : mode7       (0 = normal, 1 = Mode 7)
    0x42 : scr_mode    (0 = 8×8 tiles, 1 = 16×16 tiles)
    0x43 : chr_bank    (graphics bank index)
    0x44 : col_bank    (color bank index)
    0x45 : col_half    (0 = palette colors 0–127, 1 = colors 128–255)
    0x46 : col_cell    (base palette cell index)
    0x47–0x48 : clr_chr_no (clear tile number)

### 3.3 Tile Size

    scr_mode = 0 → tile size = 8×8
    scr_mode = 1 → tile size = 16×16

---

## 4. Visibility Data (0x2100–0x22FF)

This region controls which tiles are visible.

Total size: 0x200 bytes  
Four blocks × 0x80 bytes each.

### 4.1 Conceptual Layout

Each block has:

    32×32 = 1024 tiles
    1024 visibility bits
    1024 bits = 128 bytes = 0x80 bytes

Visibility bit meaning:

    1 = tile is visible
    0 = tile is hidden

### 4.2 Visibility Block Locations

    Block 0: 0x2100–0x217F
    Block 1: 0x2180–0x21FF
    Block 2: 0x2200–0x227F
    Block 3: 0x2280–0x22FF

### 4.3 Bit Order

Visibility bits are stored in a packed bitstream.  
The exact byte ordering is not important for understanding the format:  
each block contains 1024 bits, one per tile, in tile order.

---

## 5. Relationship to CGX Graphics

SCR does not contain graphics.  
It references **tile indices** stored in a CGX file.

CGX contains:

- 1024 tiles for 2bpp
- 1024 tiles for 4bpp
- 1024 tiles for 8bpp

Tile index (bits 0–9) selects which CGX tile to draw.

SCR does not modify tile indices.  
SCR does not contain tile graphics.  
SCR does not contain tile attributes beyond flip and palette row.

---

## 6. Relationship to COL Palettes

SCR does not contain palette data.  
It references palette rows stored in a COL file.

### 6.1 Palette Halves

COL contains 256 colors:

    Half 0: colors 0–127
    Half 1: colors 128–255

SCR metadata field:

    col_half

selects which half is used.

### 6.2 Palette Rows

For 4bpp (most common):

- Each row = 16 colors
- There are 8 rows per half

Tile entry bits 10–12 select a row:

    paletteRow = 0–7

Actual palette row start:

    paletteStart = (col_half * 128) + (paletteRow * 16)

### 6.3 2bpp and 8bpp

2bpp:

- Each row = 4 colors
- paletteStart = (col_half * 128) + (paletteRow * 4)

8bpp:

- Uses all 256 colors
- paletteRow bits are ignored except for col_half

---

## 7. Putting It All Together

To interpret an SCR file:

1. Read the four tilemap blocks (0x0000–0x1FFF)
2. Read metadata at 0x2000
3. Determine tile size from scr_mode
4. Determine palette half from col_half
5. For each tile:
    - Read tile index (0–1023)
    - Read palette row (0–7)
    - Read flip flags
6. Read visibility bits at 0x2100–0x22FF
7. For each tile:
    - If visibility bit = 0 → tile is hidden
    - If visibility bit = 1 → tile is drawn
8. When drawing:
    - Use CGX tile index to fetch graphics
    - Use COL palette half + palette row to fetch colors

---

### Generated by Copilot
