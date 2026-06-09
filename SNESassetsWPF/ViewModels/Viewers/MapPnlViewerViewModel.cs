using SNESassetsWPF.Formats;
using SNESassetsWPF.Helpers;
using SNESassetsWPF.Models;
using SNESassetsWPF.Rendering;
using System;
using System.Collections.ObjectModel;
using System.Windows.Media.Imaging;

namespace SNESassetsWPF.ViewModels
{
    public class MapPnlViewerViewModel : ViewModelBase
    {
        public PaletteViewModel Palette { get; }

        public ReadOnlyCollection<PaletteEntry> ActivePalette =>
            Palette?.ActivePalette;

        // ─────────────────────────────────────────────
        // Asset References
        // ─────────────────────────────────────────────
        private PnlFile _pnl;
        public PnlFile CurrentPnl
        {
            get => _pnl;
            set
            {
                _pnl = value;
                OnPropertyChanged();

                if ( value == null )
                {
                    PnlBitmap = null;
                    return;
                }

                RenderAll();
            }
        }

        private MapFile _map;
        public MapFile CurrentMap
        {
            get => _map;
            set
            {
                _map = value;
                OnPropertyChanged();

                if ( value == null )
                {
                    MapBitmap = null;
                    return;
                }

                RenderAll();
            }
        }

        private CgxFile _cgx;
        public CgxFile CurrentCgx
        {
            get => _cgx;
            set
            {
                _cgx = value;
                OnPropertyChanged();

                if ( value == null )
                {
                    PnlBitmap = null;
                    MapBitmap = null;
                    return;
                }

                RenderAll();
            }
        }

        private ColFile _col;
        public ColFile CurrentCol
        {
            get => _col;
            set
            {
                _col = value;
                OnPropertyChanged();

                if ( value == null )
                {
                    PnlBitmap = null;
                    MapBitmap = null;
                    return;
                }

                RenderAll();
            }
        }


        // ─────────────────────────────────────────────
        // Zoom + Grid + Debug
        // ─────────────────────────────────────────────
        private int _zoomLevel = 1;
        public int ZoomLevel
        {
            get => _zoomLevel;
            set
            {
                _zoomLevel = Math.Max( 1 , value );
                OnPropertyChanged();
                OnPropertyChanged( nameof( IsGridToggleEnabled ) );

                if ( _zoomLevel == 1 )
                    ShowGrid = false;

                RenderAll();
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
                RenderAll();
            }
        }

        private bool _showDebug;
        public bool ShowDebug
        {
            get => _showDebug;
            set
            {
                _showDebug = value;
                OnPropertyChanged();
                RenderAll();
            }
        }

        // ─────────────────────────────────────────────
        // Two separate bitmaps
        // ─────────────────────────────────────────────
        private WriteableBitmap _pnlBitmap;
        public WriteableBitmap PnlBitmap
        {
            get => _pnlBitmap;
            private set
            {
                _pnlBitmap = value;
                OnPropertyChanged();
            }
        }

        private WriteableBitmap _mapBitmap;
        public WriteableBitmap MapBitmap
        {
            get => _mapBitmap;
            private set
            {
                _mapBitmap = value;
                OnPropertyChanged();
            }
        }

        public MapPnlViewerViewModel()
        {
            Palette = new PaletteViewModel();
            Palette.PaletteChanged += RenderAll;
        }

        // ─────────────────────────────────────────────
        // Rendering
        // ─────────────────────────────────────────────
        private void RenderAll()
        {
            RenderPnl();
            RenderMap();
        }

        public void RenderPnl()
        {
            if ( CurrentPnl == null || CurrentCgx == null || CurrentCol == null )
            {
                PnlBitmap = null;
                return;
            }

            var buffer = PnlRenderer.Render(
                pnl: CurrentPnl,
                cgx: CurrentCgx,
                col: CurrentCol,
                map: CurrentMap,
                zoom: ZoomLevel,
                showGrid: ShowGrid,
                showDebug: ShowDebug,
                width: out int width,
                height: out int height
            );


            PnlBitmap = BitmapFactory.FromRenderResult(
                new RenderResult
                {
                    Buffer = buffer ,
                    Width = width ,
                    Height = height
                }
            );
        }

        public void RenderMap()
        {
            if ( CurrentMap == null ||
                CurrentPnl == null ||
                CurrentCgx == null ||
                CurrentCol == null )
            {
                MapBitmap = null;
                return;
            }

            var buffer = MapRenderer.Render(
                map: CurrentMap,
                pnl: CurrentPnl,
                cgx: CurrentCgx,
                col: CurrentCol,
                zoom: ZoomLevel,
                showGrid: ShowGrid,
                showDebug: ShowDebug,
                width: out int width,
                height: out int height
            );

            MapBitmap = BitmapFactory.FromRenderResult(
                 new RenderResult
                 {
                     Buffer = buffer ,
                     Width = width ,
                     Height = height
                 }
             );
        }

        // ─────────────────────────────────────────────
        // PNG Export
        // ─────────────────────────────────────────────
        public void ExportPnlPng(string path)
        {
            if ( PnlBitmap == null )
                return;

            var r = new RenderResult
            {
                Width = PnlBitmap.PixelWidth,
                Height = PnlBitmap.PixelHeight,
                Buffer = new byte[PnlBitmap.PixelWidth * PnlBitmap.PixelHeight * 4]
            };

            PnlBitmap.CopyPixels( r.Buffer , r.Width * 4 , 0 );
            BitmapFactory.SavePng( r , path );
        }

        public void ExportMapPng(string path)
        {
            if ( MapBitmap == null )
                return;

            var r = new RenderResult
            {
                Width = MapBitmap.PixelWidth,
                Height = MapBitmap.PixelHeight,
                Buffer = new byte[MapBitmap.PixelWidth * MapBitmap.PixelHeight * 4]
            };

            MapBitmap.CopyPixels( r.Buffer , r.Width * 4 , 0 );
            BitmapFactory.SavePng( r , path );
        }
    }
}
