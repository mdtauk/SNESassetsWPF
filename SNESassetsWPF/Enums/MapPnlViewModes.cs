using System;
using System.Collections.Generic;
using System.Text;

namespace SNESassetsWPF.Enums
{
    public enum MapPnlViewModes
    {
        Auto,       // Default: choose based on loaded files
        PnlDebug,   // PNL debug rectangles
        MapDebug,   // MAP debug rectangles
        MapFull,    // MAP + PNL + CGX + COL
        PnlFull     // Future: PNL full render using CGX + COL
    }
}
