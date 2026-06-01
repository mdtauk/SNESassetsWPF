using System.Collections.Generic;


namespace SNESassetsWPF.Rendering.Fonts
{
    public static class Font1x
    {
        public static readonly Dictionary<char, FontGlyph> Glyphs = new()
        {
            { '0', new FontGlyph { Width = 4, Height = 8, BytesPerRow = 1, Data = new byte[] { 0x00, 0x60, 0x90, 0xD0, 0xB0, 0x90, 0x60, 0x00 } } },
            { '1', new FontGlyph { Width = 4, Height = 8, BytesPerRow = 1, Data = new byte[] { 0x00, 0x60, 0x20, 0x20, 0x20, 0x20, 0x70, 0x00 } } },
            { '2', new FontGlyph { Width = 4, Height = 8, BytesPerRow = 1, Data = new byte[] { 0x00, 0x60, 0x90, 0x10, 0x60, 0x80, 0xF0, 0x00 } } },
            { '3', new FontGlyph { Width = 4, Height = 8, BytesPerRow = 1, Data = new byte[] { 0x00, 0xE0, 0x10, 0x60, 0x10, 0x10, 0xE0, 0x00 } } },
            { '4', new FontGlyph { Width = 4, Height = 8, BytesPerRow = 1, Data = new byte[] { 0x00, 0x20, 0x60, 0xA0, 0xA0, 0xF0, 0x20, 0x00 } } },
            { '5', new FontGlyph { Width = 4, Height = 8, BytesPerRow = 1, Data = new byte[] { 0x00, 0xF0, 0x80, 0xE0, 0x10, 0x90, 0x60, 0x00 } } },
            { '6', new FontGlyph { Width = 4, Height = 8, BytesPerRow = 1, Data = new byte[] { 0x00, 0x60, 0x80, 0xE0, 0x90, 0x90, 0x60, 0x00 } } },
            { '7', new FontGlyph { Width = 4, Height = 8, BytesPerRow = 1, Data = new byte[] { 0x00, 0xF0, 0x10, 0x10, 0x20, 0x40, 0x40, 0x00 } } },
            { '8', new FontGlyph { Width = 4, Height = 8, BytesPerRow = 1, Data = new byte[] { 0x00, 0x60, 0x90, 0x60, 0x90, 0x90, 0x60, 0x00 } } },
            { '9', new FontGlyph { Width = 4, Height = 8, BytesPerRow = 1, Data = new byte[] { 0x00, 0x60, 0x90, 0x90, 0x70, 0x10, 0x60, 0x00 } } },
            { 'A', new FontGlyph { Width = 4, Height = 8, BytesPerRow = 1, Data = new byte[] { 0x00, 0x60, 0x90, 0x90, 0xF0, 0x90, 0x90, 0x00 } } },
            { 'B', new FontGlyph { Width = 4, Height = 8, BytesPerRow = 1, Data = new byte[] { 0x00, 0xE0, 0x90, 0xE0, 0x90, 0x90, 0xE0, 0x00 } } },
            { 'C', new FontGlyph { Width = 4, Height = 8, BytesPerRow = 1, Data = new byte[] { 0x00, 0x60, 0x90, 0x80, 0x80, 0x90, 0x60, 0x00 } } },
            { 'D', new FontGlyph { Width = 4, Height = 8, BytesPerRow = 1, Data = new byte[] { 0x00, 0xE0, 0x90, 0x90, 0x90, 0x90, 0xE0, 0x00 } } },
            { 'E', new FontGlyph { Width = 4, Height = 8, BytesPerRow = 1, Data = new byte[] { 0x00, 0xF0, 0x80, 0xE0, 0x80, 0x80, 0xF0, 0x00 } } },
            { 'F', new FontGlyph { Width = 4, Height = 8, BytesPerRow = 1, Data = new byte[] { 0x00, 0xF0, 0x80, 0x80, 0xE0, 0x80, 0x80, 0x00 } } },
        };
    }
}