# S‑CG‑CAD `.SCR` Format — Complete Specification  
Covers **all known SCR variants** used by S‑CG‑CAD and H‑CG‑CAD.

Variants included:

1. **Normal SCR** — 0x2300 bytes (with interleaved visibility mask)  
2. **F‑Format SCR** — 0x4100 bytes (with expanded per‑tile visibility words)  
3. **NoClearData SCR** — 0x2100 bytes (no visibility mask; all tiles visible)

Each variant is documented separately to avoid confusion.

---

# 1. NORMAL SCR FORMAT (0x2300 bytes)
This is the most common SCR format and the one used by S‑CG‑CAD Ver1.23.

Total size: **0x2300 bytes**

```
+----------------------+ 0x0000
| Block 0 tilemap      | 0x0800 bytes
+----------------------+ 0x0800
| Block 1 tilemap      | 0x0800 bytes
+----------------------+ 0x1000
| Block 2 tilemap      | 0x0800 bytes
+----------------------+ 0x1800
| Block 3 tilemap      | 0x0800 bytes
+----------------------+ 0x2000
| Footer / metadata    | 0x0100 bytes
+----------------------+ 0x2100
| Visibility mask      | 0x0200 bytes
+----------------------+ 0x2300 EOF
```

## 1.1 Tilemap Blocks (0x0000–0x1FFF)

Each block is **32×32 tiles**, 1024 entries, 2 bytes each.

### Tile entry bit layout (Normal format)

```
Bits  0–9  : Tile index (0–1023)
Bits 10–12 : Palette row (0–7)
Bit     13 : Priority flag
Bit     14 : X flip
Bit     15 : Y flip
```

Tile order is row‑major (0..31 rows × 0..31 columns).

---

## 1.2 Footer / Metadata (0x2000–0x20FF)

```
0x2000–0x200F : "NAK1989 S-CG-CAD"
0x2010–0x2017 : Version string
0x2018–0x201F : Date string
```

Metadata fields (offsets relative to 0x2000):

```
0x40 : bitmode
0x41 : mode7
0x42 : scr_mode (0 = 8×8, 1 = 16×16)
0x43 : chr_bank
0x44 : col_bank
0x45 : col_half
0x46 : col_cell
0x47–0x48 : clr_chr_no
```

---

## 1.3 Visibility Mask (0x2100–0x22FF) — **Corrected**

This format **does contain visibility data**.

- 4 screens × 1024 bits each  
- 1024 bits = 128 bytes = 0x80 bytes per screen  
- Total = 0x200 bytes

### Physical storage pattern (interleaved)

For screen `s` (0–3), byte `j` (0–0x7F) is stored at:

```
0x2100
+ ((s & 2) * 0x80)
+ ((s & 1) * 4)
+ (j % 4)
+ ((j / 4) * 8)
```

### Bit order (reverse)

```
bit[i] = ((byte[i / 8] << (i % 8)) & 0x80) != 0
```

### Meaning

```
1 = tile visible
0 = tile hidden
```

This mask is used by S‑CG‑CAD and H‑CG‑CAD to hide tiles.

---

# 2. F‑FORMAT SCR (0x4100 bytes)
This is an alternate SCR format used by some versions of S‑CG‑CAD.

Total size: **0x4100 bytes**

```
+----------------------+ 0x0000
| Block 0 tilemap      | 0x0800 bytes
+----------------------+ 0x0800
| Block 1 tilemap      | 0x0800 bytes
+----------------------+ 0x1000
| Block 2 tilemap      | 0x0800 bytes
+----------------------+ 0x1800
| Block 3 tilemap      | 0x0800 bytes
+----------------------+ 0x2000
| Footer / metadata    | 0x0100 bytes
+----------------------+ 0x2100
| F‑format visibilit y | 0x2000 bytes
+----------------------+ 0x4100 EOF
```

## 2.1 Tilemap Blocks
Identical to Normal format.

## 2.2 Footer
Identical to Normal format.

## 2.3 F‑Format Visibility (0x2100–0x40FF)

This format uses **2 bytes per tile** for visibility.

For each tile:

```
visibility = (tileWord >> 15) & 1
```

Meaning:

```
1 = visible
0 = hidden
```

Tile order is linear, no interleaving, no bit‑packing.

---

# 3. NOCLEARDATA SCR (0x2100 bytes)
This is the smallest SCR format.

Total size: **0x2100 bytes**

```
+----------------------+ 0x0000
| Block 0 tilemap      | 0x0800 bytes
+----------------------+ 0x0800
| Block 1 tilemap      | 0x0800 bytes
+----------------------+ 0x1000
| Block 2 tilemap      | 0x0800 bytes
+----------------------+ 0x1800
| Block 3 tilemap      | 0x0800 bytes
+----------------------+ 0x2000
| Footer / metadata    | 0x0100 bytes
+----------------------+ 0x2100 EOF
```

## 3.1 Tilemap Blocks
Same as Normal format.

## 3.2 Footer
Same as Normal format.

## 3.3 Visibility
**No visibility data exists in this format.**

All tiles are implicitly:

```
visible = true
```

---

# 4. Summary of Differences

| Variant        | Size     | Visibility Format                          | Notes |
|----------------|----------|---------------------------------------------|-------|
| **Normal**     | 0x2300   | 4×0x80 bytes, interleaved, reverse bits     | Most common; used by S‑CG‑CAD Ver1.23 |
| **F‑Format**   | 0x4100   | 2 bytes per tile (bit 15 = visible)         | Larger; no interleaving |
| **NoClearData**| 0x2100   | None (all tiles visible)                    | Simplest format |

---

# 5. Variant Detection

You can reliably detect the variant by file size:

```
0x2300 → Normal
0x4100 → F‑Format
0x2100 → NoClearData
```

---

# 6. Rendering Rules (All Variants)

Regardless of variant:

- Tile index selects CGX tile  
- Palette row selects COL palette row  
- col_half selects palette half  
- scr_mode selects tile size  
- Flip bits apply normally  
- Visibility comes from the variant‑specific mask  

---

This is the complete, authoritative SCR specification.
