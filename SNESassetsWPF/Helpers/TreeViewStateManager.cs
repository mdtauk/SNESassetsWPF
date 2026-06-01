using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Threading;
using SNESassetsWPF.Models;

namespace SNESassetsWPF.Helpers
{
    public class TreeViewStateManager
    {
        private readonly TreeView _tree;
        private bool _pendingRestore = false;
        private bool _pendingExpandRestore = false;
        private bool _pendingSelectRestore = false;

        public TreeViewStateManager(TreeView tree)
        {
            _tree = tree;
        }


        private bool AllExpandedNodesExist(List<string> expanded)
        {
            foreach ( var path in expanded )
            {
                if ( !NodeContainerExists( path ) )
                    return false;
            }
            return true;
        }

        private bool NodeContainerExists(string path)
        {
            foreach ( var item in _tree.Items )
            {
                var tvi = _tree.ItemContainerGenerator.ContainerFromItem(item) as TreeViewItem;
                if ( FindNodeContainer( tvi , path ) != null )
                    return true;
            }
            return false;
        }

        private TreeViewItem FindNodeContainer(TreeViewItem item , string path)
        {
            if ( item == null )
                return null;

            if ( item.DataContext is FolderNode f && f.FullPath == path )
                return item;

            foreach ( var child in item.Items )
            {
                var childItem = item.ItemContainerGenerator.ContainerFromItem(child) as TreeViewItem;
                var found = FindNodeContainer(childItem, path);
                if ( found != null )
                    return found;
            }

            return null;
        }



        // -------- SAVE --------

        public List<string> SaveExpanded()
        {
            var list = new List<string>();

            foreach ( var item in _tree.Items )
            {
                var tvi = _tree.ItemContainerGenerator.ContainerFromItem(item) as TreeViewItem;
                CollectExpanded( tvi , list );
            }

            return list;
        }

        public string SaveSelected()
        {
            if ( _tree.SelectedItem is FileNode f )
                return f.FullPath;

            if ( _tree.SelectedItem is FolderNode d )
                return d.FullPath;

            return null;
        }

        private void CollectExpanded(TreeViewItem item , List<string> list)
        {
            if ( item == null ) return;

            if ( item.IsExpanded )
            {
                if ( item.DataContext is FolderNode folder )
                    list.Add( folder.FullPath );
            }

            foreach ( var child in item.Items )
            {
                var childItem = item.ItemContainerGenerator.ContainerFromItem(child) as TreeViewItem;
                CollectExpanded( childItem , list );
            }
        }

        // -------- RESTORE ENTRY POINT --------

        public void RestoreState(List<string> expanded , string selected)
        {
            if ( expanded == null && selected == null )
                return;

            _pendingExpandRestore = true;
            _pendingSelectRestore = true;

            // First pass: expand what exists
            RestoreExpanded( expanded );

            // Defer deeper expansion + selection
            _tree.Dispatcher.BeginInvoke( DispatcherPriority.Background , new Action( () =>
            {
                ExpandToPath( selected );
                RestoreSelectedWhenReady( selected );
            } ) );
        }



        // -------- EXPANSION RESTORE --------

        private void RestoreExpanded(List<string> expanded)
        {
            if ( expanded == null ) return;

            foreach ( var item in _tree.Items )
            {
                var tvi = _tree.ItemContainerGenerator.ContainerFromItem(item) as TreeViewItem;
                RestoreExpandedItem( tvi , expanded );
            }
        }

        private void RestoreExpandedItem(TreeViewItem item , List<string> expanded)
        {
            if ( item == null ) return;

            if ( item.DataContext is FolderNode folder && expanded.Contains( folder.FullPath ) )
                item.IsExpanded = true;

            foreach ( var child in item.Items )
            {
                var childItem = item.ItemContainerGenerator.ContainerFromItem(child) as TreeViewItem;
                RestoreExpandedItem( childItem , expanded );
            }
        }

        // -------- EXPAND PATH TO SELECTED --------

