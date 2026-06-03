using SNESassetsWPF.Enums;
using SNESassetsWPF.Formats;
using SNESassetsWPF.Rendering;
using SNESassetsWPF.Services;
using System.Diagnostics;
using System.Windows.Media.Imaging;

namespace SNESassetsWPF.ViewModels
{
    public class ScrViewerViewModel : ViewModelBase
    {
        // ─────────────────────────────────────────────────────────────
        // SCR / CGX / COL references
        // ─────────────────────────────────────────────────────────────

        private ScrFile _scrFile;
        public ScrFile ScrFile
        {
            get => _scrFile;
            set
            {
                _scrFile = value;
                OnPropertyChanged();

                SelectedDebugMode = ScrDebugMode.None;
                RenderScr();
            }
        }

        private CgxFile _cgxFile;
        public CgxFile CgxFile
        {
            get => _cgxFile;
            set
            {
                _cgxFile = value;
                OnPropertyChanged();
                RenderScr();
            }
        }

        private ColFile _colFile;
        public ColFile ColFile
        {
            get => _colFile;
            set
            {
                _colFile = value;
                OnPropertyChanged();
                RenderScr();
            }
        }

        // ─────────────────────────────────────────────────────────────
        // Palette source toggle
        // ─────────────────────────────────────────────────────────────

        private bool _useScrPaletteRow = true;
        public bool UseScrPaletteRow
        {
            get => _useScrPaletteRow;
            set
            {
                _useScrPaletteRow = value;
                OnPropertyChanged();
                RenderScr();
            }
        }

        // ─────────────────────────────────────────────────────────────
        // Debug mode
        // ─────────────────────────────────────────────────────────────

        private ScrDebugMode _selectedDebugMode = ScrDebugMode.TileIndex;
        public ScrDebugMode SelectedDebugMode
        {
            get => _selectedDebugMode;
            set
            {
                _selectedDebugMode = value;
                OnPropertyChanged();
                RenderScr();
            }
        }

        public static ScrDebugMode[] DebugModes { get; } =
        {
            ScrDebugMode.None,
            ScrDebugMode.TileIndex,
            ScrDebugMode.PaletteRowIndex,
            ScrDebugMode.Flip,
            ScrDebugMode.Priority
        };

        // ─────────────────────────────────────────────────────────────
        // Zoom + Grid
        // ─────────────────────────────────────────────────────────────

        private int _zoomLevel = 1;
        public int ZoomLevel
        {
            get => _zoomLevel;
            set
            {
                if ( value < 1 || value > 4 )
                    return;

                _zoomLevel = value;
                OnPropertyChanged();
                OnPropertyChanged( nameof( IsGridToggleEnabled ) );
                RenderScr();
            }
        }

        public int ZoomPercent
        {
            get => _zoomLevel * 100;
            set
            {
                ZoomLevel = value / 100;
                OnPropertyChanged();
            }
        }

        public bool IsGridToggleEnabled => ZoomLevel >= 2;

        private bool _showGrid;
        public bool ShowGrid
        {
            get => _showGrid;
            set
            {
                _showGrid = value;
                OnPropertyChanged();
                RenderScr();
            }
        }

        // ─────────────────────────────────────────────────────────────
        // Bitmap + RenderResult
        // ─────────────────────────────────────────────────────────────

        private WriteableBitmap _bitmap;
        public WriteableBitmap Bitmap
        {
            get => _bitmap;
            private set
            {
                _bitmap = value;
                OnPropertyChanged();
            }
        }

        private RenderResult _lastRenderResult;
        public RenderResult LastRenderResult => _lastRenderResult;


        // ─────────────────────────────────────────────────────────────
        // Visibility Toggle
        // ─────────────────────────────────────────────────────────────

        private bool _showInvisibleTiles = false;
        public bool ShowInvisibleTiles
        {
            get => _showInvisibleTiles;
            set
            {
                _showInvisibleTiles = value;
                OnPropertyChanged();
                RenderScr();
            }
        }


        // ─────────────────────────────────────────────────────────────
        // Constructor
        // ─────────────────────────────────────────────────────────────

        public ScrViewerViewModel()
        {
            SelectedDebugMode = ScrDebugMode.None;
        }

        // ─────────────────────────────────────────────────────────────
        // Rendering
        // ─────────────────────────────────────────────────────────────

        private void RenderScr()
        {
            if ( ScrFile == null )
                return;

            int zoomFactor = ZoomLevel;
            bool enableGrid = ShowGrid && zoomFactor >= 2;

            RenderResult result;

            bool useDebug = SelectedDebugMode != ScrDebugMode.None;

            if ( useDebug )
            {
                var renderer = new ScrDebugRenderer();
                result = renderer.Render(
                    ScrFile ,
                    SelectedDebugMode ,
                    zoomFactor ,
                    enableGrid ,
                    ShowInvisibleTiles
                );
            }
            else
            {
                if ( CgxFile == null || ColFile == null )
                    return;

                var renderer = new ScrRenderer(
                    ScrFile,
                    CgxFile,
                    ColFile,
                    enableGrid,
                    ShowInvisibleTiles
                );

                result = renderer.Render( zoomFactor );
            }

            _lastRenderResult = result;
            Bitmap = BitmapFactory.FromRenderResult( result );
        }

        // ─────────────────────────────────────────────────────────────
        // PNG Export
        // ─────────────────────────────────────────────────────────────

        public void SavePng(string path)
        {
            if ( ScrFile == null )
                return;

            int zoomFactor = ZoomLevel;

            RenderResult result;

            bool useDebug = SelectedDebugMode != ScrDebugMode.None;

            if ( useDebug )
            {
                var renderer = new ScrDebugRenderer();
                result = renderer.Render(
                    ScrFile ,
                    SelectedDebugMode ,
                    zoomFactor ,
                    showGrid: false ,
                    ShowInvisibleTiles
                );
            }
            else
            {
                if ( CgxFile == null || ColFile == null )
                    return;

                var renderer = new ScrRenderer(
                    ScrFile,
                    CgxFile,
                    ColFile,
                    showGrid: false,
                    ShowInvisibleTiles
                );

                result = renderer.Render( zoomFactor );
            }

            BitmapFactory.SavePng( result , path );
        }
    }
}
