# PNL_Format.md  
**S‑CG‑CAD PNL File Format – Editor‑Side Meta‑Tile Panels**

This document explains the **PNL** format in plain language, based on:

- The RetroReversing S‑CG‑CAD research
- Behaviour observed in the S‑CG‑CAD decomp / H‑CG‑CAD
- SNES tile and palette conventions

It is **code‑agnostic** and aimed at people who want to understand what a PNL “is” and how it behaves, not just how to parse bytes.

---

## 1. Where PNL sits in the format hierarchy

These are **editor formats** used during asset creation, not what the SNES sees directly:

- **COL** – Colour palettes (SNES CGRAM data)
- **CGX** – Tile graphics (SNES VRAM‑style bitplanes)
- **SCR** – Flat tilemap using raw 8×8 tiles  
- **PNL** – Panel / meta‑tile editor state built from CGX tiles  
- **MAP** – Map layout that references PNL panels

Hierarchy:

```text
COL
COL + CGX
SCR  <- COL + CGX
PNL  <- COL + CGX
MAP  <- PNL <- COL + CGX
```

So:

- **PNL depends on CGX + COL** (it describes how to arrange tiles and which colours to show)
- **MAP depends on PNL** (it places those panels in a larger layout)

PNL is the **source of truth** for per‑tile attributes (flip, priority, palette row) that eventually end up in SCR.

---

## 2. What a PNL file represents (conceptual view)

A **PNL file** is the saved state of the **panel editor** in S‑CG‑CAD.

You can think of it as:

- A big **virtual canvas** of 8×8 tiles (a “panel”)
- Each tile slot:
  - May be **empty**
  - Or may reference a **CGX tile** with:
    - A palette row (which colours to use)
    - Horizontal/vertical flip
    - A priority flag
- A set of **header settings** that tell the editor:
  - How to interpret tile indices
  - How big a “logical tile” is (8×8 vs 16×16 vs larger)
  - How to choose which colours to show in the preview

The important part:  
**PNL does not store SNES palette data or pixel data.**  
It stores **references and editor settings**.

---

## 3. High‑level file layout

On disk, a PNL file looks like this:

```text
+------------------------------+
| Header (0x100 bytes)         |
+------------------------------+
| Tile attribute table         |
|  - 0x4000 16-bit words       |
+------------------------------+
| Tile flag table              |
|  - 0x4000 16-bit words       |
+------------------------------+
```

- The **header** stores editor settings and provenance.
- The **tile attribute table** stores per‑tile CGX index + flip + priority + palette row.
- The **tile flag table** stores per‑tile “present/active” flags and some unknown bits.

The two tables together describe a **32×512 tile panel** (32 tiles per row, 512 rows).

---

## 4. Header (0x100 bytes)

### 4.1 Provenance string (0x00–0x1F)

The first 0x20 bytes are typically an ASCII string like:

```text
"NAK1989 S-CG-CADVer...."
```

This is just a **provenance / version string** for the editor.

### 4.2 Editor settings (0x60–0x6A)

These bytes are used by S‑CG‑CAD to control how the panel is previewed and converted:

| Offset | Size | Name                         | Meaning (editor‑side) |
|--------|------|------------------------------|------------------------|
| 0x60   | 1    | colormap mode                | Which colormap upload mode to use (32‑colour, 128‑colour, 256‑colour). |
| 0x61   | 1    | Mode 7 enable flag           | 0/1 flag to toggle “Mode 7” label in the UI for this bank. |
| 0x62   | 1    | panel graphics mode          | Small mode selector for panel/map conversion; only bit 1 is used (`& 0x02`). |
| 0x63   | 1    | referenced CGX bank          | `header[0x63] & 0x03` – which CGX bank to sample tiles from in some operations. |
| 0x64   | 1    | panel colormap bank index    | `header[0x64] & 0x03` – “colour bank” selector for preview. |
| 0x65   | 1    | colormap selector A          | Chooses which slice of internal colormap tables to upload. |
| 0x66   | 1    | colormap selector B          | Secondary selector used in some colormap modes. |
| 0x67–68 | 2  | base tile index               | 16‑bit value remembering the last “character tile” you picked. |
| 0x69   | 1    | panel tile width exponent    | Used as `1 << (header[0x69] & 0x1F)` for meta‑tile width. |
| 0x6A   | 1    | panel tile height exponent   | Used as `1 << (header[0x6A] & 0x1F)` for meta‑tile height. |

