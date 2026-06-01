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

                // Reset debug mode when a new SCR loads
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
        // Debug mode + options
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
        // SCR Endianness Toggle (THIS IS THE IMPORTANT PART)
        // ─────────────────────────────────────────────────────────────

        private bool _scrLittleEndian;
        public bool ScrLittleEndian
        {
            get => _scrLittleEndian;
            set
            {
                _scrLittleEndian = value;

                // Update parser mode
                ScrFileParser.DebugEndianMode =
                    value ? ScrEndian.BigEndian : ScrEndian.LittleEndian;

                OnPropertyChanged();

                // Re-parse SCR file using new endian mode
                if ( ScrFile?.RawBytes != null )
                {
                    ScrFile = ScrFileParser.Parse(
                        ScrFile.RawBytes ,
                        ScrFile.WidthTiles ,
                        ScrFile.HeightTiles
                    );
                }

                // NOW render the updated SCR
                RenderScr();
            }
        }

        // ─────────────────────────────────────────────────────────────
        // Zoom + Grid
        // ─────────────────────────────────────────────────────────────

        private int _zoomLevel = 1; // 1x, 2x, 3x, 4x
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

        // Optional: if UI still uses 100/200/300/400
        public int ZoomPercent
        {
            get => _zoomLevel * 100;
            set
            {
                int level = value / 100;
                ZoomLevel = level;
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

            int zoomFactor = ZoomLevel;                 // ← FIXED
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
                    enableGrid
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
                    enableGrid
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

            int zoomFactor = ZoomLevel;   // ← FIXED

            RenderResult result;

            bool useDebug = SelectedDebugMode != ScrDebugMode.None;

            if ( useDebug )
            {
                var renderer = new ScrDebugRenderer();
                result = renderer.Render(
                    ScrFile ,
                    SelectedDebugMode ,
                    zoomFactor ,
                    showGrid: false
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
            showGrid: false
        );

                result = renderer.Render( zoomFactor );
            }

            BitmapFactory.SavePng( result , path );
        }

    }
}
