using SNESassetsWPF.Formats;
using SNESassetsWPF.Rendering;
using System.Diagnostics;
using System.Windows.Media.Imaging;

namespace SNESassetsWPF.ViewModels
{
    /// <summary>
    /// Debug overlay modes for SCR rendering.
    /// </summary>
    public enum ScrDebugMode
    {
        None,
        TileIndex,
        PaletteIndex,
        Flags
    }

    /// <summary>
    /// ViewModel responsible for rendering SCR files using either the
    /// normal renderer or the debug renderer. Handles zoom, grid display,
    /// debug overlays, palette-source toggling, and PNG export.
    /// </summary>
    public class ScrViewerViewModel : ViewModelBase
    {
        public static ScrDebugMode[] DebugModes { get; } =
        {
            ScrDebugMode.TileIndex,
            ScrDebugMode.PaletteIndex,
            ScrDebugMode.Flags
        };

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
        // Palette source toggle (SCR vs CGX)
        // ─────────────────────────────────────────────────────────────

        private bool _useScrPaletteRow = true;

        /// <summary>
        /// When true: use SCR tile's palette row (normal SNES behaviour).
        /// When false: use CGX tile's palette group (editor-side behaviour).
        /// </summary>
        public bool UseScrPaletteRow
        {
            get => _useScrPaletteRow;
            set
            {
                _useScrPaletteRow = value;
                Debug.WriteLine( $"UseScrPaletteRow changed → {value}" );
                OnPropertyChanged();
                RenderScr();
            }
        }

        // ─────────────────────────────────────────────────────────────
        // Debug mode
        // ─────────────────────────────────────────────────────────────

        private bool _debugMode;
        public bool DebugMode
        {
            get => _debugMode;
            set
            {
                _debugMode = value;
                OnPropertyChanged();
                OnPropertyChanged( nameof( IsDebugOptionsVisible ) );
                RenderScr();
            }
        }

        public bool IsDebugOptionsVisible => DebugMode;

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

        // ─────────────────────────────────────────────────────────────
        // Zoom + Grid
        // ─────────────────────────────────────────────────────────────

        private int _zoom = 100;
        public int Zoom
        {
            get => _zoom;
            set
            {
                if ( value != 100 && value != 200 && value != 300 && value != 400 )
                    return;

                _zoom = value;
                OnPropertyChanged();
                OnPropertyChanged( nameof( IsGridToggleEnabled ) );
                RenderScr();
            }
        }

        public bool IsGridToggleEnabled => Zoom >= 200;

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
        // Rendering
        // ─────────────────────────────────────────────────────────────

        private void RenderScr()
        {
            if ( ScrFile == null )
                return;

            int zoomFactor = Zoom / 100;
            bool enableGrid = ShowGrid && zoomFactor >= 2;

            RenderResult result;

            if ( DebugMode )
            {
                var renderer = new ScrDebugRenderer(
                    ScrFile,
                    SelectedDebugMode,
                    enableGrid);

                result = renderer.Render( zoomFactor );
            }
            else
            {
                if ( CgxFile == null || ColFile == null )
                    return;

                var renderer = new ScrRenderer(
                    ScrFile,
                    CgxFile,
                    ColFile,
                    enableGrid
                );

                result = renderer.Render( zoomFactor );
            }

            _lastRenderResult = result;

            Debug.WriteLine(
                $"SCR render: {ScrFile.WidthTiles}×{ScrFile.HeightTiles} tiles, " +
                $"zoom={zoomFactor}, final={result.Width}×{result.Height}"
            );

            Bitmap = BitmapFactory.FromRenderResult( result );
        }

        // ─────────────────────────────────────────────────────────────
        // PNG Export
        // ─────────────────────────────────────────────────────────────

        public void SavePng(string path)
        {
            if ( ScrFile == null )
                return;

            int zoomFactor = Zoom / 100;

            RenderResult result;

            if ( DebugMode )
            {
                var renderer = new ScrDebugRenderer(
                    ScrFile,
                    SelectedDebugMode,
                    showGrid: false);

                result = renderer.Render( zoomFactor );
            }
            else
            {
                if ( CgxFile == null || ColFile == null )
                    return;

                var renderer = new ScrRenderer(
                    ScrFile,
                    CgxFile,
                    ColFile,
                    showGrid: false
                );

                result = renderer.Render( zoomFactor );
            }

            BitmapFactory.SavePng( result , path );
        }
    }
}
