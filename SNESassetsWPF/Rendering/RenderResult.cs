using System;
using System.Collections.Generic;
using System.Text;

namespace SNESassetsWPF.Rendering
{
    public class RenderResult
    {
        public byte[] Buffer { get; set; } = Array.Empty<byte>(); // BGRA32
        public int Width { get; set; }
        public int Height { get; set; }
    }
}

