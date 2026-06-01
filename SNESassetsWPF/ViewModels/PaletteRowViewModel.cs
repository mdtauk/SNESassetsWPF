using SNESassetsWPF.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;

namespace SNESassetsWPF.ViewModels
{
    public class PaletteRowViewModel
    {
        public ObservableCollection<PaletteEntry> Colors { get; } = new();
    }

}
