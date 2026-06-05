using System;
using System.Collections.Generic;
using System.Text;

namespace SNESassetsWPF.Models
{
    /// <summary>
    /// One of the (usually 64 x 64 ) Cells in a MAP file.
    /// </summary>
    public class MapCell
    {
        /// <summary>
        /// Cell x position within the Map grid.
        /// </summary>
        public int CellPositionX { get; set; }


        /// <summary>
        /// Cell y position within the Map grid.
        /// </summary>
        public int CellPositionY { get; set; }


        /// <summary>
        /// Cell contents x position within the PNL.
        /// </summary>
        public int PnlX { get; set; }


        /// <summary>
        /// Cell contents y position within the PNL.
        /// </summary>
        public int PnlY { get; set; }


        /// <summary>
        /// Stores a raw value as read from the MAP file.
        /// </summary>
        public ushort RawValue { get; set; }


        /// <summary>
        /// Bool used when converting MAP + PNL to SCR
        /// </summary>
        public bool UsePanelAttributes { get; set; }
    }
}