        private void ExpandToPath(string path)
        {
            if ( string.IsNullOrEmpty( path ) )
                return;

            foreach ( var item in _tree.Items )
            {
                var tvi = _tree.ItemContainerGenerator.ContainerFromItem(item) as TreeViewItem;
                if ( ExpandToPathRecursive( tvi , path ) )
                    break;
            }
        }

        private bool ExpandToPathRecursive(TreeViewItem item , string path)
        {
            if ( item == null )
                return false;

            // Expand folder nodes that are prefixes of the selected path
            if ( item.DataContext is FolderNode folder )
            {
                if ( path.StartsWith( folder.FullPath , StringComparison.OrdinalIgnoreCase ) )
                    item.IsExpanded = true;

                if ( string.Equals( folder.FullPath , path , StringComparison.OrdinalIgnoreCase ) )
                    return true;
            }

            // NEW: Expand parent folder of FileNode
            if ( item.DataContext is FileNode file )
            {
                var parent = System.IO.Path.GetDirectoryName(file.FullPath);

                if ( path.StartsWith( parent , StringComparison.OrdinalIgnoreCase ) )
                {
                    var parentItem = item.Parent as TreeViewItem;
                    if ( parentItem != null )
                        parentItem.IsExpanded = true;
                }

                if ( string.Equals( file.FullPath , path , StringComparison.OrdinalIgnoreCase ) )
                    return true;
            }

            // Recurse into children
            foreach ( var child in item.Items )
            {
                var childItem = item.ItemContainerGenerator.ContainerFromItem(child) as TreeViewItem;
                if ( ExpandToPathRecursive( childItem , path ) )
                    return true;
            }

            return false;
        }


        // -------- SELECTION RESTORE (RETRY UNTIL READY) --------

        private void RestoreSelectedWhenReady(string selectedPath)
        {
            if ( string.IsNullOrEmpty( selectedPath ) )
                return;

            // First attempt after background
            _tree.Dispatcher.BeginInvoke( DispatcherPriority.Background , new Action( () =>
            {
                if ( TryRestoreSelected( selectedPath ) )
                    return;

                // Second attempt after background again
                _tree.Dispatcher.BeginInvoke( DispatcherPriority.Background , new Action( () =>
                {
                    if ( TryRestoreSelected( selectedPath ) )
                        return;

                    // Final attempt at idle
                    _tree.Dispatcher.BeginInvoke( DispatcherPriority.ApplicationIdle , new Action( () =>
                    {
                        TryRestoreSelected( selectedPath );
                    } ) );
                } ) );
            } ) );
        }

        private bool TryRestoreSelected(string selectedPath)
        {
            foreach ( var item in _tree.Items )
            {
                var tvi = _tree.ItemContainerGenerator.ContainerFromItem(item) as TreeViewItem;
                if ( TrySelect( tvi , selectedPath ) )
                    return true;
            }
            return false;
        }

        private bool TrySelect(TreeViewItem item , string path)
        {
            if ( item == null ) return false;

            if ( item.DataContext is FileNode f && string.Equals( f.FullPath , path , StringComparison.OrdinalIgnoreCase ) )
            {
                item.IsSelected = true;
                item.BringIntoView();
                return true;
            }

            if ( item.DataContext is FolderNode d && string.Equals( d.FullPath , path , StringComparison.OrdinalIgnoreCase ) )
            {
                item.IsSelected = true;
                item.BringIntoView();
                return true;
            }

            foreach ( var child in item.Items )
            {
                var childItem = item.ItemContainerGenerator.ContainerFromItem(child) as TreeViewItem;
                if ( TrySelect( childItem , path ) )
                    return true;
            }

            return false;
        }



        public void TryPendingRestore(string selected)
        {
            if ( !_pendingRestore )
                return;

            if ( TryRestoreSelected( selected ) )
            {
                _pendingRestore = false;
            }
        }


        public void TryPendingExpandRestore(List<string> expanded)
        {
            if ( !_pendingExpandRestore )
                return;

            RestoreExpanded( expanded );

            // Check if all expanded nodes now exist
            if ( AllExpandedNodesExist( expanded ) )
                _pendingExpandRestore = false;
        }
    }
}
