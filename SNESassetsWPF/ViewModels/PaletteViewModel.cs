using SNESassetsWPF.Formats;
using SNESassetsWPF.Models;
using System;
using System.Collections.ObjectModel;
using System.Linq;

namespace SNESassetsWPF.ViewModels
{
    public class PaletteViewModel : ViewModelBase
    {
        public ObservableCollection<PaletteRowViewModel> PaletteRows { get; } = new();


        public event Action PaletteChanged;



        // ---------------------------------------------------------
        // Selected Palette Row
        // ---------------------------------------------------------
        private int _selectedPaletteRowIndex = -1;
        public int SelectedPaletteRowIndex
        {
            get => _selectedPaletteRowIndex;
            set
            {
                if ( SetProperty( ref _selectedPaletteRowIndex , value ) )
                {
                    UpdateCgxPreview();
                    PaletteChanged?.Invoke();
                }
            }
        }

        // ---------------------------------------------------------
        // Force Single Row
        // ---------------------------------------------------------
        private bool _forceSingleRow;
        public bool ForceSingleRow
        {
            get => _forceSingleRow;
            set
            {
                if ( SetProperty( ref _forceSingleRow , value ) )
                {
                    // If user unchecks → no row selected
                    if ( !value )
                        SelectedPaletteRowIndex = -1;

                    UpdateCgxPreview();
                    PaletteChanged?.Invoke();
                }
            }
        }

        private bool _forceSingleRowEnabled;
        public bool ForceSingleRowEnabled
        {
            get => _forceSingleRowEnabled;
            set => SetProperty( ref _forceSingleRowEnabled , value );
        }

        // ---------------------------------------------------------
        // Load Palette
        // ---------------------------------------------------------
        public void LoadPalette(ColFile col)
        {
            PaletteRows.Clear();

            for ( int p = 0 ; p < 16 ; p++ )
            {
                var row = new PaletteRowViewModel();

                for ( int c = 0 ; c < 16 ; c++ )
                {
                    var snes = col.RawColors[p, c];
                    var rgb = col.RgbColors[p, c];

                    row.Colors.Add( new PaletteEntry
                    {
                        SnesColor = snes ,
                        SnesColorString = snes.ToHexPair() ,
                        RgbColor = rgb ,
                        RGBColorString = $"#{rgb.R:X2}{rgb.G:X2}{rgb.B:X2}"
                    } );
                }

                PaletteRows.Add( row );
            }

            // If not forcing a row → no selection
            if ( !ForceSingleRow )
                SelectedPaletteRowIndex = -1;
        }

        // ---------------------------------------------------------
        // Apply BPP Rules
        // ---------------------------------------------------------
        public void ApplyBitDepthRules(int bitDepth)
        {
            switch ( bitDepth )
            {
                case 2:
                    // 2bpp: always force single row
                    ForceSingleRow = true;
                    ForceSingleRowEnabled = false;

                    if ( SelectedPaletteRowIndex < 0 || SelectedPaletteRowIndex > 15 )
                        SelectedPaletteRowIndex = 0;
                    break;

                case 4:
                    // 4bpp: user can choose
                    ForceSingleRowEnabled = true;

                    if ( !ForceSingleRow )
                        SelectedPaletteRowIndex = -1;
                    break;

                case 8:
                    // 8bpp: cannot force row
                    ForceSingleRow = false;
                    ForceSingleRowEnabled = false;
                    SelectedPaletteRowIndex = -1;
                    break;
            }
        }

        // ---------------------------------------------------------
        // Update CGX Preview
        // ---------------------------------------------------------
        private void UpdateCgxPreview()
        {
            // Prevent crashes when palette is not loaded yet
            if ( PaletteRows == null || PaletteRows.Count == 0 )
                return;

            if ( ForceSingleRow )
            {
                // Ensure valid row
                SelectedPaletteRowIndex = Math.Clamp(
                    SelectedPaletteRowIndex ,
                    0 ,
                    PaletteRows.Count - 1
                );

                var activePalette = PaletteRows[SelectedPaletteRowIndex].Colors;

                // TODO: Pass activePalette to renderer (via event or callback)
            }
            else
            {
                // No row selected
                SelectedPaletteRowIndex = -1;

                var activePalette = PaletteRows.SelectMany(r => r.Colors);

                // TODO: Pass activePalette to renderer
            }
        }
    }
}
