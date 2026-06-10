using SNESassetsWPF.Enums;
using SNESassetsWPF.Models;
using SNESassetsWPF.Services;
using System.Collections.ObjectModel;
using System.Linq;

namespace SNESassetsWPF.ViewModels
{
    /// <summary>
    /// ViewModel for the PNL TreeView.
    /// Displays folders and PNL/PNL.BAK files scanned from a chosen directory.
    /// </summary>
    public class PnlTreeViewModel
    {
        /// <summary>
        /// Root items shown in the TreeView.
        /// Contains FolderNode and FileNode objects.
        /// </summary>
        public ObservableCollection<object> RootItems { get; } = new();

        /// <summary>
        /// The currently selected PNL file node.
        /// </summary>
        public FileNode SelectedFileNode { get; private set; }

        /// <summary>
        /// The folder currently being scanned.
        /// </summary>
        public string CurrentFolder { get; private set; }

        private readonly AssetScannerService _scanner;

        public PnlTreeViewModel()
        {
            _scanner = new AssetScannerService();
        }




        /// <summary>
        /// Loads the folder and populates the TreeView with PNL and PNL backup files.
        /// </summary>
        public void LoadFolder(string folder)
        {
            CurrentFolder = folder;

            RootItems.Clear();

            var scanned = _scanner.Scan(folder);

            var filtered = FilterTree(scanned, FileType.Pnl, FileType.PnlBackup);

            // Only add the root folder if it contains matching files
            if ( filtered.Files.Any() || filtered.SubFolders.Any() )
                RootItems.Add( filtered );
        }





        /// <summary>
        /// Recursively filters a FolderNode tree to include only allowed file types.
        /// </summary>
        private FolderNode FilterTree(FolderNode node , params FileType[] allowed)
        {
            var filtered = new FolderNode
            {
                Name = node.Name,
                FullPath = node.FullPath
            };

            // Keep only allowed file types
            foreach ( var file in node.Files )
            {
                if ( allowed.Contains( file.Type ) )
                    filtered.Files.Add( file );
            }

            // Recursively filter subfolders
            foreach ( var sub in node.SubFolders )
            {
                var child = FilterTree(sub, allowed);

                // Only include folders that contain valid files or subfolders
                if ( child.Files.Any() || child.SubFolders.Any() )
                    filtered.SubFolders.Add( child );
            }

            return filtered;
        }




        /// <summary>
        /// Called by the TreeViewController when a file is selected.
        /// </summary>
        public void SetSelectedFile(FileNode file)
        {
            SelectedFileNode = file;
        }
    }
}
