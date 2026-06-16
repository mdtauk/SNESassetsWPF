using SNESassetsWPF.Formats;
using SNESassetsWPF.Models;
using System;
using System.Collections.ObjectModel;
using System.Drawing;
using System.Drawing.Imaging;
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
        //  PNG EXPORT CELL SIZE
        // ─────────────────────────────────────────────────────────────
        //
        public int PaletteCellSize
        {
            get => Properties.Settings.Default.PaletteCellSize;
            set
            {
                Properties.Settings.Default.PaletteCellSize = value;
                Properties.Settings.Default.Save();
                OnPropertyChanged();
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




        //
        // ─────────────────────────────────────────────────────────────
        //  PALETTE EXPORT PNG
        // ─────────────────────────────────────────────────────────────
        //
        public void ExportPng(string path)
        {
            int cell = PaletteCellSize;
            int width = 16 * cell;
            int height = 16 * cell;

            using var bmp = new Bitmap(width, height, PixelFormat.Format32bppArgb);

            for ( int row = 0 ; row < 16 ; row++ )
            {
                if ( row >= PaletteRows.Count )
                    break;

                var paletteRow = PaletteRows[row];

                for ( int col = 0 ; col < 16 ; col++ )
                {
                    if ( col >= paletteRow.Colors.Count )
                        break;

                    PaletteEntry entry = paletteRow.Colors[col];

                    // Transparent if placeholder
                    System.Drawing.Color pixelColor;

                    if ( entry.IsPlaceholder )
                    {
                        pixelColor = System.Drawing.Color.FromArgb( 0 , 0 , 0 , 0 );
                    }
                    else
                    {
                        var c = entry.RgbColor;
                        pixelColor = System.Drawing.Color.FromArgb( 255 , c.R , c.G , c.B );
                    }

                    int px = col * cell;
                    int py = row * cell;

                    for ( int y = 0 ; y < cell ; y++ )
                    {
                        for ( int x = 0 ; x < cell ; x++ )
                        {
                            bmp.SetPixel( px + x , py + y , pixelColor );
                        }
                    }
                }
            }

            bmp.Save( path , ImageFormat.Png );
        }
    }
}
