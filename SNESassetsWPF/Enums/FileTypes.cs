using System;
using System.Collections.Generic;
using System.Text;




namespace SNESassetsWPF.Enums
{
    /// <summary>
    /// Represents the type of file detected during scanning.
    /// This enum is intentionally simple and only classifies file types.
    /// It is NOT a model — it is a supporting domain type.
    /// </summary>
    public enum FileType
    {
    None,

        Col,
        ColBackup,

        Pnl,
        PnlBackup,

        Map,
        MapBackup,

        Scr,
        ScrBackup,

        Cgx,
        CgxBackup
    }
}