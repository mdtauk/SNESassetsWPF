using SNESassetsWPF.Enums;
using SNESassetsWPF.Formats;
using SNESassetsWPF.Models;
using SNESassetsWPF.Rendering;
using System;
using System.Collections.ObjectModel;
using System.Windows.Media.Imaging;

namespace SNESassetsWPF.ViewModels
{
    public class ScrViewerViewModel : ViewModelBase
    {
        public PaletteViewModel Palette { get; }

        public ReadOnlyCollection<PaletteEntry> ActivePalette =>
            Palette?.ActivePalette;

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

        // ─────────────────────────────────────────────
        // Debug Mode (enum-based)
        // ─────────────────────────────────────────────
        private ScrDebugMode _selectedDebugMode = ScrDebugMode.None;
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

        public ScrDebugMode[] DebugModes { get; } =
        {
            ScrDebugMode.None,
            ScrDebugMode.TileIndex,
            ScrDebugMode.PaletteRowIndex,
            ScrDebugMode.Flip,
            ScrDebugMode.Priority
        };

        // ─────────────────────────────────────────────
        // Zoom, Visibility + Grid
        // ─────────────────────────────────────────────
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

                if ( _zoomLevel == 1 )
                    ShowGrid = false;

                RenderScr();
            }
        }


        private bool _showInvisible;
        public bool ShowInvisible
        {
            get => _showInvisible;
            set
            {
                _showInvisible = value;
                OnPropertyChanged();
                RenderScr();
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

        // ─────────────────────────────────────────────
        // Output bitmap
        // ─────────────────────────────────────────────
        private WriteableBitmap _scrBitmap;
        public WriteableBitmap ScrBitmap
        {
            get => _scrBitmap;
            private set
            {
                _scrBitmap = value;
                OnPropertyChanged();
            }
        }

        public ScrViewerViewModel(PaletteViewModel palette)
        {
            Palette = palette;
            Palette.PaletteChanged += RenderScr;
        }

        // ─────────────────────────────────────────────
        // Rendering
        // ─────────────────────────────────────────────
        public void RenderScr()
        {
            if ( ScrFile == null )
            {
                ScrBitmap = null;
                return;
            }

            var result = ScrRenderer.Render(
                ScrFile,
                CgxFile,        // may be null → renderer handles fallback
                ColFile,        // may be null → renderer handles fallback
                zoom: ZoomLevel,
                showGrid: ShowGrid,
                showInvisible: ShowInvisible ,
                debugMode: SelectedDebugMode
            );

            ScrBitmap = BitmapFactory.FromRenderResult( result );
        }

        // ─────────────────────────────────────────────
        // PNG Export
        // ─────────────────────────────────────────────
        public void ExportPng(string path , int zoom)
        {
            if ( ScrFile == null )
                return;

            var result = ScrRenderer.Render(
                ScrFile,
                CgxFile,
                ColFile,
                zoom: zoom,
                showGrid: false,
                showInvisible: ShowInvisible ,
                debugMode: SelectedDebugMode
            );

            BitmapFactory.SavePng( result , path );
        }
    }
}
