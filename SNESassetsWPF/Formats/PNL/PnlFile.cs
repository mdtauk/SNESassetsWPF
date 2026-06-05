using SNESassetsWPF.Models;
using System.Windows.Media.Media3D;

namespace SNESassetsWPF.Formats
{
    public class PnlFile
    {
        /// <summary>
        /// Raw header bytes from the PNL file.
        /// Contains width/height exponents, mode flags, etc.
        /// </summary>
        public byte[] Header { get; set; }


        /// <summary>
        /// True if Mode 7 flag is set in the PNL header.
        /// header[0x61]
        /// </summary>
        public bool IsMode7Enabled { get; set; }


        /// <summary>
        /// Meta Width of a single PnlTile, multiples of CgxTiles.
        /// 1 << (header[0x69] & 0x1F)
        /// </summary>
        public int MetaWidth { get; set; }


        /// <summary>
        /// Meta Height of a single PnlTile, multiples of CgxTiles.
        /// 1 << (header[0x6A] & 0x1F)
        /// </summary>
        public int MetaHeight { get; set; }


        /// <summary>
        /// Flat array of PNL tiles (PanelWidth × PanelHeight).
        /// Each tile stores references to Cgx Tiles.
        /// </summary>
        public PnlTile[] PnlTiles { get; set; }




        /// <summary>
        /// Constants used for Renderin PNLs.
        /// </summary>
        public const int PanelWidth = 32;
        public const int PanelHeight = 512;





        /// <summary>
        /// A Pnl File created as we Parse a PNL file
        /// </summary>
        public PnlFile()
        {
            PnlTiles = new PnlTile[PanelWidth * PanelHeight];

            // Allocate header to correct size (0x100)
            Header = new byte[0x100];
        }
    }
}
