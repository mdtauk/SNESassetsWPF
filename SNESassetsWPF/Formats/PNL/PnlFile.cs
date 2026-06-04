using System.Collections.Generic;
using SNESassetsWPF.Models;

namespace SNESassetsWPF.Formats
{
    public class PnlFile
    {
        public byte[] Header { get; set; }
        public PnlTile[,] Tiles { get; set; }

        public int MetaWidth { get; set; }
        public int MetaHeight { get; set; }

        public bool Mode7Enabled { get; set; }

        public List<PnlPattern> Patterns { get; set; } = new List<PnlPattern>();

        public const int PanelWidth = 32;
        public const int PanelHeight = 512;

        public int MetaTileCount => ( PanelWidth * PanelHeight ) / ( MetaWidth * MetaHeight );

        /// <summary>
        /// Meta-tiles extracted from the PNL panel.
        /// Each meta-tile is a 2D array of PnlTile.
        /// </summary>
        public PnlTile[][,] MetaTiles { get; set; }

        // ─────────────────────────────────────────────────────────────
        // ADD THIS METHOD
        // ─────────────────────────────────────────────────────────────
        public void BuildMetaTiles()
        {
            int mw = MetaWidth;
            int mh = MetaHeight;

            int tilesX = PanelWidth  / mw;
            int tilesY = PanelHeight / mh;

            MetaTiles = new PnlTile[tilesX * tilesY][,];

            int index = 0;

            for ( int ty = 0 ; ty < tilesY ; ty++ )
            {
                for ( int tx = 0 ; tx < tilesX ; tx++ )
                {
                    var meta = new PnlTile[mw, mh];

                    for ( int y = 0 ; y < mh ; y++ )
                    {
                        for ( int x = 0 ; x < mw ; x++ )
                        {
                            meta[x , y] = Tiles[tx * mw + x , ty * mh + y];
                        }
                    }

                    MetaTiles[index++] = meta;
                }
            }
        }


        public PnlTile GetTile(int x , int y)
        {
            if ( x < 0 || x >= PanelWidth )
                return null;

            if ( y < 0 || y >= PanelHeight )
                return null;

            return Tiles[x , y];
        }

    }
}
