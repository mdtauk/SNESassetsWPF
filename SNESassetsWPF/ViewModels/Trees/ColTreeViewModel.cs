using SNESassetsWPF.Enums;
using SNESassetsWPF.Models;
using SNESassetsWPF.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;




namespace SNESassetsWPF.ViewModels
{
    public class ColTreeViewModel : ViewModelBase
    {
        public ObservableCollection<object> RootItems { get; } = new();
        public FileNode SelectedFileNode { get; private set; }
        public FileNode BuiltInFileNode { get; private set; }

        private readonly BuiltInAssetService _builtIn;
        private readonly AssetScannerService _scanner;

        public string CurrentFolder { get; private set; }


        public ColTreeViewModel()
        {
            _builtIn = new BuiltInAssetService();
            _scanner = new AssetScannerService();
        }

        public void LoadBuiltIn()
        {
            RootItems.Clear();

            BuiltInFileNode = _builtIn.GetBuiltInColFile();

            RootItems.Add( BuiltInFileNode );
        }


        public void SelectBuiltIn()
        {
            SelectedFileNode = BuiltInFileNode;
        }

        public void LoadFolder(string folder)
        {
            CurrentFolder = folder;
            var builtIn = BuiltInFileNode; // preserve reference

            RootItems.Clear();

            // re-add built-in at top
            if ( builtIn != null )
                RootItems.Add( builtIn );

            var scanned = _scanner.Scan(folder);

            var filtered = FilterTree(scanned, FileType.Col, FileType.ColBackup);

            foreach ( var sub in filtered.SubFolders )
                RootItems.Add( sub );
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

                if ( child.Files.Any() || child.SubFolders.Any() )
                    filtered.SubFolders.Add( child );
            }

            return filtered;
        }


    }


}
