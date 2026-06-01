using SNESassetsWPF.Enums;

using System;
using System.Collections.Generic;
using System.Text;




namespace SNESassetsWPF.Models
{
    /// <summary>
    /// Represents a folder in the scanned directory tree.
    /// Contains subfolders and supported files.
    /// </summary>
    public class FolderNode
    {
        public string Name { get; set; } = "";
        public string FullPath { get; set; } = "";

        public List<FolderNode> SubFolders { get; set; } = new();
        public List<FileNode> Files { get; set; } = new();

        /// <summary>
        /// True if this folder represents built-in assets rather than a real folder.
        /// </summary>
        public bool IsBuiltIn { get; set; } = false;

        public IEnumerable<object> Children
            => SubFolders.Cast<object>().Concat( Files );
    }

}