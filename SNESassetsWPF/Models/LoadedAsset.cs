using System.IO.Pipelines;

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

        /// <summary>
        /// Carries the FileReadResult
        /// </summary>
        public object ReadResult { get; }



        public LoadedAsset(string path , T asset , object readResult)
        {
            Path = path;
            Asset = asset;
            ReadResult = readResult;
        }

        // Constructor without ReadResult
        public LoadedAsset(string path , T asset)
        {
            Path = path;
            Asset = asset;
            ReadResult = null;
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
