namespace SNESassetsWPF.Models
{
    /// <summary>
    /// Wraps a parsed SNES asset together with its source file path.
    /// This keeps format models pure while still allowing the app
    /// to track metadata needed for exporting, logging, and UI.
    /// </summary>
    public class LoadedAsset<T>
    {
        /// <summary>
        /// Full path to the file on disk.
        /// </summary>
        public string Path { get; }

        /// <summary>
        /// Parsed SNES data (CgxFile, ColFile, ScrFile, etc.)
        /// </summary>
        public T Asset { get; }

        public LoadedAsset(string path , T asset)
        {
            Path = path;
            Asset = asset;
        }

        /// <summary>
        /// Convenience: returns just the filename without extension.
        /// Useful for exporters.
        /// </summary>
        public string FileNameWithoutExtension =>
            System.IO.Path.GetFileNameWithoutExtension( Path );

        /// <summary>
        /// Convenience: returns the filename including extension.
        /// </summary>
        public string FileName =>
            System.IO.Path.GetFileName( Path );
    }
}
