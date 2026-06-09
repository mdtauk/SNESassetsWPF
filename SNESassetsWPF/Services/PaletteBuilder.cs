using SNESassetsWPF.Formats;
using SNESassetsWPF.Models;
using SNESassetsWPF.ViewModels;
using System.Collections.ObjectModel;
using System.Windows.Media;

namespace SNESassetsWPF.Services
{
    /// <summary>
    /// Builds palette rows and entries from a COL file.
    /// This version correctly respects the BG/OBJ toggle (metadata[0x22])
    /// so that the palette rows match H‑CG‑CAD’s editor behaviour.
    /// </summary>
    public class PaletteBuilder
    {
        /// <summary>
        /// Builds the full 16×16 palette grid from a COL file.
        /// Each row represents one SNES palette row (16 colours).
        /// </summary>
        public ObservableCollection<PaletteRowViewModel> Build(ColFile col)
        {
            var rows = new ObservableCollection<PaletteRowViewModel>();

            // Always show all 16 rows of the COL file.
            // CGX uses only 0–7, but the user must see the full palette.
            for ( int p = 0 ; p < 16 ; p++ )
            {
                var row = new PaletteRowViewModel();

                for ( int c = 0 ; c < 16 ; c++ )
                {
                    var snes = col.RawColors[p, c];
                    var rgb  = col.RgbColors[p, c];

                    row.Colors.Add( new PaletteEntry
                    {
                        SnesColor = snes ,
                        SnesColorString = snes.ToHexPair() ,
                        RgbColor = rgb ,
                        RGBColorString = $"#{rgb.R:X2}{rgb.G:X2}{rgb.B:X2}"
                    } );
                }

                rows.Add( row );
            }

            return rows;
        }


        /// <summary>
        /// Builds a single PaletteEntry from raw SNES BGR555 bytes.
        /// This helper is useful for palette editing or conversion tools.
        /// </summary>
        public PaletteEntry BuildEntry(byte low , byte high)
        {
            // Combine bytes into a 15‑bit SNES colour value.
            int value = low | (high << 8);

            // Extract 5‑bit channels (BGR555) and scale to 8‑bit.
            int r = (value & 0x1F) << 3;
            int g = ((value >> 5) & 0x1F) << 3;
            int b = ((value >> 10) & 0x1F) << 3;

            var rgb = Color.FromRgb((byte)r, (byte)g, (byte)b);
            var snes = new SnesColor(low, high);

            return new PaletteEntry
            {
                SnesColor = snes ,
                SnesColorString = snes.ToHexPair() ,
                RgbColor = rgb ,
                RGBColorString = $"#{rgb.R:X2}{rgb.G:X2}{rgb.B:X2}"
            };
        }
    }
}
