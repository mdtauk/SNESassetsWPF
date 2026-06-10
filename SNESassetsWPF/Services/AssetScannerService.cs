using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Diagnostics;
using SNESassetsWPF.Enums;
using SNESassetsWPF.Models;

namespace SNESassetsWPF.Services
{
    public class AssetScannerService
    {
        // All supported SNES file types (easy to extend)
        private static readonly (string ext, FileType type, FileType backupType)[] FileTypes =
        {
            (".col", FileType.Col, FileType.ColBackup),
            (".pnl", FileType.Pnl, FileType.PnlBackup),
            (".map", FileType.Map, FileType.MapBackup),
            (".scr", FileType.Scr, FileType.ScrBackup),
            (".cgx", FileType.Cgx, FileType.CgxBackup)
        };

        // Public entry point
        public FolderNode Scan(string rootFolder)
        {
            Debug.WriteLine( "" );
            Debug.WriteLine( "──────────────────────────────────────────────" );
            Debug.WriteLine( $"[SCAN START] Root: {rootFolder}" );
            Debug.WriteLine( "──────────────────────────────────────────────" );

            var result = ScanFolder(rootFolder, depth: 0);

            Debug.WriteLine( "──────────────────────────────────────────────" );
            Debug.WriteLine( "[SCAN COMPLETE]" );
            Debug.WriteLine( $"Root folders: {result.SubFolders.Count}" );
            Debug.WriteLine( $"Root files:   {result.Files.Count}" );
            Debug.WriteLine( "──────────────────────────────────────────────" );

            return result;
        }

        // Recursive folder scan
        private FolderNode ScanFolder(string folder , int depth)
        {
            string indent = new string(' ', depth * 2);

            Debug.WriteLine( $"{indent}→ Enter folder: {folder}" );

            var node = new FolderNode
            {
                Name = Path.GetFileName(folder),
                FullPath = folder
            };

            // Scan files
            foreach ( var file in Directory.GetFiles( folder ) )
            {
                var type = DetectFileType(file);

                if ( type != FileType.None )
                {
                    Debug.WriteLine( $"{indent}  • File matched: {Path.GetFileName( file )} → {type}" );

                    node.Files.Add( new FileNode
                    {
                        Name = Path.GetFileName( file ) ,
                        FullPath = file ,
                        Type = type
                    } );
                }
                else
                {
                    Debug.WriteLine( $"{indent}    (skip) {Path.GetFileName( file )}" );
                }
            }

            // Scan subfolders
            foreach ( var sub in Directory.GetDirectories( folder ) )
            {
                var child = ScanFolder(sub, depth + 1);

                // Only drop folders that contain NOTHING
                if ( child.Files.Count == 0 && child.SubFolders.Count == 0 )
                {
                    Debug.WriteLine( $"{indent}  Drop empty folder: {sub}" );
                }
                else
                {
                    Debug.WriteLine( $"{indent}  Keep folder: {sub}" );
                    node.SubFolders.Add( child );
                }

            }

            Debug.WriteLine( $"{indent}← Exit folder: {folder}  (Files: {node.Files.Count}, Subfolders: {node.SubFolders.Count})" );

            return node;
        }

        // Unified file type detection
        public FileType DetectFileType(string file)
        {
            string name = Path.GetFileName(file).ToLowerInvariant().Trim();

            // Normalize trailing junk
            name = name.TrimEnd( '.' , ' ' , '~' );

            // Remove Windows duplicate suffixes: "file.col.bak (1)"
            int paren = name.IndexOf(" (");
            if ( paren > 0 )
                name = name.Substring( 0 , paren );

            // Normalize extra extensions
            if ( name.EndsWith( ".bak.old" ) )
                name = name.Replace( ".bak.old" , ".bak" );

            if ( name.EndsWith( ".bak.tmp" ) )
                name = name.Replace( ".bak.tmp" , ".bak" );

            // Match against supported types
            foreach ( var (ext , type , backupType) in FileTypes )
            {
                if ( name.EndsWith( ext + ".bak" ) )
                    return backupType;

                if ( name.EndsWith( ext ) )
                    return type;
            }

            return FileType.None;
        }
    }
}
