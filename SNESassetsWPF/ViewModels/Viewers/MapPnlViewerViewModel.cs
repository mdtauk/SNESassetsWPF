using SNESassetsWPF.Formats;
using SNESassetsWPF.Rendering;
using SNESassetsWPF.ViewModels;
using System.Windows.Forms;
using System.Windows.Media.Imaging;

public class MapPnlViewerViewModel : ViewModelBase
{
    private PnlFile _pnl;
    private MapFile _map;
    private CgxFile _cgx;
    private ColFile _col;

    private bool _debugOverlayEnabled;
    private int _zoomLevel = 1;

    private WriteableBitmap _pnlBitmap;
    private WriteableBitmap _mapBitmap;

    private bool _showGrid = false;




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

    private bool _showDebugOverlay;
    public bool ShowDebugOverlay
    {
        get => _showDebugOverlay;
        set
        {
            _showDebugOverlay = value;
            OnPropertyChanged();
            RenderPnl();   // re-render when toggled
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

    private void RenderAll()
    {
        RenderPnl();
        RenderMap();
    }

    private void RenderPnl()
    {
        if ( _pnl == null || _cgx == null || _col == null )
        {
            System.Diagnostics.Debug.WriteLine( "RenderPnl: missing dependency" );
            PnlBitmap = null;
            return;
        }

        System.Diagnostics.Debug.WriteLine( "RenderPnl: starting" );

        var renderer = new PnlRenderer();

        var result = renderer.Render(
            _pnl,
            _cgx,
            _col,
            zoom: ZoomLevel,
            forceVisible: false,
            showGrid: ShowGrid,
            debugMode: ShowDebugOverlay,
            debugTint: ShowDebugOverlay
        );

        System.Diagnostics.Debug.WriteLine( $"RenderPnl: result = {result.Width}x{result.Height}" );

        PnlBitmap = BitmapFactory.FromRenderResult( result );

        System.Diagnostics.Debug.WriteLine( "RenderPnl: bitmap assigned" );
    }



    private void RenderMap()
    {
        //if ( _map == null || _pnl == null || _cgx == null || _col == null )
        //{
        //    MapBitmap = null;
        //    return;
        //}

        //// Stub: you’ll implement MapRenderer with this signature:
        //// RenderResult Render(MapFile map, PnlFile pnl, CgxFile cgx, ColFile col)
        //var renderer = new MapRenderer();

        //var result = renderer.Render(_map, _pnl, _cgx, _col);

        //MapBitmap = BitmapFactory.FromRenderResult( result );
    }





    // ---------------------------------------
    // GRID & ZOOM
    // ---------------------------------------
    public int ZoomLevel
    {
        get => _zoomLevel;
        set
        {
            _zoomLevel = value < 1 ? 1 : value;
            OnPropertyChanged();
            RenderAll();

            // Grid toggle only enabled at zoom >= 2
            OnPropertyChanged( nameof( IsGridToggleEnabled ) );

            // Auto-disable grid at 100%
            if ( _zoomLevel == 1 )
                ShowGrid = false;
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
                RenderPnl();
            }
        }
    }

    public bool IsGridToggleEnabled => ZoomLevel >= 2;



    // ---------------------------------------
    // PNG EXPORTS
    // ---------------------------------------
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



    public void SavePng(string path)
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
