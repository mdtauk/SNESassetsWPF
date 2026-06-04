using SNESassetsWPF.Enums;
using SNESassetsWPF.Formats;
using SNESassetsWPF.Helpers;
using SNESassetsWPF.Rendering;
using SNESassetsWPF.Services;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows.Media.Imaging;

namespace SNESassetsWPF.ViewModels
{
    public class MapPnlViewerViewModel : ViewModelBase
    {
        // ─────────────────────────────────────────────────────────────
        // Helpers
        // ─────────────────────────────────────────────────────────────

        private bool HasMapOrPnl => _pnlFile != null || _mapFile != null;

        private void ClearRender()
        {
            _lastRender = null;
            Bitmap = null;
        }

        // ─────────────────────────────────────────────────────────────
        // Render Mode (ComboBox-bound)
        // ─────────────────────────────────────────────────────────────

        private MapPnlViewModes _renderMode = MapPnlViewModes.Auto;
        public MapPnlViewModes RenderMode
        {
            get => _renderMode;
            set
            {
                if ( _renderMode == value )
                    return;

                _renderMode = value;
                OnPropertyChanged();
                OnPropertyChanged( nameof( AvailableModes ) );

                // Only render if we actually have something to show
                if ( HasMapOrPnl )
                    Render();
            }
        }

        private readonly ObservableCollection<MapPnlViewModeItem> _availableModes = new();
        public ObservableCollection<MapPnlViewModeItem> AvailableModes => _availableModes;

        private void RefreshAvailableModes()
        {
            _availableModes.Clear();

            foreach ( var m in Enum.GetValues( typeof( MapPnlViewModes ) ).Cast<MapPnlViewModes>() )
            {
                _availableModes.Add( new MapPnlViewModeItem
                {
                    Mode = m ,
                    DisplayName = MapPnlViewModeValues.GetName( m ) ,
                    IsEnabled = CanUse( m )
                } );
            }

            OnPropertyChanged( nameof( AvailableModes ) );
        }

        // ─────────────────────────────────────────────────────────────
        // CGX + COL
        // ─────────────────────────────────────────────────────────────

        private CgxFile _cgxFile;
        public CgxFile CgxFile
        {
            get => _cgxFile;
            set
            {
                _cgxFile = value;
                OnPropertyChanged();
                RefreshAvailableModes();

                // Only re-render if there is a MAP or PNL loaded
                if ( HasMapOrPnl )
                    Render();
                else
                    ClearRender();
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
                RefreshAvailableModes();

                // Only re-render if there is a MAP or PNL loaded
                if ( HasMapOrPnl )
                    Render();
                else
                    ClearRender();
            }
        }

        // ─────────────────────────────────────────────────────────────
        // PNL + MAP
        // ─────────────────────────────────────────────────────────────

        private PnlFile _pnlFile;
        public PnlFile PnlFile
        {
            get => _pnlFile;
            set
            {
                _pnlFile = value;
                OnPropertyChanged();
                RefreshAvailableModes();

                if ( HasMapOrPnl )
                    Render();
                else
                    ClearRender();
            }
        }

        private MapFile _mapFile;
        public MapFile MapFile
        {
            get => _mapFile;
            set
            {
                _mapFile = value;
                OnPropertyChanged();
                RefreshAvailableModes();

                if ( HasMapOrPnl )
                    Render();
                else
                    ClearRender();
            }
        }

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

