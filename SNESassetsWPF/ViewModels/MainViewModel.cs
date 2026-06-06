using SNESassetsWPF.Enums;
using SNESassetsWPF.Formats;
using SNESassetsWPF.Models;
using SNESassetsWPF.Services;
using SNESassetsWPF.ViewModels;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Windows.Input;

namespace SNESassetsWPF.ViewModels
{
    public class MainViewModel : ViewModelBase
    {
        //
        // ─────────────────────────────────────────────────────────────
        //  TREE VIEW MODELS
        // ─────────────────────────────────────────────────────────────
        //

        public ColTreeViewModel ColTree { get; }
        public CgxTreeViewModel CgxTree { get; }
        public ScrTreeViewModel ScrTree { get; }
        public PnlTreeViewModel PnlTree { get; }

        public MapTreeViewModel MapTree { get; }


        //
        // ─────────────────────────────────────────────────────────────
        //  VIEW MODELS
        // ─────────────────────────────────────────────────────────────
        //

        public PaletteViewModel Palette { get; } = new();
        public CgxViewerViewModel CgxViewer { get; } = new();
        public ScrViewerViewModel ScrViewer { get; } = new();
        public MapPnlViewerViewModel MapPnlViewer { get; } = new();



        //
        // ─────────────────────────────────────────────────────────────
        //  COMMANDS
        // ─────────────────────────────────────────────────────────────
        //

        public RelayCommand ChooseFolderCommand { get; private set; }

        public RelayCommand<FileNode> LoadColCommand { get; private set; }
        public RelayCommand<FileNode> LoadCgxCommand { get; private set; }
        public RelayCommand<FileNode> LoadScrCommand { get; private set; }
        public RelayCommand<FileNode> LoadPnlCommand { get; private set; }
        public RelayCommand<FileNode> LoadMapCommand { get; private set; }



        public RelayCommand ShowColHexCommand { get; private set; }

        // ⭐ NEW EXPORT COMMANDS
        public ICommand ExportCgxPngCommand { get; }
        public ICommand ExportScrPngCommand { get; }
        public ICommand ExportPnlPngCommand { get; }
        public ICommand ExportMapPngCommand { get; }



        //
        // ─────────────────────────────────────────────────────────────
        //  LOADED FILE PATHS
        // ─────────────────────────────────────────────────────────────
        //

        public string LoadedCgxPath { get; private set; }
        public string LoadedColPath { get; private set; }
        public string LoadedScrPath { get; private set; }
        public string LoadedPnlPath { get; private set; }
        public string LoadedMapPath { get; private set; }


        //
        // ─────────────────────────────────────────────────────────────
        //  CONSTRUCTOR
        // ─────────────────────────────────────────────────────────────
        //

        public MainViewModel()
        {
            //
            // COL TREE
            //
            ColTree = new ColTreeViewModel();
            ColTree.LoadBuiltIn();
            ColTree.SelectBuiltIn();
            LoadCol( ColTree.BuiltInFileNode );

            //
            // CGX TREE
            //
            CgxTree = new CgxTreeViewModel();
            CgxTree.LoadBuiltIn();
            CgxTree.SelectBuiltIn();
            LoadCgx( CgxTree.BuiltInFileNode );

            //
            // SCR TREE
            //
            ScrTree = new ScrTreeViewModel();


            //
            // PNL TREE
            //
            PnlTree = new PnlTreeViewModel();

            ///
            // PNL TREE
            //
            MapTree = new MapTreeViewModel();


            //
            // COMMANDS
            //
            ChooseFolderCommand = new RelayCommand( ChooseFolder );

            LoadColCommand = new RelayCommand<FileNode>( LoadCol );
            LoadCgxCommand = new RelayCommand<FileNode>( LoadCgx );
            LoadScrCommand = new RelayCommand<FileNode>( LoadScr );
            LoadPnlCommand = new RelayCommand<FileNode>( LoadPnl );
            LoadMapCommand = new RelayCommand<FileNode>( LoadMap );


            ShowColHexCommand = new RelayCommand(
                ShowColRawHex ,
                () => HasCol
            );

            //
            // PNG EXPORT COMMANDS
            //
            ExportCgxPngCommand = new RelayCommand( ExportCgxPng );
            ExportScrPngCommand = new RelayCommand( ExportScrPng );
            ExportPnlPngCommand = new RelayCommand( ExportPnlPng );
            ExportMapPngCommand = new RelayCommand( ExportMapPng );


        }


