# MVVM Refactoring Guide - SNESassetsWPF

This document provides step-by-step refactoring examples to improve MVVM compliance in the project.

---

## 1. Standardize All ViewModels to Use ViewModelBase

### Current Problem (CgxViewerViewModel)

```csharp
// ❌ Duplicate code - not inheriting ViewModelBase
public class CgxViewerViewModel : INotifyPropertyChanged
{
    private CgxFile _cgxFile;

    public CgxFile CgxFile
    {
        get => _cgxFile;
        set
        {
            if (_cgxFile != value)
            {
                _cgxFile = value;
                OnPropertyChanged();
            }
        }
    }

    public event PropertyChangedEventHandler PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
```

### Refactored

```csharp
// ✅ Clean - inherits ViewModelBase
public class CgxViewerViewModel : ViewModelBase
{
    private CgxFile _cgxFile;

    public CgxFile CgxFile
    {
        get => _cgxFile;
        set => SetProperty(ref _cgxFile, value);  // No need for change detection
    }

    public bool HasCgx => CgxFile != null;
    public bool HasCol => ColFile != null;
}
```

**Changes Applied:**
- Inherit `ViewModelBase` instead of implementing `INotifyPropertyChanged` directly
- Use `SetProperty<T>()` helper method
- Remove duplicate `OnPropertyChanged()` implementation
- Leverage existing infrastructure

---

## 2. Create Attached Behavior for TreeView Selection

### Current Problem (Code-Behind Event)

```csharp
// MainWindow.xaml.cs
private void colTreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
{
    if (e.NewValue is FileNode fileNode)
    {
        var vm = (MainViewModel)DataContext;
        vm.LoadColCommand.Execute(fileNode);
    }
}
```

### Solution: Attached Behavior

**New File:** `SNESassetsWPF\Helpers\TreeViewSelectionBehavior.cs`

```csharp
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace SNESassetsWPF.Helpers
{
    /// <summary>
    /// Attached behavior for binding TreeView SelectedItem changes to a command.
    /// </summary>
    public static class TreeViewSelectionBehavior
    {
        public static ICommand GetSelectionChangedCommand(DependencyObject obj)
        {
            return (ICommand)obj.GetValue(SelectionChangedCommandProperty);
        }

        public static void SetSelectionChangedCommand(DependencyObject obj, ICommand value)
        {
            obj.SetValue(SelectionChangedCommandProperty, value);
        }

        public static readonly DependencyProperty SelectionChangedCommandProperty =
            DependencyProperty.RegisterAttached(
                "SelectionChangedCommand",
                typeof(ICommand),
                typeof(TreeViewSelectionBehavior),
                new PropertyMetadata(null, OnSelectionChangedCommandChanged));

        private static void OnSelectionChangedCommandChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is TreeView treeView)
            {
                treeView.SelectedItemChanged -= TreeView_SelectedItemChanged;

                if (e.NewValue != null)
                {
                    treeView.SelectedItemChanged += TreeView_SelectedItemChanged;
                }
            }
        }

        private static void TreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (sender is TreeView treeView)
            {
                ICommand command = GetSelectionChangedCommand(treeView);
                if (command?.CanExecute(e.NewValue) == true)
                {
                    command.Execute(e.NewValue);
                }
            }
        }
    }
}
```

### Updated XAML

```xaml
<!-- MainWindow.xaml -->
<TreeView x:Name="colTreeView"
          ItemsSource="{Binding ColTree.Items}"
          helpers:TreeViewSelectionBehavior.SelectionChangedCommand="{Binding LoadColCommand}">
</TreeView>
```

### Updated ViewModel

```csharp
// MainViewModel.cs
public class MainViewModel : ViewModelBase
{
    public RelayCommand<FileNode> LoadColCommand { get; private set; }

    public MainViewModel()
    {
        // Command now accepts the selected item directly
        LoadColCommand = new RelayCommand<FileNode>(LoadCol, col => col != null);
    }

    private void LoadCol(FileNode fileNode)
    {
        // Load logic here
    }
}
```

### Removed Code-Behind

```csharp
// ✅ REMOVE from MainWindow.xaml.cs
private void colTreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
{
    // This is now handled by the Attached Behavior
}
```