                if ( HasMapOrPnl )
                    Render();
            }
        }

        private bool _showGrid;
        public bool ShowGrid
        {
            get => _showGrid;
            set
            {
                _showGrid = value;
                OnPropertyChanged();

                if ( HasMapOrPnl )
                    Render();
            }
        }

        // ─────────────────────────────────────────────────────────────
        // Bitmap
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

        private RenderResult _lastRender;
        public RenderResult LastRender => _lastRender;

        // ─────────────────────────────────────────────────────────────
        // Constructor
        // ─────────────────────────────────────────────────────────────

        public MapPnlViewerViewModel() { }

        // ─────────────────────────────────────────────────────────────
        // Main Render Dispatcher
        // ─────────────────────────────────────────────────────────────

        private void Render()
        {
            // If nothing to show, clear and bail
            if ( !HasMapOrPnl )
            {
                ClearRender();
                return;
            }

            int zoom = ZoomLevel;
            bool grid = ShowGrid && zoom >= 2;

            // Decide effective mode without causing recursive property sets
            var mode = _renderMode;

            if ( mode == MapPnlViewModes.Auto )
            {
                if ( _pnlFile != null && _mapFile == null )
                    mode = MapPnlViewModes.PnlDebug;
                else if ( _mapFile != null && _pnlFile == null )
                    mode = MapPnlViewModes.MapDebug;
                else if ( _mapFile != null && _pnlFile != null && _cgxFile != null && _colFile != null )
                    mode = MapPnlViewModes.MapFull;
                else if ( _pnlFile != null )
                    mode = MapPnlViewModes.PnlDebug;
            }

            // If chosen mode is not valid, clear and bail
            if ( !CanUse( mode ) )
            {
                ClearRender();
                return;
            }

            // Keep backing field in sync without re-entering Render()
            if ( mode != _renderMode )
            {
                _renderMode = mode;
                OnPropertyChanged( nameof( RenderMode ) );
                RefreshAvailableModes();
            }

            RenderResult result = null;

            switch ( mode )
            {
                case MapPnlViewModes.PnlDebug:
                    if ( _pnlFile != null )
                        result = new PnlDebugRenderer( _pnlFile ).Render( zoom );
                    break;

                case MapPnlViewModes.MapDebug:
                    if ( _mapFile != null )
                        result = new MapRenderer( _mapFile ).RenderDebug( zoom );
                    break;

                case MapPnlViewModes.MapFull:
                    if ( _mapFile != null && _pnlFile != null && _cgxFile != null && _colFile != null )
                    {
                        result = new MapPnlRenderer(
                            map: _mapFile ,
                            pnl: _pnlFile ,
                            cgx: _cgxFile ,
                            col: _colFile ,
                            showGrid: grid
                        ).Render( zoom );
                    }
                    break;

                case MapPnlViewModes.PnlFull:
                    if ( _pnlFile != null && _cgxFile != null && _colFile != null )
                        result = new PnlRenderer( _pnlFile , _cgxFile , _colFile ).Render( zoom );
                    break;
            }

            if ( result == null )
            {
                ClearRender();
                return;
            }

            _lastRender = result;
            Bitmap = BitmapFactory.FromRenderResult( result );
        }

        // ─────────────────────────────────────────────────────────────
        // Mode Availability Logic
        // ─────────────────────────────────────────────────────────────

        public bool CanUse(MapPnlViewModes mode)
        {
            Debug.WriteLine(
                $"CanUse({mode})  PNL={_pnlFile != null}  MAP={_mapFile != null}  CGX={_cgxFile != null}  COL={_colFile != null}" );

            return mode switch
            {
                MapPnlViewModes.PnlDebug => _pnlFile != null,
                MapPnlViewModes.MapDebug => _mapFile != null,
                MapPnlViewModes.MapFull =>
                    _mapFile != null && _pnlFile != null &&
                    _cgxFile != null && _colFile != null,
                MapPnlViewModes.PnlFull =>
                    _pnlFile != null && _cgxFile != null && _colFile != null,
                MapPnlViewModes.Auto => true,
                _ => false
            };
        }

        // ─────────────────────────────────────────────────────────────
        // PNG Export
        // ─────────────────────────────────────────────────────────────

        public void SavePng(string path)
        {
            if ( _lastRender == null ) return;
            BitmapFactory.SavePng( _lastRender , path );
        }
    }
}