Other bytes in 0x60–0x6A are preserved but not fully understood; they should be treated as **opaque editor metadata**.

### 4.3 How the header affects preview colours

The editor uses these fields to decide **how to show colours** in the panel preview:

- **colormap mode** chooses:
  - 32‑colour upload
  - 128‑colour upload
  - 256‑colour upload
- **panel colormap bank index** and **selector A/B** choose **which slice** of the internal colormap tables is sent to the display.

Important:  
**PNL does not embed SNES palette data.**  
It stores **“how to interpret pixel indices and which colormap slice to show”**.

Behaviour by mode:

- **32‑colour mode**: uses both selector A and selector B to pick a 32‑colour slice.
- **128‑colour mode**: uses selector A only; selector B is ignored.
- **256‑colour mode**: uses the bank index; selectors are ignored.

### 4.4 How the width/height exponents are used

The width and height exponents do **not** change the on‑disk table size.  
They change how the editor treats a **single tile selection**:

```text
metaWidth  = 1 << (header[0x69] & 0x1F)
metaHeight = 1 << (header[0x6A] & 0x1F)
```

When these are greater than 1, one “logical tile” in the UI is actually a **block of multiple 8×8 tiles** (e.g. 16×16, 32×32). This is how the editor supports:

- 16×16 stamping
- Larger meta‑tiles
- Map conversion based on bigger blocks

…without changing the underlying 8×8 tile storage.

### 4.5 Treating the header safely

If you are writing tools:

- Treat the **entire 0x100‑byte header as opaque**.
- Preserve it byte‑for‑byte when saving.
- Only interpret the fields you truly understand.

This avoids breaking other tools or editor builds that may rely on currently unknown bytes.

---

## 5. Tile attribute table (0x4000 16‑bit words)

Immediately after the header is a table of **0x4000 (16384) 16‑bit words**.

Each word describes **one tile slot** in the panel:

- Which CGX tile to use
- Which palette row to use
- Whether it is flipped
- Whether it has priority

### 5.1 Bit layout (per tile)

Each 16‑bit word is:

```text
bit 15   : vertical flip
bit 14   : horizontal flip
bit 13   : priority
bits 12–10: palette row (0–7)
bits 9–0 : tile id (0–1023)
```

ASCII view:

```text
15  14  13  12 11 10   9 ... 0
+---+---+---+----------+------+
| V | H | P | Palette  | Tile |
+---+---+---+----------+------+
```

For non‑programmers, each tile entry is like a small record:

| Field          | Meaning                                                |
|----------------|--------------------------------------------------------|
| Tile id        | Which 8×8 tile from the CGX to use                     |
| Palette row    | Which row of colours from the COL to use              |
| Horizontal flip| Mirror the tile left/right                             |
| Vertical flip  | Mirror the tile top/bottom                             |
| Priority       | Whether this tile should be drawn “in front” of others |

### 5.2 Endianness

On disk, these 16‑bit words are stored as **big‑endian** (high byte first).  
The S‑CG‑CAD load/save path **does not byte‑swap** them in the PNL I/O functions.

If you are round‑tripping PNL files, you must preserve this.

### 5.3 How the editor uses these bits

Common operations:

- **Erase / clear**  
  - Does **not** change the attribute word directly  
  - Instead clears the “present” flag in the second table (see next section)

- **Palette row change**  
  - Updates the palette bits (12–10) for all **present** tiles in the selected region

- **Priority change**  
  - Updates bit 13 for all **present** tiles in the selected region

