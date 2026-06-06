using SNESassetsWPF.Formats;
using SNESassetsWPF.Rendering;
using SNESassetsWPF.ViewModels;
using System.Windows.Media.Imaging;

public class MapPnlViewerViewModel : ViewModelBase
{
    private PnlFile _pnl;
    private MapFile _map;
    private CgxFile _cgx;
    private ColFile _col;

    private int _zoomLevel = 1;
    private bool _showDebugOverlay;
    private bool _showGrid;

    private WriteableBitmap _pnlBitmap;
    private WriteableBitmap _mapBitmap;

    // -------------------------------------------------------
    // PUBLIC PROPERTIES
    // -------------------------------------------------------

    public PnlFile CurrentPnl
    {
        get => _pnl;
        set
        {
            _pnl = value;
            OnPropertyChanged();
            RenderAll();
        }
    }

    public MapFile CurrentMap
    {
        get => _map;
        set
        {
            _map = value;
            OnPropertyChanged();
            RenderAll();
        }
    }

    public CgxFile CurrentCgx
    {
        get => _cgx;
        set
        {
            _cgx = value;
            OnPropertyChanged();
            RenderAll();
        }
    }

    public ColFile CurrentCol
    {
        get => _col;
        set
        {
            _col = value;
            OnPropertyChanged();
            RenderAll();
        }
    }

    public bool ShowDebugOverlay
    {
        get => _showDebugOverlay;
        set
        {
            _showDebugOverlay = value;
            OnPropertyChanged();
            RenderAll();
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
                RenderAll();
            }
        }
    }

    public bool IsGridToggleEnabled => ZoomLevel >= 2;

    public int ZoomLevel
    {
        get => _zoomLevel;
        set
        {
            _zoomLevel = value < 1 ? 1 : value;
            OnPropertyChanged();
            RenderAll();

            OnPropertyChanged( nameof( IsGridToggleEnabled ) );

            if ( _zoomLevel == 1 )
                ShowGrid = false;
        }
    }

    public WriteableBitmap PnlBitmap
    {
        get => _pnlBitmap;
        private set
        {
            _pnlBitmap = value;
            OnPropertyChanged();
        }
    }

    public WriteableBitmap MapBitmap
    {
        get => _mapBitmap;
        private set
        {
            _mapBitmap = value;
            OnPropertyChanged();
        }
    }

    // -------------------------------------------------------
    // RENDERING
    // -------------------------------------------------------

    private void RenderAll()
    {
        RenderPnl();
        RenderMap();
    }

    private void RenderPnl()
    {
        if ( _pnl == null || _cgx == null || _col == null )
        {
            PnlBitmap = null;
            return;
        }

        var renderer = new PnlRenderer();

        var result = renderer.Render(
            pnl: _pnl,
            cgx: _cgx,
            col: _col,
            zoom: ZoomLevel,
            forceVisible: false,
            showGrid: ShowGrid,
            debugMode: ShowDebugOverlay,
            debugTint: ShowDebugOverlay
        );

        PnlBitmap = BitmapFactory.FromRenderResult( result );
    }

    private void RenderMap()
    {
        // MAP requires PNL + CGX + COL to render correctly
        if ( _map == null )
        {
            MapBitmap = null;
            return;
        }

        if ( _pnl == null || _cgx == null || _col == null )
        {
            // Render MAP-only debug view (optional)
            MapBitmap = RenderMapWithoutPnl();
            return;
        }

        var renderer = new MapRenderer();

        var result = renderer.Render(
            map: _map,
            pnl: _pnl,
            cgx: _cgx,
            col: _col,
            zoom: ZoomLevel,
            showGrid: ShowGrid,
            debugMode: ShowDebugOverlay,
            debugTint: ShowDebugOverlay
        );

        MapBitmap = BitmapFactory.FromRenderResult( result );
    }

    // -------------------------------------------------------
    // MAP WITHOUT PNL SUPPORT
    // -------------------------------------------------------
    private WriteableBitmap RenderMapWithoutPnl()
    {
        // Render a simple debug-only MAP grid
        // Each cell is a colored block based on PNL group index
        // This allows MAP files to be viewed even without PNL

        int cellSize = 16 * ZoomLevel;
        int width = _map.Width * cellSize;
        int height = _map.Height * cellSize;

        var buffer = new byte[width * height * 4];

        for ( int y = 0 ; y < _map.Height ; y++ )
        {
            for ( int x = 0 ; x < _map.Width ; x++ )
            {
                var cell = _map.Cells[x, y];
                if ( cell == null )
                    continue;

                var color = DebugColors.GetColorForPnlTile(cell.PnlGroupIndex);

                for ( int py = 0 ; py < cellSize ; py++ )
                {
                    for ( int px = 0 ; px < cellSize ; px++ )
                    {
                        int dx = x * cellSize + px;
                        int dy = y * cellSize + py;

                        int idx = (dy * width + dx) * 4;
                        buffer[idx + 0] = color.B;
                        buffer[idx + 1] = color.G;
                        buffer[idx + 2] = color.R;
                        buffer[idx + 3] = 255;
                    }
                }
            }
        }

        return BitmapFactory.FromRenderResult( new RenderResult
        {
            Buffer = buffer ,
            Width = width ,
            Height = height
        } );
    }

    // -------------------------------------------------------
    // PNG EXPORTS
    // -------------------------------------------------------

    public void SavePnlPng(string path)
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

    public void SaveMapPng(string path)
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
