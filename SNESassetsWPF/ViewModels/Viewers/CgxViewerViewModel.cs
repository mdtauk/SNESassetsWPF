using SNESassetsWPF.Formats;
using SNESassetsWPF.Models;
using SNESassetsWPF.Rendering;
using SNESassetsWPF.Services;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Media.Imaging;

namespace SNESassetsWPF.ViewModels
{
    public class CgxViewerViewModel : INotifyPropertyChanged
    {
        //
        // ─────────────────────────────────────────────────────────────
        //  Fields
        // ─────────────────────────────────────────────────────────────
        //
        private CgxFile _cgxFile;
        private ColFile _colFile;
        private WriteableBitmap _cgxBitmap;

        private int _tilesPerRow = 16;
        private int _bitDepthOverride = -1;

        private int _zoomLevel = 1;
        private bool _showGrid = false;




        //
        // ─────────────────────────────────────────────────────────────
        //  Palette (Injected)
        // ─────────────────────────────────────────────────────────────
        //
        public PaletteViewModel Palette { get; }

        public ReadOnlyCollection<PaletteEntry> ActivePalette =>
            Palette?.ActivePalette;




        //
        // ─────────────────────────────────────────────────────────────
        //  Public Properties
        // ─────────────────────────────────────────────────────────────
        //
        public CgxFile CgxFile
        {
            get => _cgxFile;
            set
            {
                if ( _cgxFile != value )
                {
                    _cgxFile = value;
                    OnPropertyChanged();
                    OnPropertyChanged( nameof( HasCgx ) );

                    BitDepthOverride = _cgxFile?.BitDepth ?? -1;
                    RenderCgx();
                }
            }
        }

        public ColFile ColFile
        {
            get => _colFile;
            set
            {
                if ( _colFile != value )
                {
                    _colFile = value;
                    OnPropertyChanged();
                    OnPropertyChanged( nameof( HasCol ) );
                    RenderCgx();
                }
            }
        }

        public bool HasCgx => CgxFile != null;
        public bool HasCol => ColFile != null;


        public WriteableBitmap CgxBitmap
        {
            get => _cgxBitmap;
            private set
            {
                if ( _cgxBitmap != value )
                {
                    _cgxBitmap = value;
                    OnPropertyChanged();
                }
            }
        }




        //
        // ─────────────────────────────────────────────────────────────
        //  Headers
        // ─────────────────────────────────────────────────────────────
        //
        private WriteableBitmap _columnHeader;
        public WriteableBitmap ColumnHeader
        {
            get => _columnHeader;
            private set { _columnHeader = value; OnPropertyChanged(); }
        }

        private WriteableBitmap _rowHeader;
        public WriteableBitmap RowHeader
        {
            get => _rowHeader;
            private set { _rowHeader = value; OnPropertyChanged(); }
        }

        private WriteableBitmap _spacer;
        public WriteableBitmap Spacer
        {
            get => _spacer;
            private set { _spacer = value; OnPropertyChanged(); }
        }




        //
        // ─────────────────────────────────────────────────────────────
        //  Layout Properties
        // ─────────────────────────────────────────────────────────────
        //
        public int TilesPerRow
        {
            get => _tilesPerRow;
            set
            {
                if ( _tilesPerRow != value )
                {
                    _tilesPerRow = value;
                    OnPropertyChanged();
                    RenderCgx();
                }
            }
        }


        public int BitDepthOverride
        {
            get => _bitDepthOverride;
            set
            {
                if ( _bitDepthOverride == value )
                    return;

                _bitDepthOverride = value;
                OnPropertyChanged();

                Palette?.ApplyBitDepthRules( _bitDepthOverride );

                if ( CgxFile != null )
                {
                    var parser = new CgxFileParser();
                    parser.ReinterpretBitDepth( CgxFile , _bitDepthOverride );
                }

                RenderCgx();
            }
        }




        //
        // ─────────────────────────────────────────────────────────────
        //  Zoom + Grid
        // ─────────────────────────────────────────────────────────────
        //
        public int ZoomLevel
        {
            get => _zoomLevel;
            set
            {
                if ( _zoomLevel != value )
                {
                    _zoomLevel = value;
                    OnPropertyChanged();
                    OnPropertyChanged( nameof( IsGridToggleEnabled ) );

                    if ( _zoomLevel == 1 )
                        ShowGrid = false;

                    RenderCgx();
                }
            }
        }

        public bool ShowGrid
        {
            get => _showGrid;
            set
            {
                if ( _showGrid != value )
                {
                    _showGrid = value;
                    OnPropertyChanged();
                    RenderCgx();
                }
            }
        }

        public bool IsGridToggleEnabled => ZoomLevel >= 2;




        //
        // ─────────────────────────────────────────────────────────────
        //  Rendering
        // ─────────────────────────────────────────────────────────────
        //
        public void RenderCgx()
        {
            if ( CgxFile == null || ColFile == null )
            {
                CgxBitmap = null;
                return;
            }

            try
            {
                var renderer = new CgxRenderer();

                var render = renderer.Render(
                    CgxFile,
                    ColFile,
                    Palette.ForceSingleRow,
                    Palette.SelectedPaletteRowIndex,
                    TilesPerRow,
                    zoom: ZoomLevel,
                    showGrid: ShowGrid
                );

                CgxBitmap = BitmapFactory.FromRenderResult( render );

                GenerateHeaders();
            }
            catch ( Exception ex )
            {
                Console.WriteLine( "CGX render error: " + ex.Message );
            }
        }


        private void GenerateHeaders()
        {
            try
            {
                ColumnHeader = HeaderGenerator.GenerateColumnHeader(
                    TilesPerRow ,
                    ZoomLevel ,
                    ShowGrid
                );

                int rows = (int)Math.Ceiling(CgxFile.TileCount / (double)TilesPerRow);

                RowHeader = HeaderGenerator.GenerateRowHeader(
                    CgxFile.TileCount ,
                    TilesPerRow ,
                    ZoomLevel ,
                    ShowGrid
                );

                Spacer = HeaderGenerator.GenerateSpacer( ZoomLevel , ShowGrid );
            }
            catch ( Exception ex )
            {
                Console.WriteLine( "Header generation error: " + ex.Message );
            }
        }




        //
        // ─────────────────────────────────────────────────────────────
        //  PNG Export
        // ─────────────────────────────────────────────────────────────
        //
        public void ExportPng(string path , int zoom)
        {
            if ( CgxFile == null || ColFile == null )
                return;

            var renderer = new CgxRenderer();

            var render = renderer.Render(
                CgxFile,
                ColFile,
                Palette.ForceSingleRow,
                Palette.SelectedPaletteRowIndex,
                TilesPerRow,
                zoom: zoom,
                showGrid: false
            );

            BitmapFactory.SavePng( render , path );
        }




        //
        // ─────────────────────────────────────────────────────────────
        //  INotifyPropertyChanged
        // ─────────────────────────────────────────────────────────────
        //
        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string name = null)
            => PropertyChanged?.Invoke( this , new PropertyChangedEventArgs( name ) );




        //
        // ─────────────────────────────────────────────────────────────
        //  Constructor
        // ─────────────────────────────────────────────────────────────
        //
        public CgxViewerViewModel()
        {
            Palette = new PaletteViewModel();

            // React to palette changes
            Palette.PaletteChanged += RenderCgx;
        }
    }
}
