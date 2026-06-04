using SNESassetsWPF.Formats;
using SNESassetsWPF.Models;
using System.Diagnostics;
using System.Windows.Media;

namespace SNESassetsWPF.Rendering
{
    public class MapPnlRenderer
    {
        private const int TileSize = 8;

        private readonly MapFile _map;
        private readonly PnlFile _pnl;
        private readonly CgxFile _cgx;
        private readonly ColFile _col;
        private readonly bool _showGrid;

        public MapPnlRenderer(
            MapFile map ,
            PnlFile pnl ,
            CgxFile cgx ,
            ColFile col ,
            bool showGrid)
        {
            _map = map;
            _pnl = pnl;
            _cgx = cgx;
            _col = col;
            _showGrid = showGrid;
        }

        public RenderResult Render(int zoom)
        {
            if ( zoom < 1 ) zoom = 1;

            int mw = _pnl.MetaWidth;
            int mh = _pnl.MetaHeight;

            int width  = _map.Width  * mw * TileSize * zoom;
            int height = _map.Height * mh * TileSize * zoom;

            var buffer = new byte[width * height * 4];

            // Clear to transparent
            for ( int i = 0 ; i < buffer.Length ; i += 4 )
                buffer[i + 3] = 0;

            for ( int my = 0 ; my < _map.Height ; my++ )
            {
                for ( int mx = 0 ; mx < _map.Width ; mx++ )
                {
                    var mapTile = _map.Tiles[mx, my];
                    if ( mapTile == null || mapTile.MetaTileIndex == 0 )
                        continue;

                    // Debug output — add here
                    //System.Diagnostics.Debug.WriteLine(
                    //    $"MAP({mx},{my}) MetaTileIndex={mapTile.MetaTileIndex}, " +
                    //    $"PNL.MetaTileCount={_pnl.MetaTileCount}, " +
                    //    $"CGX.BitDepth={_cgx.BitDepth}" );

                    DrawMetaTile( buffer , width , height , mx , my , mapTile , zoom );
                }
            }

            return new RenderResult
            {
                Buffer = buffer ,
                Width = width ,
                Height = height
            };
        }

        private void DrawMetaTile(
            byte[] buf ,
            int width ,
            int height ,
            int mapX ,
            int mapY ,
            MapTile mapTile ,
            int zoom)
        {
            int metaIndex = mapTile.MetaTileIndex;
            if ( metaIndex < 0 )
                return;

            int mw = _pnl.MetaWidth;
            int mh = _pnl.MetaHeight;

            int tilesAcross = PnlFile.PanelWidth / mw;

            // Number of meta‑tiles across the PNL panel
            int tilesX = PnlFile.PanelWidth / mw;

            // Convert MAP meta‑tile index → panel‑space meta‑tile origin
            int metaX = metaIndex % tilesX;
            int metaY = metaIndex / tilesX;

            int originPanelX = metaX * mw;
            int originPanelY = metaY * mh;


            // Debug the resolved panel-space origin
            Debug.WriteLine(
                $"  → MetaIndex {metaIndex} resolves to panel origin ({originPanelX},{originPanelY}) " +
                $"metaSize={mw}x{mh}, tilesAcross={tilesAcross}" );


            // Destination origin in output buffer
            int baseX = mapX * mw * TileSize * zoom;
            int baseY = mapY * mh * TileSize * zoom;

            // Loop through tiles inside the meta‑tile
            for ( int ty = 0 ; ty < mh ; ty++ )
            {
                for ( int tx = 0 ; tx < mw ; tx++ )
                {
                    int panelX = originPanelX + tx;
                    int panelY = originPanelY + ty;

                    var pnlTile = _pnl.GetTile(panelX, panelY);
                    if ( pnlTile == null || !pnlTile.Present )
                        continue;

                    DrawCgxTile(
                        buf ,
                        width ,
                        height ,
                        baseX + tx * TileSize * zoom ,
                        baseY + ty * TileSize * zoom ,
                        pnlTile ,
                        mapTile ,
                        zoom
                    );
                }
            }
        }

        private void DrawCgxTile(
            byte[] buf ,
            int width ,
            int height ,
            int px ,
            int py ,
            PnlTile pnlTile ,
            MapTile mapTile ,
            int zoom)
        {
            int tileId = pnlTile.TileId;
            if ( tileId < 0 || tileId >= _cgx.TileCount )
                return;

            var cgxTile = _cgx.Tiles[tileId];
            byte[,] pixels = cgxTile.Pixels;

            // Combine flips
            bool hflip = pnlTile.HFlip ^ mapTile.HFlip;
            bool vflip = pnlTile.VFlip ^ mapTile.VFlip;

            // Combine palette group (row)
            int paletteGroup =
                (mapTile.PaletteRowOverride != 0
                    ? mapTile.PaletteRowOverride
                    : pnlTile.PaletteRow) & 0x0F;

            Debug.WriteLine(
                $"    TileId={pnlTile.TileId}, H={hflip}, V={vflip}, " +
                $"PaletteRow={pnlTile.PaletteRow}, Override={mapTile.PaletteRowOverride}" );


            for ( int y = 0 ; y < TileSize ; y++ )
            {
                int sy = vflip ? (TileSize - 1 - y) : y;

                for ( int x = 0 ; x < TileSize ; x++ )
                {
                    int sx = hflip ? (TileSize - 1 - x) : x;

                    byte baseIndex = pixels[sy, sx];

                    // Transparent
                    if ( baseIndex == 0 )
                        continue;

                    // Compute palette index using CGX bit depth
                    int paletteIndex = _cgx.BitDepth switch
                    {
                        2 => (paletteGroup << 2) | (baseIndex & 0x03),
                        4 => (paletteGroup << 4) | (baseIndex & 0x0F),
                        8 => baseIndex,
                        _ => baseIndex
                    };

                    int row    = paletteIndex / 16;
                    int colIdx = paletteIndex % 16;

                    var col = _col.GetColor(row, colIdx);

                    int destX0 = px + x * zoom;
                    int destY0 = py + y * zoom;

                    for ( int zy = 0 ; zy < zoom ; zy++ )
                    {
                        int dy = destY0 + zy;
                        if ( dy < 0 || dy >= height ) continue;

                        int rowOffset = dy * width * 4;

                        for ( int zx = 0 ; zx < zoom ; zx++ )
                        {
                            int dx = destX0 + zx;
                            if ( dx < 0 || dx >= width ) continue;

                            int i = rowOffset + dx * 4;

                            buf[i + 0] = col.B;
                            buf[i + 1] = col.G;
                            buf[i + 2] = col.R;
                            buf[i + 3] = 255;
                        }
                    }
                }
            }
        }

    }
}
