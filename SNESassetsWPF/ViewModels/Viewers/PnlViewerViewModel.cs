using SNESassetsWPF.Enums;
using SNESassetsWPF.Formats;
using SNESassetsWPF.Rendering;
using SNESassetsWPF.Services;
using System.Diagnostics;
using System.Windows.Media.Imaging;

namespace SNESassetsWPF.ViewModels
{
    public class PnlViewerViewModel : ViewModelBase
    {
        // ─────────────────────────────────────────────────────────────
        // CGX and COL reference
        // ─────────────────────────────────────────────────────────────
        private CgxFile _cgxFile;
        public CgxFile CgxFile
        {
            get => _cgxFile;
            set
            {
                _cgxFile = value;
                OnPropertyChanged();
                RenderPnl();
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
                RenderPnl();
            }
        }

        private bool _showInvisibleTiles = false;
        public bool ShowInvisibleTiles
        {
            get => _showInvisibleTiles;
            set
            {
                _showInvisibleTiles = value;
                OnPropertyChanged();
                RenderPnl();
            }
        }



        // ─────────────────────────────────────────────────────────────
        // PNL reference
        // ─────────────────────────────────────────────────────────────

        private PnlFile _pnlFile;
        public PnlFile PnlFile
        {
            get => _pnlFile;
            set
            {
                _pnlFile = value;
                OnPropertyChanged();

                // ─────────────────────────────────────────────
                // DEBUG: Inspect parsed PNL
                // ─────────────────────────────────────────────
                Debug.WriteLine( $"PNL MetaWidth  = {_pnlFile.MetaWidth}" );
                Debug.WriteLine( $"PNL MetaHeight = {_pnlFile.MetaHeight}" );
                Debug.WriteLine( $"PNL Mode7      = {_pnlFile.Mode7Enabled}" );
                Debug.WriteLine( $"Tiles array    = {_pnlFile.Tiles.GetLength( 0 )} x {_pnlFile.Tiles.GetLength( 1 )}" );

                for ( int i = 0 ; i < 10 ; i++ )
                {
                    int x = i % 32;
                    int y = i / 32;
                    var t = _pnlFile.Tiles[x, y];

                    Debug.WriteLine(
                        $"Tile[{x},{y}] ID={t.TileId} Pal={t.PaletteRow} H={t.HFlip} V={t.VFlip} Present={t.Present}"
                    );
                }
                // ─────────────────────────────────────────────

                SelectedDebugMode = PnlDebugMode.None;
                RenderPnl();


                SelectedDebugMode = PnlDebugMode.None;
                RenderPnl();
            }
        }

        // ─────────────────────────────────────────────────────────────
        // Debug mode
        // ─────────────────────────────────────────────────────────────

        private PnlDebugMode _selectedDebugMode = PnlDebugMode.None;
        public PnlDebugMode SelectedDebugMode
        {
            get => _selectedDebugMode;
            set
            {
                _selectedDebugMode = value;
                OnPropertyChanged();
                RenderPnl();
            }
        }

        public static PnlDebugMode[] DebugModes { get; } =
        {
            PnlDebugMode.None,
            PnlDebugMode.PatternDebug,
            PnlDebugMode.OverlayDebug
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
                RenderPnl();
            }
        }

        public int ZoomPercent
        {
            get => _zoomLevel * 100;
            set
            {
                ZoomLevel = value / 100;
                OnPropertyChanged();
                RenderPnl();
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
                RenderPnl();
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

        public PnlViewerViewModel()
        {
            SelectedDebugMode = PnlDebugMode.None;
        }

        // ─────────────────────────────────────────────────────────────
        // Rendering
        // ─────────────────────────────────────────────────────────────

        private void RenderPnl()
        {
            if ( PnlFile == null )
                return;

            // Prevent early blank renders before CGX/COL are assigned
            if ( ( CgxFile == null || ColFile == null ) &&
                SelectedDebugMode != PnlDebugMode.PatternDebug )
            {
                Debug.WriteLine( "RenderPnl skipped: CGX/COL not ready" );
                return;
            }


            int zoomFactor = ZoomLevel;
            bool enableGrid = ShowGrid && zoomFactor >= 2;

            RenderResult result;

            switch ( SelectedDebugMode )
            {
                case PnlDebugMode.PatternDebug:
                    {
                        var patterns = PnlPatternExtractor.Extract(PnlFile);
                        result = PnlDebugRenderer.Render( PnlFile , patterns , tileSize: 8 * zoomFactor );
                        break;
                    }

                case PnlDebugMode.OverlayDebug:
                    {
                        // Future mode:
                        // 1. Render normal PNL
                        // 2. Render debug overlay
                        // 3. Composite
                        result = RenderOverlayMode( PnlFile , zoomFactor , enableGrid );
                        break;
                    }

                default:
                    {
                        // Normal PNL renderer (to be implemented)
                        var renderer = new PnlRenderer(
                            PnlFile,
                            CgxFile,
                            ColFile,
                            enableGrid,
                            ShowInvisibleTiles
                        );

                        result = renderer.Render( zoomFactor );
                        break;
                    }
            }

            _lastRenderResult = result;
            Bitmap = BitmapFactory.FromRenderResult( result );
        }

        private RenderResult RenderOverlayMode(PnlFile pnl , int zoomFactor , bool enableGrid)
        {
            // Placeholder until overlay renderer exists
            var patterns = PnlPatternExtractor.Extract(pnl);

            // 1. Base render
            var baseRenderer = new PnlRenderer(
                pnl,
                CgxFile,
                ColFile,
                enableGrid,
                showInvisibleTiles: false
            );

            var baseResult = baseRenderer.Render(zoomFactor);

            // 2. Debug overlay
            var overlay = PnlDebugRenderer.Render(pnl, patterns, tileSize: 8 * zoomFactor);

            // 3. Composite overlay onto base
            PnlOverlayComposer.ApplyOverlay( baseResult , overlay );

            return baseResult;
        }

        // ─────────────────────────────────────────────────────────────
        // PNG Export
        // ─────────────────────────────────────────────────────────────

        public void SavePng(string path)
        {
            if ( PnlFile == null )
                return;

            int zoomFactor = ZoomLevel;

            RenderResult result;

            switch ( SelectedDebugMode )
            {
                case PnlDebugMode.PatternDebug:
                    {
                        var patterns = PnlPatternExtractor.Extract(PnlFile);
                        result = PnlDebugRenderer.Render( PnlFile , patterns , tileSize: 8 * zoomFactor );
                        break;
                    }

                case PnlDebugMode.OverlayDebug:
                    {
                        if ( CgxFile == null || ColFile == null )
                            return;

                        result = RenderOverlayMode( PnlFile , zoomFactor , false );
                        break;
                    }


                default:
                    {
                        if ( CgxFile == null || ColFile == null )
                            return;

                        var renderer = new PnlRenderer(
                        PnlFile,
                        CgxFile,
                        ColFile,
                        false,
                        ShowInvisibleTiles
                    );

                        result = renderer.Render( zoomFactor );
                        break;
                    }

            }

            BitmapFactory.SavePng( result , path );
        }
    }
}