        //
        // ─────────────────────────────────────────────────────────────
        //  FOLDER SELECTION
        // ─────────────────────────────────────────────────────────────
        //

        private string _selectedFolder = "Please choose a folder containing S-CAD assets";
        public string SelectedFolder
        {
            get => _selectedFolder;
            set
            {
                _selectedFolder = value;
                OnPropertyChanged();
            }
        }

        private void ChooseFolder()
        {
            var dlg = new Microsoft.Win32.OpenFolderDialog();

            if ( dlg.ShowDialog() == true )
            {
                SelectedFolder = dlg.FolderName;

                // Load all format trees
                ColTree.LoadFolder( dlg.FolderName );
                CgxTree.LoadFolder( dlg.FolderName );
                ScrTree.LoadFolder( dlg.FolderName );
                PnlTree.LoadFolder( dlg.FolderName );
                MapTree.LoadFolder( dlg.FolderName );
            }
        }


        //
        // ─────────────────────────────────────────────────────────────
        //  COL LOADING
        // ─────────────────────────────────────────────────────────────
        //

        private PaletteEntry _selectedPaletteEntry;
        public PaletteEntry SelectedPaletteEntry
        {
            get => _selectedPaletteEntry;
            set
            {
                _selectedPaletteEntry = value;
                OnPropertyChanged();
            }
        }

        private ColFile _currentCol;
        public ColFile CurrentCol
        {
            get => _currentCol;
            set
            {
                _currentCol = value;
                OnPropertyChanged();
                OnPropertyChanged( nameof( HasCol ) );
            }
        }

        public bool HasCol => CurrentCol != null;

        private void LoadCol(FileNode fileNode)
        {
            if ( fileNode == null ) return;

            var reader = new ColFileReader();
            var raw = reader.Read(fileNode.FullPath);

            LoadedColPath = fileNode.FullPath;

            var parser = new ColFileParser();
            var col = parser.Parse(raw);

            CurrentCol = col;

            LoadPaletteIntoViewModel( col );

            // Update CGX + SCR viewers
            CgxViewer.ColFile = col;
            ScrViewer.ColFile = col;

            MapPnlViewer.CurrentCol = col;
        }



        private void LoadPaletteIntoViewModel(ColFile col)
        {
            Palette.PaletteRows.Clear();

            for ( int p = 0 ; p < 16 ; p++ )
            {
                var row = new PaletteRowViewModel();

                for ( int c = 0 ; c < 16 ; c++ )
                {
                    var snes = col.RawColors[p, c];
                    var rgb = col.RgbColors[p, c];

                    row.Colors.Add( new PaletteEntry
                    {
                        SnesColor = snes ,
                        SnesColorString = snes.ToHexPair() ,
                        RgbColor = rgb ,
                        RGBColorString = $"#{rgb.R:X2}{rgb.G:X2}{rgb.B:X2}"
                    } );
                }

                Palette.PaletteRows.Add( row );
            }
        }


        public void ShowColRawHex()
        {
            if ( CurrentCol == null )
                return;

            // Extract raw bytes
            byte[] raw = new byte[512];
            int index = 0;

            for ( int p = 0 ; p < 16 ; p++ )
            {
                for ( int c = 0 ; c < 16 ; c++ )
                {
                    var snes = CurrentCol.RawColors[p, c];
                    raw[index++] = snes.Low;
                    raw[index++] = snes.High;
                }
            }

            // Convert to hex
            var hexBuilder = new StringBuilder();

            for ( int i = 0 ; i < raw.Length ; i++ )
            {
                hexBuilder.Append( raw[i].ToString( "X2" ) );
                hexBuilder.Append( ' ' );

                if ( ( i + 1 ) % 16 == 0 )
                    hexBuilder.AppendLine();
            }

            string hexText = hexBuilder.ToString();

            // Extract ASCII header
            string asciiHeader = "(no ASCII header found)";

            if ( CurrentCol.Metadata != null && CurrentCol.Metadata.Length > 0 )
            {
                string rawHeader = Encoding.ASCII.GetString(CurrentCol.Metadata);

                int nullIndex = rawHeader.IndexOf('\0');
                if ( nullIndex >= 0 )
                    rawHeader = rawHeader.Substring( 0 , nullIndex );

                asciiHeader = new string( rawHeader.Where( c => c >= 32 && c <= 126 ).ToArray() );
                asciiHeader = asciiHeader.Trim();
            }

            var win = new HexWindow(hexText, asciiHeader);
            win.Owner = System.Windows.Application.Current.MainWindow;
            win.ShowDialog();
        }


