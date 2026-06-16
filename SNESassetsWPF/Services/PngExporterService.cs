using Microsoft.Win32;
using SNESassetsWPF.Enums;
using SNESassetsWPF.Formats;
using SNESassetsWPF.Models;
using SNESassetsWPF.ViewModels;
using System;
using System.IO;

namespace SNESassetsWPF.Services
{
    public class PngExportService
    {
        //
        // ─────────────────────────────────────────────────────────────
        //  COL EXPORT
        // ─────────────────────────────────────────────────────────────
        //
        public void ExportCol(PaletteViewModel view, LoadedAsset<ColFile> col)
        {
            if ( col == null )
                return;

            string exportName = BuildColExportName(
                colPath: col.Path
            );

            var dlg = new SaveFileDialog
            {
                Filter = "PNG Image|*.png",
                FileName = exportName
            };

            if ( dlg.ShowDialog() == true )
                view.ExportPng( dlg.FileName );
        }

        //
        // ─────────────────────────────────────────────────────────────
        //  CGX EXPORT
        // ─────────────────────────────────────────────────────────────
        //
        public void ExportCgx(CgxViewerViewModel viewer , LoadedAsset<CgxFile> cgx , LoadedAsset<ColFile> col)
        {
            if ( viewer == null || col == null )
                return;

            string exportName = BuildCgxExportName(
                cgxPath: cgx.Path,
                colPath: col.Path,
                zoom: viewer.ZoomLevel,
                forceSingleRow: viewer.Palette?.ForceSingleRow ?? false,
                selectedRow: viewer.Palette?.SelectedPaletteRowIndex ?? -1
            );

            var dlg = new SaveFileDialog
            {
                Filter = "PNG Image|*.png",
                FileName = exportName
            };

            if ( dlg.ShowDialog() == true )
                viewer.ExportPng( dlg.FileName , viewer.ZoomLevel );
        }


        //
        // ─────────────────────────────────────────────────────────────
        //  SCR EXPORT
        // ─────────────────────────────────────────────────────────────
        //
        public void ExportScr(ScrViewerViewModel viewer , LoadedAsset<ScrFile> scr , LoadedAsset<CgxFile> cgx , LoadedAsset<ColFile> col)
        {
            if ( viewer == null || col == null )
                return;

            string exportName = BuildScrExportName(
                scrPath: scr.Path,
                cgxPath: cgx.Path,
                colPath: col.Path,
                zoom: viewer.ZoomLevel,
                debugMode: viewer.SelectedDebugMode
            );

            var dlg = new SaveFileDialog
            {
                Filter = "PNG Image|*.png",
                FileName = exportName
            };

            if ( dlg.ShowDialog() == true )
                viewer.ExportPng( dlg.FileName, viewer.ZoomLevel );
        }


        //
        // ─────────────────────────────────────────────────────────────
        //  PNL EXPORT
        // ─────────────────────────────────────────────────────────────
        //
        public void ExportPnl(
            MapPnlViewerViewModel viewer ,
            LoadedAsset<PnlFile> pnl ,
            LoadedAsset<CgxFile> cgx ,
            LoadedAsset<ColFile> col)
        {
            if ( viewer == null || pnl == null )
                return;

            string exportName = BuildPnlExportName(
                pnlPath: pnl.Path,
                cgxPath: cgx?.Path,
                colPath: col?.Path,
                zoom: viewer.ZoomLevel
            );

            var dlg = new SaveFileDialog
            {
                Filter = "PNG Image|*.png",
                FileName = exportName
            };

            if ( dlg.ShowDialog() == true )
                viewer.ExportMapPng( dlg.FileName );
        }




        //
        // ─────────────────────────────────────────────────────────────
        //  MAP EXPORT
        // ─────────────────────────────────────────────────────────────
        //
        public void ExportMap(MapPnlViewerViewModel viewer , LoadedAsset<PnlFile> pnl , LoadedAsset<MapFile> map)
        {
            if ( viewer == null || pnl == null || map == null )
                return;

            string exportName =
                $"PNL_{Path.GetFileNameWithoutExtension(pnl.Path)}_" +
                $"MAP_{Path.GetFileNameWithoutExtension(map.Path)}_" +
                $"{viewer.ZoomLevel}x.png";

            var dlg = new SaveFileDialog
            {
                Filter = "PNG Image|*.png",
                FileName = exportName
            };

            if ( dlg.ShowDialog() == true )
                viewer.ExportMapPng( dlg.FileName );
        }




        //
        // ─────────────────────────────────────────────────────────────
        //  FILENAME HELPERS
        // ─────────────────────────────────────────────────────────────
        //
        private static string StripAllExtensions(string fileName)
        {
            string name = fileName;

            while ( Path.HasExtension( name ) )
                name = Path.GetFileNameWithoutExtension( name );

            return name;
        }

        private static string NormalizeBaseName(string path)
        {
            string fileName = Path.GetFileName(path) ?? "";
            string baseName = StripAllExtensions(fileName);

            // Preserve .bak meaning
            if ( fileName.EndsWith( ".bak" , StringComparison.OrdinalIgnoreCase ) )
                baseName += "_bak";

            return baseName;
        }




        private string BuildColExportName(string colPath)
        {
            string col = NormalizeBaseName(colPath) ?? "unknown_COL";

            string name = $"COL_{col}";

            return name;
        }


        private string BuildCgxExportName(
            string cgxPath ,
            string colPath ,
            int zoom ,
            bool forceSingleRow ,
            int selectedRow)
        {
            string cgx = NormalizeBaseName(cgxPath) ?? "unknown_CGX";
            string col = NormalizeBaseName(colPath) ?? "unknown_COL";

            string name = $"CGX_{cgx}_COL_{col}";

            if ( forceSingleRow && selectedRow >= 0 )
                name += $"_ROW_{selectedRow:X1}";

            name += $"_{zoom}x.png";
            return name;
        }


        private string BuildScrExportName(
            string scrPath ,
            string cgxPath ,
            string colPath ,
            int zoom ,
            ScrDebugMode debugMode)
        {
            string scr = NormalizeBaseName(scrPath) ?? "unknown_SCR";
            string cgx = NormalizeBaseName(cgxPath) ?? "unknown_CGX";
            string col = NormalizeBaseName(colPath) ?? "unknown_COL";

            string name = $"SCR_{scr}_CGX_{cgx}_COL_{col}";

            if ( debugMode != ScrDebugMode.None )
                name += $"_DBG_{debugMode}";

            name += $"_{zoom}x.png";
            return name;
        }



        private string BuildPnlExportName(
            string pnlPath ,
            string cgxPath ,
            string colPath ,
            int zoom)
        {
            string fileName = Path.GetFileName(pnlPath) ?? "unknown_PNL";

            // Strip all extensions
            string baseName = StripAllExtensions(fileName);

            // Detect if original filename ended with .bak (case-insensitive)
            bool isBackup = fileName.EndsWith(".bak", StringComparison.OrdinalIgnoreCase);

            if ( isBackup )
                baseName += "_bak";

            // CGX + COL present?
            if ( !string.IsNullOrEmpty( cgxPath ) && !string.IsNullOrEmpty( colPath ) )
            {
                string cgx = NormalizeBaseName(cgxPath) ?? "unknown_CGX";
                string col = NormalizeBaseName(colPath) ?? "unknown_COL";

                return $"PNL_{baseName}_CGX_{cgx}_COL_{col}_{zoom}x.png";
            }

            return $"PNL_{baseName}_{zoom}x.png";
        }

    }
}
