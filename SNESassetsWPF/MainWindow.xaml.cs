using SNESassetsWPF.Enums;
using SNESassetsWPF.Formats;
using SNESassetsWPF.Helpers;
using SNESassetsWPF.Models;
using SNESassetsWPF.UserControls;
using SNESassetsWPF.ViewModels;
using System.Diagnostics;
using System.Text;
using System.Threading.Channels;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;




namespace SNESassetsWPF
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private byte[] _lastLoadedPaletteBytes;

        private Color _lastAccent;
        private AccentHelper accentHelp = new AccentHelper();

        private TreeViewController _colTree;
        private TreeViewController _cgxTree;
        private TreeViewController _scrTree;

        private List<string> _savedColExpanded;
        private string _savedColSelected;


        public MainViewModel ViewModel { get; }
        public ColTreeViewModel ColViewModel { get; }
        public CgxTreeViewModel CgxViewModel { get; }
        public ScrTreeViewModel ScrViewModel { get; }

        public CgxViewerViewModel CgxViewerViewModel { get; }


        public MainWindow()
        {
            InitializeComponent();

            _colTree = new TreeViewController( colTreeView );
            _cgxTree = new TreeViewController( cgxTreeView );
            _scrTree = new TreeViewController( scrTreeView );

            ViewModel = new MainViewModel();
            DataContext = ViewModel;

            StartAccentWatcher();
            colTreeView.ItemContainerGenerator.StatusChanged += OnTreeContainersGenerated;
            cgxTreeView.ItemContainerGenerator.StatusChanged += OnTreeContainersGenerated;
            scrTreeView.ItemContainerGenerator.StatusChanged += OnTreeContainersGenerated;
        }




        private void listPalette_SelectionChanged(object sender , SelectionChangedEventArgs e)
        {
        }




        /// <summary>
        /// Triggered when the user selects an item in the COL TreeView.
        /// If the item is a COL file, read and display its raw hex data.
        /// </summary>
        private void colTreeView_SelectedItemChanged(object sender , RoutedPropertyChangedEventArgs<object> e)
        {
            if ( e.NewValue is FileNode fileNode )
            {
                var vm = (MainViewModel)DataContext;
                vm.LoadColCommand.Execute( fileNode );
            }
        }




        /// <summary>
        /// Triggered when the user selects an item in the CGX TreeView.
        /// If the item is a CGX file, read and display its raw hex data.
        /// </summary>
        private void cgxTreeView_SelectedItemChanged(object sender , RoutedPropertyChangedEventArgs<object> e)
        {
            if ( e.NewValue is FileNode fileNode )
            {
                var vm = (MainViewModel)DataContext;
                vm.LoadCgxCommand.Execute( fileNode );
            }
        }




        /// <summary>
        /// Triggered when the user selects an item in the CGX TreeView.
        /// If the item is a SCR file, read and display its raw hex data.
        /// </summary>
        private void scrTreeView_SelectedItemChanged(object sender , RoutedPropertyChangedEventArgs<object> e)
        {
            if ( e.NewValue is FileNode fileNode )
            {
                var vm = (MainViewModel)DataContext;
                vm.LoadScrCommand.Execute( fileNode );
            }
        }




        private void ReloadAllTrees()
        {
            // 1. Save state for each tree
            _colTree.SaveState();
            _cgxTree.SaveState();
            _scrTree.SaveState();

            // 2. Rebuild each tree using its own CurrentFolder
            if ( !string.IsNullOrEmpty( ColViewModel.CurrentFolder ) )
                ColViewModel.LoadFolder( ColViewModel.CurrentFolder );

            if ( !string.IsNullOrEmpty( CgxViewModel.CurrentFolder ) )
                CgxViewModel.LoadFolder( CgxViewModel.CurrentFolder );

            // 3. Restore state for each tree
            _colTree.RestoreState();
            _cgxTree.RestoreState();
            _scrTree.RestoreState();
        }



        private void StartAccentWatcher()
        {
            // Try to read the accent color from Fluent resources
            _lastAccent = accentHelp.GetAccentColor();

            var timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(300)
            };

            timer.Tick += (s , e) =>
            {
                var current = accentHelp.GetAccentColor();
                if ( current != _lastAccent )
                {
                    _lastAccent = current;
                    ReloadAllTrees();
                }
            };

            timer.Start();
        }




        private void OnTreeContainersGenerated(object sender , EventArgs e)
        {
            var generator = sender as ItemContainerGenerator;
            if ( generator.Status == GeneratorStatus.ContainersGenerated )
            {
                _colTree.State.TryPendingExpandRestore( _colTree.SavedExpanded );
                _colTree.State.TryPendingRestore( _colTree.SavedSelected );
                _scrTree.State.TryPendingRestore( _scrTree.SavedSelected );

                _cgxTree.State.TryPendingExpandRestore( _cgxTree.SavedExpanded );
                _cgxTree.State.TryPendingRestore( _cgxTree.SavedSelected );
                _scrTree.State.TryPendingRestore( _scrTree.SavedSelected );
            }
        }




        private void PaletteRow_PreviewMouseDown(object sender , MouseButtonEventArgs e)
        {
            var item = (ListBoxItem)sender;

            // Get the viewmodel
            var palette = ((MainViewModel)DataContext).CgxViewer.Palette;

            // If row selection is NOT allowed → block selection
            if ( !palette.ForceSingleRow )
            {
                e.Handled = true; // stops ListBox from selecting the row
            }
        }



        private void menuExportCGXtoPNG_Click(object sender , RoutedEventArgs e)
        {
            var vm = (MainViewModel)DataContext;
            var viewer = vm.CgxViewer;

            if ( viewer == null )
            {
                System.Windows.Forms.MessageBox.Show( "Viewer not initialized." );
                return;
            }

            // These are the files the viewer actually loaded
            string cgxName = System.IO.Path.GetFileNameWithoutExtension(vm.LoadedCgxPath) ?? "unknown_CGX";
            string colName = System.IO.Path.GetFileNameWithoutExtension(vm.LoadedColPath) ?? "unknown_COL";

            string exportName = $"COL_{colName}_CGX_{cgxName}_{viewer.ZoomLevel}x.png";

            var dlg = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "PNG Image|*.png",
                FileName = exportName
            };

            if ( dlg.ShowDialog() != true )
                return;

            // Call the viewer’s export method (it already knows the loaded CGX/COL)
            viewer.ExportPng( dlg.FileName , viewer.ZoomLevel );
        }



    }
}