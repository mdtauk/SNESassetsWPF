# OBJ / OBX format — object / sprite definition and animation (SNES / S-CG-CAD)

This document describes the OBJ and OBX (object / object extended) file formats
used in S-CG-CAD for defining sprite graphics, animation frames, and behavior data.
Non-expert programmers can use this to understand the on-disk layout and implement
a sprite viewer or animator.

## Summary

- **OBJ**: Static sprite definition (graphics, hitbox, palette reference)
- **OBX**: Extended sprite with animation frames, state transitions, and behavior triggers
- **Typical size**: OBJ 0x200–0x1000 bytes; OBX 0x1000–0x10000+ bytes (many frames)
- **Structure**: Header + sprite canvas / tile indices + hitbox definitions + (OBX only) animation frame list
- **Endianness**: 16-bit words are stored big-endian on disk
- **Content**: CGX tile references, palette bank, animation sequence data

## File layout — OBJ (static sprite)

| Offset | Size | Meaning |
| ---: | ---: | --- |
| 0x0000 - 0x00FF | 0x100 | object header and metadata |
| 0x0100 - 0x011F | 0x20 | hitbox definitions (up to 4 boxes) |
| 0x0120 - ... | variable | tile index map (canvas layout) |

ASCII overview:

```
OBJ file (static):

+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+
| 0x00    Object header                        | 0xFF
| Sprite dimensions, graphics mode, palette    |
+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+
| 0x0100  Hitbox definitions                   | 0x011F
| Up to 4 hitbox entries (8 bytes each)        |
+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+
| 0x0120  Tile index map (variable)            |
| Canvas: which CGX tiles are used where       |
+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+
```

## File layout — OBX (animated sprite)

| Offset | Size | Meaning |
| ---: | ---: | --- |
| 0x0000 - 0x00FF | 0x100 | object header (same as OBJ) |
| 0x0100 - 0x011F | 0x20 | hitbox definitions (up to 4 boxes) |
| 0x0120 - variable | variable | animation sequence metadata |
| variable - ... | variable | animation frame list (tile maps + duration) |

## Object header (0x100 bytes)

| Offset | Size | Meaning | Notes |
| ---: | ---: | --- | --- |
| 0x00 - 0x1F | 0x20 | ASCII provenance string | Often begins with `NAK1989 S-CG-CADVer...` |
| 0x40 | 1 | sprite width tiles | Horizontal extent in 8×8 tiles (typically 2–4) |
| 0x41 | 1 | sprite height tiles | Vertical extent in 8×8 tiles (typically 2–4) |
| 0x42 | 1 | graphics mode | 0 = 2bpp, 1 = 4bpp, 2 = 8bpp (color mode) |
| 0x43 | 1 | referenced CGX bank | `header[0x43] & 0x03` — which CGX to sample from |
| 0x44 | 1 | palette bank | `header[0x44] & 0x03` — color bank index (0–3) |
| 0x45 | 1 | animation enabled | 0 = static (OBJ), 1 = animated (OBX) |
| 0x46 - 0x47 | 2 | total animation frames | 16-bit big-endian frame count (if animated) |
| 0x48 - 0x49 | 2 | current frame index | 16-bit big-endian (used by editor UI) |
| 0x4A | 1 | frame speed | Delay in ticks between frame advances (1–255; 0 = auto) |
| 0x4B | 1 | loop mode | 0 = loop, 1 = ping-pong, 2 = one-shot |
| 0x60 | 1 | collision type | 0 = none, 1 = hurtbox, 2 = hitbox, 3 = both |
| 0x61 | 1 | hitbox count | Number of active hitbox definitions (1–4) |
| 0x62 | 1 | collision priority | Higher value = hits higher-priority targets first |
| 0x63 | 1 | reflected tile mode | 0 = standard, 1 = reserved/flipped variants available |

