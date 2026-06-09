using SNESassetsWPF.Models;
using SNESassetsWPF.ViewModels;
using System.Collections.ObjectModel;

namespace SNESassetsWPF.Services
{
    /// <summary>
    /// Central adapter that listens for palette changes and updates all renderers.
    /// This keeps PaletteViewModel decoupled from viewer VMs and avoids duplicated wiring.
    /// </summary>
    public class PaletteRendererAdapter
    {
        private readonly PaletteViewModel _palette;
        private readonly CgxViewerViewModel _cgx;
        private readonly ScrViewerViewModel _scr;
        private readonly MapPnlViewerViewModel _map;

        public PaletteRendererAdapter(
            PaletteViewModel palette ,
            CgxViewerViewModel cgx ,
            ScrViewerViewModel scr ,
            MapPnlViewerViewModel map)
        {
            _palette = palette;
            _cgx = cgx;
            _scr = scr;
            _map = map;

            // Subscribe once — all renderers update automatically
            _palette.PaletteChanged += OnPaletteChanged;

            // Initial sync (in case palette already loaded)
            OnPaletteChanged();
        }


        private void OnPaletteChanged()
        {
            ReadOnlyCollection<PaletteEntry> active = _palette.ActivePalette;

            // Trigger redraws
            _cgx.RenderCgx();
            _scr.RenderScr();
            _map.RenderMap();
        }
    }
}