**Benefits:**
- No code-behind needed
- Reusable for any TreeView
- Fully testable
- MVVM compliant

---

## 3. Move Tree State Management to ViewModel

### Current Problem (Code-Behind + TreeViewController)

```csharp
// MainWindow.xaml.cs
private TreeViewController _colTree;
private TreeViewController _cgxTree;

public MainWindow()
{
    _colTree = new TreeViewController(colTreeView);
    _cgxTree = new TreeViewController(cgxTreeView);

    colTreeView.ItemContainerGenerator.StatusChanged += OnTreeContainersGenerated;
    cgxTreeView.ItemContainerGenerator.StatusChanged += OnTreeContainersGenerated;
}

private void ReloadAllTrees()
{
    _colTree.SaveState();
    _cgxTree.SaveState();

    if (!string.IsNullOrEmpty(ColViewModel.CurrentFolder))
        ColViewModel.LoadFolder(ColViewModel.CurrentFolder);

    if (!string.IsNullOrEmpty(CgxViewModel.CurrentFolder))
        CgxViewModel.LoadFolder(CgxViewModel.CurrentFolder);

    _colTree.RestoreState();
    _cgxTree.RestoreState();
}
```

### Solution: TreeStateManager ViewModel

**New File:** `SNESassetsWPF\ViewModels\TreeStateManager.cs`

```csharp
using System;
using System.Collections.Generic;

namespace SNESassetsWPF.ViewModels
{
    public class TreeStateManager : ViewModelBase
    {
        private HashSet<string> _expandedPaths = new();
        private string _selectedPath;

        public void SaveState(ITreeNode root)
        {
            _expandedPaths.Clear();
            CollectExpandedNodes(root);
        }

        public void RestoreState(ITreeNode root)
        {
            ExpandNodes(root, _expandedPaths);
            SelectNode(root, _selectedPath);
        }

        private void CollectExpandedNodes(ITreeNode node)
        {
            if (node.IsExpanded)
            {
                _expandedPaths.Add(node.FullPath);
                foreach (var child in node.Children)
                {
                    CollectExpandedNodes(child);
                }
            }
        }

        private void ExpandNodes(ITreeNode node, HashSet<string> paths)
        {
            if (paths.Contains(node.FullPath))
            {
                node.IsExpanded = true;
            }

            foreach (var child in node.Children)
            {
                ExpandNodes(child, paths);
            }
        }

        private void SelectNode(ITreeNode node, string path)
        {
            if (node.FullPath == path)
            {
                node.IsSelected = true;
            }

            foreach (var child in node.Children)
            {
                SelectNode(child, path);
            }
        }
    }
}
```

### Updated MainViewModel

```csharp
public class MainViewModel : ViewModelBase
{
    private TreeStateManager _colTreeState = new TreeStateManager();
    private TreeStateManager _cgxTreeState = new TreeStateManager();

    private DispatcherTimer _accentWatcher;

    public MainViewModel()
    {
        // ... existing initialization ...

        InitializeAccentWatcher();
    }

    private void InitializeAccentWatcher()
    {
        _accentWatcher = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(300)
        };

        _accentWatcher.Tick += (s, e) =>
        {
            var current = AccentHelper.GetAccentColor();
            if (current != _lastAccent)
            {
                _lastAccent = current;
                RefreshTrees();
            }
        };

        _accentWatcher.Start();
    }

    public void RefreshTrees()
    {
        _colTreeState.SaveState(ColTree.RootNode);
        _cgxTreeState.SaveState(CgxTree.RootNode);

        // Reload trees
        ColTree.LoadFolder(ColTree.CurrentFolder);
        CgxTree.LoadFolder(CgxTree.CurrentFolder);

        _colTreeState.RestoreState(ColTree.RootNode);
        _cgxTreeState.RestoreState(CgxTree.RootNode);
    }
}
```

### Updated Code-Behind

```csharp
// MainWindow.xaml.cs - Minimal
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        // DataContext binding in XAML
    }

    // No tree management code!
}
```

**Benefits:**
- All logic in ViewModel (testable)
- No code-behind complexity
- Reusable TreeStateManager

---

## 4. Implement Dependency Injection

### Step 1: Add NuGet Package

