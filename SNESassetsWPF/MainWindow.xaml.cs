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
        private TreeViewController _pnlTree;
        private TreeViewController _mapTree;

        private List<string> _savedColExpanded;
        private string _savedColSelected;


        public MainViewModel ViewModel { get; }
        public ColTreeViewModel ColViewModel { get; }
        public CgxTreeViewModel CgxViewModel { get; }
        public ScrTreeViewModel ScrViewModel { get; }
        public ScrTreeViewModel PnlViewModel { get; }
        public ScrTreeViewModel MapViewModel { get; }



        public MainWindow()
        {
            InitializeComponent();

            _colTree = new TreeViewController( colTreeView );
            _cgxTree = new TreeViewController( cgxTreeView );
            _scrTree = new TreeViewController( scrTreeView );
            _pnlTree = new TreeViewController( pnlTreeView );
            _mapTree = new TreeViewController( mapTreeView );

            ViewModel = new MainViewModel();
            DataContext = ViewModel;

            StartAccentWatcher();
            colTreeView.ItemContainerGenerator.StatusChanged += OnTreeContainersGenerated;
            cgxTreeView.ItemContainerGenerator.StatusChanged += OnTreeContainersGenerated;
            scrTreeView.ItemContainerGenerator.StatusChanged += OnTreeContainersGenerated;
            pnlTreeView.ItemContainerGenerator.StatusChanged += OnTreeContainersGenerated;
            mapTreeView.ItemContainerGenerator.StatusChanged += OnTreeContainersGenerated;
        }




        private void listPalette_SelectionChanged(object sender , SelectionChangedEventArgs e)
        {
        }





        /// <summary>
        /// Triggered when the user selects an item in the COL TreeView.
        /// If the item is a COL file, read and optionally display its raw hex data.
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
        /// If the item is a CGX file.
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
        /// Triggered when the user selects an item in the SCR TreeView.
        /// If the item is a SCR file.
        /// </summary>
        private void scrTreeView_SelectedItemChanged(object sender , RoutedPropertyChangedEventArgs<object> e)
        {
            if ( e.NewValue is FileNode fileNode )
            {
                var vm = (MainViewModel)DataContext;
                vm.LoadScrCommand.Execute( fileNode );
            }
        }




        /// <summary>
        /// Triggered when the user selects an item in the PNL TreeView.
        /// If the item is a PNL file.
        /// </summary>
        private void pnlTreeView_SelectedItemChanged(object sender , RoutedPropertyChangedEventArgs<object> e)
        {
            if ( e.NewValue is FileNode fileNode )
            {
                var vm = (MainViewModel)DataContext;
                vm.LoadPnlCommand.Execute( fileNode );
            }
        }




        /// <summary>
        /// Triggered when the user selects an item in the MAP TreeView.
        /// If the item is a MAP file.
        /// </summary>
        private void mapTreeView_SelectedItemChanged(object sender , RoutedPropertyChangedEventArgs<object> e)
        {
            if ( e.NewValue is FileNode fileNode )
            {
                var vm = (MainViewModel)DataContext;
                vm.LoadMapCommand.Execute( fileNode );
            }
        }




        private void ReloadAllTrees()
        {
            // 1. Save state for each tree
            _colTree.SaveState();
            _cgxTree.SaveState();
            _scrTree.SaveState();
            _pnlTree.SaveState();
            _mapTree.SaveState();


            // 2. Rebuild each tree using its own CurrentFolder
            if ( !string.IsNullOrEmpty( ColViewModel.CurrentFolder ) )
                ColViewModel.LoadFolder( ColViewModel.CurrentFolder );

            if ( !string.IsNullOrEmpty( CgxViewModel.CurrentFolder ) )
                CgxViewModel.LoadFolder( CgxViewModel.CurrentFolder );


            // 3. Restore state for each tree
            _colTree.RestoreState();
            _cgxTree.RestoreState();
            _scrTree.RestoreState();
            _pnlTree.RestoreState();
            _mapTree.RestoreState();
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
                _pnlTree.State.TryPendingRestore( _pnlTree.SavedSelected );

                _cgxTree.State.TryPendingExpandRestore( _cgxTree.SavedExpanded );
                _cgxTree.State.TryPendingRestore( _cgxTree.SavedSelected );
                _scrTree.State.TryPendingRestore( _scrTree.SavedSelected );
                _pnlTree.State.TryPendingRestore( _pnlTree.SavedSelected );
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
    }
}