- **Flip**  
  - Toggles bit 14 (H‑flip) and/or bit 15 (V‑flip)  
  - Also **swaps tile entries** so the image stays visually consistent

This tells you:  
These bits are meant to be **edited as attributes**, not treated as part of the tile id.

---

## 6. Tile flag table (0x4000 16‑bit words)

After the attribute table is a second table of **0x4000 16‑bit words**.

In traced S‑CG‑CAD code, only **bit 15** of each word is used:

```text
flag = (word2 >> 15) & 0x01
```

This flag is treated as:

- **1** → tile is **present/active**
- **0** → tile is **empty**

The editor uses this flag in many places:

- Panel conversion
- MAP conversion
- “Erase” tools

If the flag is 0, the tile slot is treated as **empty**, even if the attribute word has non‑zero bits.

### 6.1 Unknown lower bits

In real PNL files:

- The **lower 15 bits** of this second word are often **non‑zero**.
- S‑CG‑CAD (in traced builds) does **not** use them.

So they should be treated as:

- **Unknown / reserved metadata**
- Possibly used by other tool builds or workflows

If you write tools, **preserve the full 16‑bit word** even if you only read bit 15.

---

## 7. Panel dimensions and indexing

Although the file stores 0x4000 entries, the editor treats the panel as having a **fixed row stride of 32 tiles**.

Conceptually:

```text
Width  = 32 tiles
Height = 512 tiles
Total  = 32 × 512 = 16384 tiles (0x4000)
```

You can imagine the panel as:

```text
Row 0:   tiles 0   .. 31
Row 1:   tiles 32  .. 63
Row 2:   tiles 64  .. 95
...
Row 511: tiles 16352 .. 16383
```

The index for a tile at `(x, y)` is:

```text
panelTileIndex = y * 32 + x
```

This matches the MAP format, where:

- `panelX` is 5 bits (0–31)
- `panelY` is 9 bits (0–511)

---

## 8. How PNL attributes propagate into SCR

In the S‑CG‑CAD toolchain, **PNL is the source of truth** for per‑tile attributes that end up in SCR.

When a MAP cell resolves to a panel tile, the conversion copies these fields:

| PNL field | SCR meaning                         |
|----------|--------------------------------------|
| vflip    | SCR vertical flip bit (bit 15)       |
| hflip    | SCR horizontal flip bit (bit 14)     |
| priority | SCR priority bit (bit 13)            |
| palRow   | SCR palette row (bits 12–10)         |

The **tile id** bits in SCR (0–9) are filled from:

- The panel tile id, or
- A default value,

depending on the MAP cell’s **attribute source flag** (a MAP‑side detail).

The key idea:  
If you want SCR to match what the artist saw in the panel editor, you must respect the PNL attributes.

---

## 9. Summary for non‑programmers

- A **PNL file** is the saved state of the **panel editor** in S‑CG‑CAD.
- It describes a **32×512 grid** of 8×8 tile slots.
- Each slot:
  - May be **empty** (flag table bit 15 = 0)
  - Or may reference a **CGX tile** with:
    - A palette row
    - Horizontal/vertical flip
    - A priority flag
- The **header** stores:
  - How to interpret colours for preview
  - How big a “logical tile” is (8×8 vs 16×16 etc.)
  - Which CGX/colour banks to use
- PNL does **not** store SNES palette or pixel data; it **references** COL and CGX.
- When converting to SCR, the PNL attributes (flip, priority, palette row) are copied into the SCR tilemap words.

Conceptually:

```text
COL + CGX  →  PNL (panel editor state: which tiles, which colours, which flips)
PNL        →  MAP (where panels are placed)
MAP + PNL  →  SCR-like data (flat tilemap for the SNES)
```

If you are writing tools, the safest approach is:

- Treat the header as **opaque**, preserving all bytes.
- Decode the two 0x4000‑word tables as described.
- Use the first table for tile attributes.
- Use bit 15 of the second table as the “present/empty” flag.
- Preserve all unknown bits for future compatibility.
