using System.Collections.Generic;
using System.Windows.Media;
using SNESassetsWPF.Formats;

namespace SNESassetsWPF.Rendering
{
    /// <summary>
    /// Generates a list of PnlDebugPattern objects from a PnlFile.
    /// Each tile in the panel is treated as a potential pattern origin.
    /// </summary>
    public static class PnlPatternExtractor
    {
        public static PnlDebugPattern[] Extract(PnlFile pnl)
        {
            var list = new List<PnlDebugPattern>();

            int mw = pnl.MetaWidth;
            int mh = pnl.MetaHeight;

            int index = 0;

            for ( int y = 0 ; y < PnlFile.PanelHeight ; y++ )
            {
                for ( int x = 0 ; x < PnlFile.PanelWidth ; x++ )
                {
                    // Base colour for this pattern
                    var baseColor = DebugColors.GetColorForPnlPattern(index);

                    // Fill colour at ~15% opacity
                    var fill = Color.FromArgb(
                        (byte)(255 * 0.15),
                        baseColor.R,
                        baseColor.G,
                        baseColor.B
                    );

                    list.Add( new PnlDebugPattern
                    {
                        PanelX = x ,
                        PanelY = y ,
                        WidthInTiles = mw ,
                        HeightInTiles = mh ,
                        PatternIndex = index ,
                        OutlineColor = baseColor ,
                        FillColor = fill
                    } );

                    index++;
                }
            }

            return list.ToArray();
        }
    }
}
