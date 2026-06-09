using SNESassetsWPF.Formats;
using SNESassetsWPF.Models;
using System;
using System.Collections.ObjectModel;
using System.Linq;

namespace SNESassetsWPF.ViewModels
{
    public class PaletteViewModel : ViewModelBase
    {
        //
        // ─────────────────────────────────────────────────────────────
        //  PALETTE ROWS (UI DATA)
        // ─────────────────────────────────────────────────────────────
        //
        public ObservableCollection<PaletteRowViewModel> PaletteRows { get; }
            = new ObservableCollection<PaletteRowViewModel>();



        //
        // ─────────────────────────────────────────────────────────────
        //  FIELDS
        // ─────────────────────────────────────────────────────────────
        //
        private bool _suppressEvents;



        //
        // ─────────────────────────────────────────────────────────────
        //  EVENTS
        // ─────────────────────────────────────────────────────────────
        //
        public event Action PaletteChanged;


        //
        // ─────────────────────────────────────────────────────────────
        //  SELECTED PALETTE ROW
        // ─────────────────────────────────────────────────────────────
        //
        private int _selectedPaletteRowIndex = -1;
        public int SelectedPaletteRowIndex
        {
            get => _selectedPaletteRowIndex;
            set
            {
                if ( SetProperty( ref _selectedPaletteRowIndex , value ) )
                {
                    UpdateActivePalette();
                    PaletteChanged?.Invoke();
                }
            }
        }


        //
        // ─────────────────────────────────────────────────────────────
        //  FORCE SINGLE ROW
        // ─────────────────────────────────────────────────────────────
        //
        private bool _forceSingleRow;
        public bool ForceSingleRow
        {
            get => _forceSingleRow;
            set
            {
                if ( SetProperty( ref _forceSingleRow , value ) )
                {
                    if ( !value )
                        SelectedPaletteRowIndex = -1;

                    UpdateActivePalette();
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


        //
        // ─────────────────────────────────────────────────────────────
        //  LOAD PALETTE (FROM PaletteBuilder)
        // ─────────────────────────────────────────────────────────────
        //
        public void SetPaletteRows(ObservableCollection<PaletteRowViewModel> rows)
        {
            PaletteRows.Clear();

            foreach ( var row in rows )
                PaletteRows.Add( row );

            if ( !ForceSingleRow )
                SelectedPaletteRowIndex = -1;

            UpdateActivePalette();
            PaletteChanged?.Invoke();
        }


        //
        // ─────────────────────────────────────────────────────────────
        //  BIT DEPTH RULES
        // ─────────────────────────────────────────────────────────────
        //
        public void ApplyBitDepthRules(int bitDepth)
        {
            switch ( bitDepth )
            {
                case 2:
                    ForceSingleRow = true;
                    ForceSingleRowEnabled = false;

                    if ( SelectedPaletteRowIndex < 0 || SelectedPaletteRowIndex > 15 )
                        SelectedPaletteRowIndex = 0;
                    break;

                case 4:
                    ForceSingleRowEnabled = true;

                    if ( !ForceSingleRow )
                        SelectedPaletteRowIndex = -1;
                    break;

                case 8:
                    ForceSingleRow = false;
                    ForceSingleRowEnabled = false;
                    SelectedPaletteRowIndex = -1;
                    break;
            }

            UpdateActivePalette();
            PaletteChanged?.Invoke();
        }


        //
        // ─────────────────────────────────────────────────────────────
        //  ACTIVE PALETTE (FOR RENDERERS)
        // ─────────────────────────────────────────────────────────────
        //
        public ReadOnlyCollection<PaletteEntry> ActivePalette { get; private set; }


        private void UpdateActivePalette()
        {
            if ( PaletteRows.Count == 0 )
            {
                ActivePalette = Array.Empty<PaletteEntry>().ToList().AsReadOnly();
                return;
            }

            if ( ForceSingleRow )
            {
                SelectedPaletteRowIndex = Math.Clamp(
                    SelectedPaletteRowIndex ,
                    0 ,
                    PaletteRows.Count - 1
                );

                ActivePalette = PaletteRows[SelectedPaletteRowIndex]
                    .Colors
                    .ToList()
                    .AsReadOnly();
            }
            else
            {
                SelectedPaletteRowIndex = -1;

                ActivePalette = PaletteRows
                    .SelectMany( r => r.Colors )
                    .ToList()
                    .AsReadOnly();
            }
        }
    }
}
