using System;
using System.Collections.Generic;
using System.Text;

namespace SNESassetsWPF.Rendering.Fonts
{
    public class FontGlyph
    {
        // Width of the glyph in pixels (4, 6, 10, 12)
        public int Width { get; set; }

        // Height of the glyph in pixels (8, 16, 24, 32)
        public int Height { get; set; }

        // Number of bytes per row (1 or 2)
        public int BytesPerRow { get; set; }

        // Packed monochrome bitmask data
        public byte[] Data { get; set; }
    }
}

