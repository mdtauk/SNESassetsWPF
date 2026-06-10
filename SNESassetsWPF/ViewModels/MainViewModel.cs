using SNESassetsWPF.Enums;
using SNESassetsWPF.Formats;
using SNESassetsWPF.Helpers;
using SNESassetsWPF.Models;
using SNESassetsWPF.Services;
using SNESassetsWPF.ViewModels;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Input;

namespace SNESassetsWPF.ViewModels
{
    public class MainViewModel : ViewModelBase
    {
        //
        // ─────────────────────────────────────────────────────────────
        //  SERVICES
        // ─────────────────────────────────────────────────────────────
        //
        private readonly AssetLoaderService _loader = new();
        private readonly PngExportService _exporter = new();
        private readonly PaletteBuilder _paletteBuilder = new();
        private readonly PaletteRendererAdapter _paletteAdapter;




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
        public ScrViewerViewModel ScrViewer { get; }
        public MapPnlViewerViewModel MapPnlViewer { get; } = new();


        //
        // ─────────────────────────────────────────────────────────────
        //  CURRENT FILES
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





        private LoadedAsset<ColFile> _currentCol;
        public LoadedAsset<ColFile> CurrentCol
        {
            get => _currentCol;
            set
            {
                _currentCol = value;
                OnPropertyChanged();
                OnPropertyChanged( nameof( HasCol ) );

                // If load failed or file is null → clear everything safely
                if ( value?.Asset == null )
                {
                    Debug.WriteLine( "[Main] CurrentCol set: value was null (load failed)" );

                    Palette.PaletteRows.Clear();

                    CgxViewer.ColFile = null;
                    ScrViewer.ColFile = null;
                    MapPnlViewer.CurrentCol = null;

                    return;
                }

                // Safe to use the palette now
                Debug.WriteLine( $"[Main] CurrentCol set: RawColors={value.Asset.RawColors.Length}" );

                Palette.PaletteRows.Clear();
                foreach ( var row in _paletteBuilder.Build( value.Asset ) )
                    Palette.PaletteRows.Add( row );

                CgxViewer.ColFile = value.Asset;
                ScrViewer.ColFile = value.Asset;
                MapPnlViewer.CurrentCol = value.Asset;
            }
        }

        public bool HasCol => CurrentCol != null;




        private LoadedAsset<CgxFile> _currentCgx;
        public LoadedAsset<CgxFile> CurrentCgx
        {
            get => _currentCgx;
            set
            {
                _currentCgx = value;
                OnPropertyChanged();

                if ( value?.Asset == null )
                {
                    Debug.WriteLine( "[Main] CurrentCgx set: value was null." );

                    // Clear all viewers
                    CgxViewer.CgxFile = null;
                    ScrViewer.CgxFile = null;
                    MapPnlViewer.CurrentCgx = null;
                    return;
                }

                Debug.WriteLine( $"[Main] CurrentCgx set: TileCount={value.Asset.TileCount}" );

                // Assign to viewers
                CgxViewer.CgxFile = value.Asset;
                ScrViewer.CgxFile = value.Asset;
                MapPnlViewer.CurrentCgx = value.Asset;
            }
        }





        private LoadedAsset<ScrFile> _currentScr;
        public LoadedAsset<ScrFile> CurrentScr
        {
            get => _currentScr;
            set
            {
                _currentScr = value;
                OnPropertyChanged();
                System.Diagnostics.Debug.WriteLine( $"[Main] CurrentScr set: BlockCount={value?.Asset?.BlockCount}" );
                ScrViewer.ScrFile = value.Asset;
            }
        }




        private LoadedAsset<PnlFile> _currentPnl;
        public LoadedAsset<PnlFile> CurrentPnl
        {
            get => _currentPnl;
            set
            {
                _currentPnl = value;
                OnPropertyChanged();

                OnPropertyChanged( nameof( HasPnl ) );

                MapPnlViewer.CurrentPnl = value.Asset;
            }
        }
        public bool HasPnl => CurrentPnl != null;




        private LoadedAsset<MapFile> _currentMap;
        public LoadedAsset<MapFile> CurrentMap
        {
            get => _currentMap;
            set
            {
                _currentMap = value;
                OnPropertyChanged();
                System.Diagnostics.Debug.WriteLine( $"[Main] CurrentScr set: MapWidth={value?.Asset?.Width} MapHeight={value?.Asset?.Height}" );

                OnPropertyChanged( nameof( HasMap ) );

                if ( value?.Asset == null )
                {
                    Debug.WriteLine( "[Main] CurrentMap set: value was null (load failed)" );
                    MapPnlViewer.CurrentMap = null;
                    return;
                }

                MapPnlViewer.CurrentMap = value.Asset;
            }
        }
        public bool HasMap => CurrentMap != null;
        //
        // ─────────────────────────────────────────────────────────────
        //  COMMANDS
        // ─────────────────────────────────────────────────────────────
        //
        public ICommand ChooseFolderCommand { get; }
        public ICommand ShowColHexCommand { get; }

