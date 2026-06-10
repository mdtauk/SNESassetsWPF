using SNESassetsWPF.Enums;
using SNESassetsWPF.Models;
using SNESassetsWPF.Services;
using System.Collections.ObjectModel;
using System.Linq;

namespace SNESassetsWPF.ViewModels
{
    public class CgxTreeViewModel
    {
        public ObservableCollection<object> RootItems { get; } = new();
        public FileNode SelectedFileNode { get; private set; }
        public FileNode BuiltInFileNode { get; private set; }

        private readonly BuiltInAssetService _builtIn;
        private readonly AssetScannerService _scanner;

        public string CurrentFolder { get; private set; }

        public CgxTreeViewModel()
        {
            _builtIn = new BuiltInAssetService();
            _scanner = new AssetScannerService();
        }

        public void LoadFolder(string folder)
        {
            CurrentFolder = folder;
            var builtIn = BuiltInFileNode; // preserve reference

            RootItems.Clear();

            // 1. Add built-in CGX at top
            if ( builtIn != null )
                RootItems.Add( builtIn );

            // 2. Scan folder
            var scanned = _scanner.Scan(folder);

            // 3. Filter to CGX + CGX.BAK
            var filtered = FilterTree(scanned, FileType.Cgx, FileType.CgxBackup);

            // 4. Only add the root folder if it contains matching files
            if ( filtered.Files.Any() || filtered.SubFolders.Any() )
                RootItems.Add( filtered );
        }

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

        public void LoadBuiltIn()
        {
            RootItems.Clear();

            BuiltInFileNode = _builtIn.GetBuiltInCgxFile();

            RootItems.Add( BuiltInFileNode );
        }

        public void SelectBuiltIn()
        {
            SelectedFileNode = BuiltInFileNode;
        }
    }
}