```bash
dotnet add package Microsoft.Extensions.DependencyInjection
```

### Step 2: Create App-Level Service Configuration

**File:** `SNESassetsWPF\App.xaml.cs`

```csharp
using Microsoft.Extensions.DependencyInjection;
using System.Windows;

namespace SNESassetsWPF
{
    public partial class App : Application
    {
        public static IServiceProvider ServiceProvider { get; private set; }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            var services = new ServiceCollection();

            // Register services
            services.AddSingleton<IAssetScannerService, AssetScannerService>();
            services.AddSingleton<IColFileParser, ColFileParser>();
            services.AddSingleton<ICgxFileParser, CgxFileParser>();

            // Register ViewModels
            services.AddSingleton<MainViewModel>();
            services.AddSingleton<ColTreeViewModel>();
            services.AddSingleton<CgxTreeViewModel>();
            services.AddSingleton<CgxViewerViewModel>();
            services.AddSingleton<PaletteViewModel>();

            ServiceProvider = services.BuildServiceProvider();

            // Show main window
            MainWindow mainWindow = new MainWindow();
            mainWindow.DataContext = ServiceProvider.GetRequiredService<MainViewModel>();
            mainWindow.Show();
        }
    }
}
```

### Step 3: Create Service Interfaces

**File:** `SNESassetsWPF\Services\IAssetScannerService.cs`

```csharp
public interface IAssetScannerService
{
    List<FolderNode> ScanFolder(string folderPath);
    event EventHandler<AssetScannedEventArgs> AssetScanned;
}

public class AssetScannerService : IAssetScannerService
{
    // Existing implementation
}
```

### Step 4: Inject Services into ViewModel

```csharp
// MainViewModel.cs
public class MainViewModel : ViewModelBase
{
    private readonly IAssetScannerService _scannerService;
    private readonly IColFileParser _colParser;

    public MainViewModel(
        IAssetScannerService scannerService,
        IColFileParser colParser)
    {
        _scannerService = scannerService;
        _colParser = colParser;

        // Use injected services
        // ...
    }
}
```

**Benefits:**
- Loose coupling between components
- Easy to mock for unit testing
- Centralized object graph management
- Better testability

---

## 5. Create HexWindowViewModel

### Current Problem (Code-Behind Button Handler)

```csharp
// HexWindow.xaml.cs
private void btnCopyHex_Click(object sender, RoutedEventArgs e)
{
    string raw = txtHex.Text.Replace(" ", "").Replace("\r", "").Replace("\n", "");
    Clipboard.SetText(raw);
}
```

### Solution: ViewModel with Command

**New File:** `SNESassetsWPF\ViewModels\HexWindowViewModel.cs`

```csharp
using System.Windows;
using System.Windows.Input;

namespace SNESassetsWPF.ViewModels
{
    public class HexWindowViewModel : ViewModelBase
    {
        private string _hexText;
        private string _headerText;

        public string HexText
        {
            get => _hexText;
            set => SetProperty(ref _hexText, value);
        }

        public string HeaderText
        {
            get => _headerText;
            set => SetProperty(ref _headerText, value);
        }

        public ICommand CopyHexCommand { get; }

        public HexWindowViewModel(string hexText, string headerText)
        {
            HexText = hexText;
            HeaderText = headerText;

            CopyHexCommand = new RelayCommand(CopyHexToClipboard, () => !string.IsNullOrEmpty(HexText));
        }

        private void CopyHexToClipboard()
        {
            string raw = HexText
                .Replace(" ", "")
                .Replace("\r", "")
                .Replace("\n", "");

            Clipboard.SetText(raw);
        }
    }
}
```

### Updated HexWindow

```csharp
// HexWindow.xaml.cs - Minimal
public partial class HexWindow : Window
{
    public HexWindow(string hexText, string headerText)
    {
        InitializeComponent();
        DataContext = new HexWindowViewModel(hexText, headerText);
    }
}
```

### Updated HexWindow.xaml

```xaml
<Window ...>
    <StackPanel>
        <TextBox x:Name="txtHex" Text="{Binding HexText}" />
        <TextBox x:Name="txtHeader" Text="{Binding HeaderText}" />
        <Button Content="Copy Hex" Command="{Binding CopyHexCommand}" />
    </StackPanel>
</Window>
```

