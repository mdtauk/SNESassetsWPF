using System.Collections.Generic;
using SNESassetsWPF.Rendering.Fonts;

namespace SNESassetsWPF.Rendering
{
    public static class BitmapFonts
    {
        public static readonly Dictionary<char, FontGlyph> bmpFont_1 = Font1x.Glyphs;
        public static readonly Dictionary<char, FontGlyph> bmpFont_2 = Font2x.Glyphs;
        public static readonly Dictionary<char, FontGlyph> bmpFont_3 = Font3x.Glyphs;
        public static readonly Dictionary<char, FontGlyph> bmpFont_4 = Font4x.Glyphs;
    }
}
