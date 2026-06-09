using SNESassetsWPF.Enums;




namespace SNESassetsWPF.Models
{
    public class FileNode
    {
        public string Name { get; set; } = "";
        public string FullPath { get; set; } = "";
        public FileType Type { get; set; }

        /// <summary>
        /// True if this file is a built-in test asset rather than a real file on disk.
        /// </summary>
        public bool IsBuiltIn { get; set; } = false;

        /// <summary>
        /// Parsed asset loaded by AssetLoaderService.
        /// This will be a LoadedAsset<T> instance.
        /// </summary>
        public object LoadedAsset { get; set; }

        public FileNode() { }

        public FileNode(string name , string fullPath , FileType type)
        {
            Name = name;
            FullPath = fullPath;
            Type = type;
        }
    }
}