        //
        // ─────────────────────────────────────────────────────────────
        //  CGX LOADING
        // ─────────────────────────────────────────────────────────────
        //

        private void LoadCgx(FileNode fileNode)
        {
            if ( fileNode == null ) return;

            var reader = new CgxFileReader();
            var readResult = reader.Read(fileNode.FullPath);

            LoadedCgxPath = fileNode.FullPath;

            var parser = new CgxFileParser();
            var cgx = parser.Parse(readResult);

            Debug.WriteLine( $"CGX BPP = {cgx.BitDepth}, tiles = {cgx.Tiles.Length}" );

            CgxViewer.CgxFile = cgx;
            ScrViewer.CgxFile = cgx;

            MapPnlViewer.CurrentCgx = cgx;
        }





        //
        // ─────────────────────────────────────────────────────────────
        //  SCR LOADING
        // ─────────────────────────────────────────────────────────────
        //
        private void LoadScr(FileNode fileNode)
        {
            if ( fileNode == null || string.IsNullOrWhiteSpace( fileNode.FullPath ) )
                return;

            Debug.WriteLine( $"LoadScr called for: {fileNode.FullPath}" );

            try
            {
                // New: ScrFileReader auto-detects 32×32 or 64×64
                var readResult = ScrFileReader.Load(fileNode.FullPath);

                LoadedScrPath = fileNode.FullPath;

                if ( !readResult.Success )
                {
                    Debug.WriteLine( "SCR read error: " + readResult.ErrorMessage );
                    return;
                }

                var scr = readResult.Scr;

                Debug.WriteLine(
                    $"SCR loaded: {scr.WidthTiles}×{scr.HeightTiles} tiles " +
                    $"({scr.WidthTiles * 8}×{scr.HeightTiles * 8} px)" );

                // Send SCR + current CGX/COL to viewer
                ScrViewer.ScrFile = scr;
                ScrViewer.CgxFile = CgxViewer.CgxFile;
                ScrViewer.ColFile = CurrentCol;
            }
            catch ( Exception ex )
            {
                Debug.WriteLine( "LoadScr exception: " + ex.Message );
            }
        }




        //
        // ─────────────────────────────────────────────────────────────
        //  PNL Loading
        // ─────────────────────────────────────────────────────────────
        //
        private PnlFile _currentPnl;
        public PnlFile CurrentPnl
        {
            get => _currentPnl;
            set
            {
                _currentPnl = value;
                OnPropertyChanged();
                OnPropertyChanged( nameof( HasPnl ) );
            }
        }

        public bool HasPnl => CurrentPnl != null;




        private void LoadPnl(FileNode fileNode)
        {
            if ( fileNode == null )
                return;

            // 1. Read raw bytes
            var reader = new PnlFileReader();
            var readResult = reader.Read(fileNode.FullPath);

            LoadedPnlPath = fileNode.FullPath;

            if ( !readResult.Success )
            {
                Debug.WriteLine( "PNL read error: " + readResult.ErrorMessage );
                return;
            }


            // 2. Parse raw bytes into a PnlFile
            var parser = new PnlFileParser();
            var pnl = parser.Parse(readResult.Data);


            // 3. Store parsed PNL
            CurrentPnl = pnl;

            MapPnlViewer.CurrentPnl = pnl;


            // 4. Verification
            PnlVerify.DumpSummary( pnl );

        }





        //
        // ─────────────────────────────────────────────────────────────
        //  MAP Loading
        // ─────────────────────────────────────────────────────────────
        //
        private MapFile _currentMap;
        public MapFile CurrentMap
        {
            get => _currentMap;
            set
            {
                _currentMap = value;
                OnPropertyChanged();
                OnPropertyChanged( nameof( HasMap ) );
            }
        }

        public bool HasMap => CurrentMap != null;




        private void LoadMap(FileNode fileNode)
        {
        }