        public ICommand LoadColCommand { get; }
        public ICommand LoadCgxCommand { get; }
        public ICommand LoadScrCommand { get; }
        public ICommand LoadPnlCommand { get; }
        public ICommand LoadMapCommand { get; }

        public ICommand ExportCgxPngCommand { get; }
        public ICommand ExportScrPngCommand { get; }
        public ICommand ExportPnlPngCommand { get; }
        public ICommand ExportMapPngCommand { get; }


        //
        // ─────────────────────────────────────────────────────────────
        //  CONSTRUCTOR
        // ─────────────────────────────────────────────────────────────
        //
        public MainViewModel()
        {
            ChooseFolderCommand = new RelayCommand( ChooseFolder );
            ShowColHexCommand = new RelayCommand( ShowColHex );


            // Create ALL viewer VMs FIRST
            MapPnlViewer = new MapPnlViewerViewModel();
            CgxViewer = new CgxViewerViewModel();
            ScrViewer = new ScrViewerViewModel(Palette);


            ColTree = new ColTreeViewModel();
            CgxTree = new CgxTreeViewModel();
            ScrTree = new ScrTreeViewModel();
            PnlTree = new PnlTreeViewModel();
            MapTree = new MapTreeViewModel();

            LoadColCommand = new RelayCommand<FileNode>( node => CurrentCol = _loader.LoadCol( node.FullPath ) );
            LoadCgxCommand = new RelayCommand<FileNode>( node => CurrentCgx = _loader.LoadCgx( node.FullPath ) );
            LoadScrCommand = new RelayCommand<FileNode>( node => CurrentScr = _loader.LoadScr( node.FullPath ) );
            LoadPnlCommand = new RelayCommand<FileNode>( node => CurrentPnl = _loader.LoadPnl( node.FullPath ) );
            LoadMapCommand = new RelayCommand<FileNode>( node => CurrentMap = _loader.LoadMap( node.FullPath ) );


            ExportCgxPngCommand = new RelayCommand(
                () => _exporter.ExportCgx( CgxViewer , CurrentCgx , CurrentCol )
            );

            ExportScrPngCommand = new RelayCommand(
                () => _exporter.ExportScr( ScrViewer , CurrentScr, CurrentCgx , CurrentCol )
            );

            ExportPnlPngCommand = new RelayCommand(
                () => _exporter.ExportPnl( MapPnlViewer , CurrentPnl , CurrentCgx , CurrentCol )
            );

            ExportMapPngCommand = new RelayCommand(
                () => _exporter.ExportMap( MapPnlViewer , CurrentPnl , CurrentMap )
            );


            // Load Built In files
            ColTree.LoadBuiltIn();
            ColTree.SelectBuiltIn();
            LoadColCommand.Execute( ColTree.BuiltInFileNode );

            CgxTree.LoadBuiltIn();
            CgxTree.SelectBuiltIn();
            LoadCgxCommand.Execute( CgxTree.BuiltInFileNode );


            _paletteAdapter = new PaletteRendererAdapter( Palette , CgxViewer , ScrViewer , MapPnlViewer );

        }




        //
        // ─────────────────────────────────────────────────────────────
        //  COMMAND METHODS
        // ─────────────────────────────────────────────────────────────
        //
        private void ChooseFolder()
        {
            var dlg = new Microsoft.Win32.OpenFolderDialog();

            if ( dlg.ShowDialog() == true )
            {
                SelectedFolder = dlg.FolderName;

                ColTree.LoadFolder( dlg.FolderName );
                CgxTree.LoadFolder( dlg.FolderName );
                ScrTree.LoadFolder( dlg.FolderName );
                PnlTree.LoadFolder( dlg.FolderName );
                MapTree.LoadFolder( dlg.FolderName );
            }
        }




        private void ShowColHex()
        {
            var node = ColTree.SelectedFileNode;
            if ( node == null )
                return;

            var col = CurrentCol;

            // 1. Build raw 512-byte palette
            byte[] raw = new byte[512];
            int k = 0;

            for ( int row = 0 ; row < 16 ; row++ )
            {
                for ( int colIndex = 0 ; colIndex < 16 ; colIndex++ )
                {
                    var snes = col.Asset.RawColors[row, colIndex];

                    raw[k++] = snes.Low;
                    raw[k++] = snes.High;
                }
            }


            // 2. Format hex dump (16 bytes per line)
            var sb = new StringBuilder();
            sb.AppendLine( "=== RAW 512-BYTE PALETTE HEX ===" + System.Environment.NewLine );

            for ( int i = 0 ; i < raw.Length ; i++ )
            {
                sb.Append( raw[i].ToString( "X2" ) );
                sb.Append( ' ' );

                if ( ( i + 1 ) % 16 == 0 )
                    sb.AppendLine();
            }


            // 3. Show modal window
            var win = new HexWindow(sb.ToString(), col.Asset.Metadata);
            win.Owner = Application.Current.MainWindow;
            win.ShowDialog();
        }




    }

}