Decode example (C#-like pseudocode):

```csharp
int spriteWidthTiles = header[0x40];
int spriteHeightTiles = header[0x41];
int graphicsMode = header[0x42];  // 0=2bpp, 1=4bpp, 2=8bpp
int cgxBank = header[0x43] & 0x03;
int paletteBank = header[0x44] & 0x03;
bool isAnimated = header[0x45] != 0;
int totalFrames = ReadBigEndian16(0x46);
int frameSpeed = header[0x4A];
```

## Hitbox definitions (0x20 bytes, up to 4 entries)

Located at offset 0x0100. Each hitbox is 8 bytes:

| Offset within block | Size | Meaning | Notes |
| ---: | ---: | --- | --- |
| +0x00 | 1 | hitbox type | 0 = none/disabled, 1 = damage, 2 = pickup, 3 = sensor |
| +0x01 | 1 | hitbox priority | 0–255 (higher = checked first) |
| +0x02 | 1 | X offset (tiles) | Horizontal position relative to sprite origin (−8 to +8) |
| +0x03 | 1 | Y offset (tiles) | Vertical position relative to sprite origin (−8 to +8) |
| +0x04 | 1 | width (tiles) | Horizontal extent (1–4 tiles) |
| +0x05 | 1 | height (tiles) | Vertical extent (1–4 tiles) |
| +0x06 - 0x07 | 2 | reserved | typically 0x0000 |

Decode example:

```csharp
int baseOffset = 0x0100;
for (int i = 0; i < hitboxCount; i++) {
  int offset = baseOffset + (i * 8);
  byte type = buffer[offset + 0x00];
  int xOfs = (sbyte)buffer[offset + 0x02];
  int yOfs = (sbyte)buffer[offset + 0x03];
  int width = buffer[offset + 0x04];
  int height = buffer[offset + 0x05];

  // Hitbox pixel bounds: (xOfs*8, yOfs*8) to ((xOfs+width)*8, (yOfs+height)*8)
}
```

## Tile index map (OBJ — static)

Starting at offset 0x0120, a grid of 16-bit big-endian tile references.
Grid dimensions are `spriteWidthTiles × spriteHeightTiles`.

Each word:

| Bits | Mask | Meaning |
| ---: | ---: | --- |
| 15 | 0x8000 | vertical flip (vflip) |
| 14 | 0x4000 | horizontal flip (hflip) |
| 13 | 0x2000 | priority (pri) |
| 12 | 0x1000 | reserved / extended bit |
| 11-0 | 0x0FFF | CGX tile ID (0–4095) |

Decode example:

```csharp
int mapOffset = 0x0120;
int stride = spriteWidthTiles;

for (int y = 0; y < spriteHeightTiles; y++) {
  for (int x = 0; x < spriteWidthTiles; x++) {
    int tileIndex = y * stride + x;
    ushort word = ReadBigEndian16(mapOffset + tileIndex * 2);

    bool vflip = (word >> 15) & 1 == 1;
    bool hflip = (word >> 14) & 1 == 1;
    int tileId = word & 0x0FFF;

    // Render tile at (x*8, y*8) with flips
  }
}
```

## Animation frame list (OBX only)

In OBX files, animation frames follow the hitbox definitions.
The frame list structure varies slightly, but typically:

**Frame metadata block** (starting ~0x0180):

| Offset | Size | Meaning | Notes |
| ---: | ---: | --- | --- |
| +0x00 - 0x01 | 2 | frame 0 offset | 16-bit big-endian byte offset to frame 0 tile map |
| +0x02 - 0x03 | 2 | frame 1 offset | offset to frame 1 |
| ... | ... | ... | one entry per frame |

Offset table layout (assuming 4-frame animation):

```csharp
// Frame offset table at ~0x0180 (after hitbox definitions at 0x0100-0x011F)
int frameOffsetTableBase = 0x0120;  // or 0x0180 depending on implementation

for (int i = 0; i < totalFrames; i++) {
  ushort offset = ReadBigEndian16(buffer, frameOffsetTableBase + (i * 2));
  // offset points to frame data (tile map) within file
}
```

**Frame data blocks** (variable offsets):

Each frame block is structured as a tile index map (like OBJ canvas),
plus an optional duration/timing entry:

```csharp
for (int frameIdx = 0; frameIdx < totalFrames; frameIdx++) {
  // Get frame offset from offset table
  ushort frameOffset = ReadBigEndian16(buffer, frameOffsetTableBase + (frameIdx * 2));

  // Read tile map (spriteWidthTiles × spriteHeightTiles entries, 2 bytes each)
  int mapSizeInBytes = spriteWidthTiles * spriteHeightTiles * 2;

  for (int y = 0; y < spriteHeightTiles; y++) {
    for (int x = 0; x < spriteWidthTiles; x++) {
      int tileIndex = y * spriteWidthTiles + x;
      ushort tileWord = ReadBigEndian16(buffer, frameOffset + (tileIndex * 2));

      bool vflip = (tileWord >> 15) & 1 == 1;
      bool hflip = (tileWord >> 14) & 1 == 1;
      int tileId = tileWord & 0x0FFF;

      // Render tile at (x*8, y*8) with flips
    }
  }

  // Optional: read frame duration (1 byte, typically after tile map)
  // byte frameDuration = buffer[frameOffset + mapSizeInBytes];
}
```

Complete OBX loader pseudocode:

```csharp
public class AnimationFrame {
  public ushort[] TileWords { get; set; }  // spriteWidth × spriteHeight words
  public byte Duration { get; set; }  // ticks per frame (0 = auto)
}

public List<AnimationFrame> LoadOBXFrames(byte[] buffer, int spriteWidth, int spriteHeight, int totalFrames) {
  List<AnimationFrame> frames = new List<AnimationFrame>();

  // Frame offset table typically starts at 0x0120 or 0x0180
  int frameOffsetTableBase = 0x0120;  // depends on implementation

  for (int frameIdx = 0; frameIdx < totalFrames; frameIdx++) {
    ushort frameOffset = ReadBigEndian16(buffer, frameOffsetTableBase + (frameIdx * 2));

    AnimationFrame frame = new AnimationFrame();
    int mapSizeInWords = spriteWidth * spriteHeight;
    frame.TileWords = new ushort[mapSizeInWords];

    // Read tile words
    for (int i = 0; i < mapSizeInWords; i++) {
      int offset = frameOffset + (i * 2);
      if (offset + 1 < buffer.Length) {
        frame.TileWords[i] = ReadBigEndian16(buffer, offset);
      }
    }

    // Read duration if present
    int durationOffset = frameOffset + (mapSizeInWords * 2);
    if (durationOffset < buffer.Length) {
      frame.Duration = buffer[durationOffset];
    }

    frames.Add(frame);
  }

  return frames;
}
```

## Animation playback logic

Example renderer for animating OBX sprites:

```csharp
public class SpriteAnimator {
  private List<AnimationFrame> frames;
  private int currentFrameIndex = 0;
  private int frameCounter = 0;
  private byte frameSpeed = 1;  // ticks per frame
  private byte loopMode = 0;    // 0 = loop, 1 = ping-pong, 2 = one-shot

  public void Advance() {
    frameCounter++;
    if (frameCounter >= frameSpeed) {
      frameCounter = 0;

      if (loopMode == 0) {  // loop
        currentFrameIndex = (currentFrameIndex + 1) % frames.Count;
      } else if (loopMode == 1) {  // ping-pong
        // Simplified ping-pong (advanced version tracks direction)
        currentFrameIndex = (currentFrameIndex + 1) % (frames.Count * 2 - 2);
        if (currentFrameIndex >= frames.Count) {
          currentFrameIndex = frames.Count * 2 - 2 - currentFrameIndex;
        }
      } else if (loopMode == 2) {  // one-shot
        if (currentFrameIndex < frames.Count - 1) {
          currentFrameIndex++;
        }
      }
    }
  }

  public AnimationFrame GetCurrentFrame() => frames[currentFrameIndex];
}
```

## Animation state machine (OBX advanced feature)

Some OBX files include state transition tables. This is rarely used in early projects
but may appear as:

- **State entry table** (offset varies): list of animation sequences, each with entry/exit frames
- **Trigger table**: events that transition between sequences (e.g., "on impact" → play "hurt" sequence)

Check the header for a state count field (typically at 0x70 or later) to detect this.

Example state machine structure (if present):

| Offset | Size | Meaning |
| ---: | ---: | --- |
| 0x70 | 1 | state count | number of animation states (0 = no state machine) |
| 0x71+ | variable | state definitions | each state has entry/exit frame range and transition info |

## Practical sprite viewer usage

To display or animate an OBJ / OBX:

1. Read the 0x100 header and extract dimensions, graphics mode, palette bank, CGX reference.
2. Read hitbox definitions (if `collision type != 0`).
3. For OBJ (static):
   - Read the tile index map at 0x0120.
   - For each tile entry, look up the CGX tile and render with flip + palette.
4. For OBX (animated):
   - Read the animation frame offset table.
   - Load all frame tile maps into memory.
   - On each game frame (or editor tick), read the current frame tile map and render.
   - Advance frame counter based on `frameSpeed`; handle loop mode (loop, ping-pong, one-shot).

ASCII sprite grid example (2×2 sprite, 4 frames):

```
Frame 0:              Frame 1:              Frame 2:              Frame 3:
+-------+-------+     +-------+-------+     +-------+-------+     +-------+-------+
| tile0 | tile1 |     | tileA | tileB |     | tileX | tileY |     | tileP | tileQ |
+-------+-------+     +-------+-------+     +-------+-------+     +-------+-------+
| tile2 | tile3 |     | tileC | tileD |     | tileZ | tileW |     | tileR | tileS |
+-------+-------+     +-------+-------+     +-------+-------+     +-------+-------+

Animation plays: 0 → 1 → 2 → 3 → (loop to 0)
```

## File validation

When reading an OBJ / OBX:

1. **Check file size**: minimum 0x200 bytes (header + hitbox + minimal tile map)
2. **Validate header**: check ASCII provenance string at 0x00-0x1F
3. **Validate dimensions**: sprite width/height should be 1-8 tiles each
4. **Validate graphics mode**: 0–2 (2bpp, 4bpp, 8bpp)
5. **For OBX**: validate frame offsets point within file bounds; check frame count > 0

## Common variations

- **Compressed tile maps**: Rare; some OBX files use RLE-encoded frame data for smaller file size.
- **Shared hitboxes**: Some OBX files define hitboxes per-frame (variable hitbox count per frame); check header 0x61 dynamically.
- **Multiple animations**: Advanced OBX may contain several independent animation sequences (e.g., "idle", "walk", "jump"), each with its own frame list and entry point in frame table.
- **Rotation / scaling**: Rarely used in early CAD projects; ignore if no "rotation" header field.

## Notes and edge cases

- Tile IDs reference the CGX bank specified in header 0x43.
- Palette bank determines which COL row(s) are used (see COL and palette entry docs).
- Flip bits are applied per-tile, allowing complex sprite composition (e.g., asymmetric limbs).
- Frame speed 0 is reserved; typically defaults to 1 tick per frame in game engines.
- OBX files with many frames (100+) can be large; consider memory usage when loading into editor.
- Always preserve the 0x100 header and hitbox block when round-tripping; unknown bytes may be used by other CAD builds.

---

Document prepared to aid implementing a sprite viewer and animator in this project.