        //
        // ─────────────────────────────────────────────────────────────
        //  PNG EXPORT HELPERS
        // ─────────────────────────────────────────────────────────────
        //

        public static string BuildPngExportName(
            string scrPath ,
            string cgxPath ,
            string colPath ,
            int zoom ,
            bool forceSingleRow = false ,
            int selectedRow = -1 ,
            ScrDebugMode debugMode = ScrDebugMode.None ,
            bool isScrExport = false)
        {
            string scr = System.IO.Path.GetFileNameWithoutExtension(scrPath) ?? "unknown_SCR";
            string cgx = System.IO.Path.GetFileNameWithoutExtension(cgxPath) ?? "unknown_CGX";
            string col = System.IO.Path.GetFileNameWithoutExtension(colPath) ?? "unknown_COL";

            string name;

            if ( !isScrExport )
            {
                //
                // CGX EXPORT FORMAT
                //
                name = $"CGX_{cgx}_COL_{col}";

                if ( forceSingleRow && selectedRow >= 0 )
                    name += $"_ROW_{selectedRow:X1}";
            }
            else
            {
                //
                // SCR EXPORT FORMAT
                //
                name = $"SCR_{scr}_CGX_{cgx}_COL_{col}";

                if ( debugMode != ScrDebugMode.None )
                    name += $"_DBG_{debugMode}";
            }

            name += $"_{zoom}x.png";

            return name;
        }



        //
        // ─────────────────────────────────────────────────────────────
        //  CGX PNG EXPORT
        // ─────────────────────────────────────────────────────────────
        //

        private void ExportCgxPng()
        {
            if ( CgxViewer == null )
                return;

            string exportName = BuildPngExportName(
                scrPath: null,
                cgxPath: LoadedCgxPath,
                colPath: LoadedColPath,
                zoom: CgxViewer.ZoomLevel,
                forceSingleRow: Palette.ForceSingleRow,
                selectedRow: Palette.SelectedPaletteRowIndex,
                debugMode: ScrDebugMode.None,
                isScrExport: false
            );


            var dlg = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "PNG Image|*.png",
                FileName = exportName
            };

            if ( dlg.ShowDialog() == true )
                CgxViewer.ExportPng( dlg.FileName , CgxViewer.ZoomLevel );
        }


        //
        // ─────────────────────────────────────────────────────────────
        //  SCR PNG EXPORT
        // ─────────────────────────────────────────────────────────────
        //
        private void ExportScrPng()
        {
            if ( ScrViewer == null )
                return;

            string exportName = BuildPngExportName(
                scrPath: LoadedScrPath,
                cgxPath: LoadedCgxPath,
                colPath: LoadedColPath,
                zoom: ScrViewer.ZoomLevel,
                forceSingleRow: false,
                selectedRow: -1,
                debugMode: ScrViewer.SelectedDebugMode,
                isScrExport: true
            );


            var dlg = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "PNG Image|*.png",
                FileName = exportName
            };

            if ( dlg.ShowDialog() == true )
                ScrViewer.SavePng( dlg.FileName );
        }




        //
        // ─────────────────────────────────────────────────────────────
        //  MAP PNL PNG EXPORT
        // ─────────────────────────────────────────────────────────────
        //
        private void ExportMapPng()
        {
            if ( MapPnlViewer == null )
                return;

            string exportName = $"PNL_{System.IO.Path.GetFileNameWithoutExtension(LoadedPnlPath)}_MAP_{System.IO.Path.GetFileNameWithoutExtension(LoadedMapPath)}_{MapPnlViewer.ZoomLevel}x.png";

            var dlg = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "PNG Image|*.png",
                FileName = exportName
            };

            if ( dlg.ShowDialog() == true )
                MapPnlViewer.SaveMapPng( dlg.FileName );
        }



        private void ExportPnlPng()
        {
            if ( MapPnlViewer == null )
                return;

            string exportName = $"PNL_{System.IO.Path.GetFileNameWithoutExtension(LoadedPnlPath)}_{MapPnlViewer.ZoomLevel}x.png";

            var dlg = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "PNG Image|*.png",
                FileName = exportName
            };

            if ( dlg.ShowDialog() == true )
                MapPnlViewer.SaveMapPng( dlg.FileName );
        }

    }
}