**Benefits:**
- No code-behind event handlers
- Command is testable
- Reusable ViewModel

---

## 6. Create Design-Time ViewModel Support

**File:** `SNESassetsWPF\ViewModels\ViewModelLocator.cs`

```csharp
using System;

namespace SNESassetsWPF.ViewModels
{
    public class ViewModelLocator
    {
        private static ViewModelLocator _instance;

        public static ViewModelLocator Instance => 
            _instance ??= new ViewModelLocator();

        public MainViewModel MainViewModel { get; }
        public PaletteViewModel PaletteViewModel { get; }

        public ViewModelLocator()
        {
            // In design mode, create minimal instances
            if (System.ComponentModel.DesignerProperties.GetIsInDesignMode(
                new System.Windows.DependencyObject()))
            {
                MainViewModel = new MainViewModel();
                PaletteViewModel = new PaletteViewModel();
            }
            else
            {
                // In runtime, use dependency injection (from App.xaml.cs)
                MainViewModel = App.ServiceProvider.GetRequiredService<MainViewModel>();
                PaletteViewModel = App.ServiceProvider.GetRequiredService<PaletteViewModel>();
            }
        }
    }
}
```

**Usage in XAML:**

```xaml
<Window.DataContext>
    <Binding Path="MainViewModel" Source="{x:Static vm:ViewModelLocator.Instance}" />
</Window.DataContext>
```

---

## 7. Unit Test Examples

**File:** `SNESassetsWPFTests\ViewModels\MainViewModelTests.cs`

```csharp
using Xunit;
using Moq;

namespace SNESassetsWPFTests
{
    public class MainViewModelTests
    {
        [Fact]
        public void LoadCol_WithValidFileNode_ShouldLoadPalette()
        {
            // Arrange
            var mockScanner = new Mock<IAssetScannerService>();
            var mockParser = new Mock<IColFileParser>();

            var vm = new MainViewModel(mockScanner.Object, mockParser.Object);
            var fileNode = new FileNode { Path = "test.col" };

            // Act
            vm.LoadColCommand.Execute(fileNode);

            // Assert
            Assert.NotNull(vm.Palette);
            mockParser.Verify(p => p.ParseCol("test.col"), Times.Once);
        }

        [Fact]
        public void RefreshTrees_WithAccentChange_ShouldReloadAndRestoreState()
        {
            // Arrange
            var vm = new MainViewModel();

            // Act
            vm.RefreshTrees();

            // Assert
            Assert.NotNull(vm.ColTree.Items);
            Assert.NotNull(vm.CgxTree.Items);
        }
    }
}
```

**Benefits:**
- ViewModel logic is isolated and testable
- No UI dependencies required
- Easy to mock services

---

## Migration Checklist

- [ ] Create TreeViewSelectionBehavior.cs
- [ ] Update all TreeView bindings in XAML
- [ ] Remove TreeView event handlers from MainWindow.xaml.cs
- [ ] Make CgxViewerViewModel inherit ViewModelBase
- [ ] Add DI container to App.xaml.cs
- [ ] Create service interfaces (IAssetScannerService, IColFileParser, ICgxFileParser)
- [ ] Create HexWindowViewModel
- [ ] Remove HexWindow.xaml.cs event handlers
- [ ] Create ViewModelLocator for design-time support
- [ ] Create unit tests for ViewModel logic
- [ ] Update all service registrations to use interfaces
- [ ] Minimize MainWindow.xaml.cs code-behind

---

## Testing the Refactoring

After each refactoring:

1. **Build the project** - Ensure no compilation errors
2. **Run the application** - Verify UI behavior unchanged
3. **Check bindings** - Output window in Visual Studio for binding errors
4. **Unit test logic** - Create tests for new ViewModel commands

---

## Further Reading

- [Microsoft MVVM Documentation](https://docs.microsoft.com/en-us/dotnet/architecture/maui/mvvm)
- [WPF Best Practices](https://www.wpftutorial.net/)
- [Dependency Injection in .NET](https://docs.microsoft.com/en-us/dotnet/core/extensions/dependency-injection)
- [Attached Behaviors in WPF](https://www.wpf-tutorial.com/attached-behaviors/)

