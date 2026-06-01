# MVVM Best Practices Analysis - SNESassetsWPF Project

## Executive Summary

**Overall MVVM Compliance: ~70%**

The project demonstrates a solid foundation in MVVM architecture but has several areas where best practices are not fully implemented. The application has proper separation of concerns in many areas, but inconsistencies exist around View code-behind logic, ViewModel instantiation, and UI command handling.

---

## Strengths ✅

### 1. **ViewModelBase with INotifyPropertyChanged**
- ✅ Proper base class implementing `INotifyPropertyChanged`
- ✅ Uses `OnPropertyChanged()` for property notifications
- ✅ Implements `SetProperty<T>()` helper for cleaner property declarations
- ✅ Uses `CallerMemberName` attribute to avoid magic strings

```csharp
// Good pattern
public bool HasCgx => CgxFile != null;
```

### 2. **Separation of Models, ViewModels, and Services**
- ✅ Clear folder structure (Models/, ViewModels/, Services/, Formats/)
- ✅ Models are pure POCOs (PaletteEntry, SnesColor, FileNode)
- ✅ Services layer exists (AssetScannerService, CgxFileParser, ColFileParser)
- ✅ Formats layer abstracts file I/O

### 3. **RelayCommand Implementation**
- ✅ Custom RelayCommand for delegating command execution to ViewModel
- ✅ Supports predicate (CanExecute) logic
- ✅ Properly typed for generic parameter support

### 4. **Observable Collections**
- ✅ Uses `ObservableCollection<T>` for dynamic UI updates
- ✅ PaletteRows collection properly notifies on changes
- ✅ Tree structures use proper collection binding

### 5. **Data Binding (Mostly Correct)**
- ✅ XAML binds to ViewModel properties
- ✅ Command bindings use RelayCommand
- ✅ Uses DependencyProperty for UserControl state (Swatch)

---

## Issues & Violations ❌

### 1. **Excessive Code-Behind Logic in Views** (HIGH PRIORITY)

**Location:** `MainWindow.xaml.cs` (219 lines)

**Problems:**
- Direct file I/O operations: `File.ReadAllBytes()`
- Direct UI manipulation via TreeViewController
- Business logic mixed with view presentation (ReloadAllTrees, AccentWatcher)
- Direct cast to DataContext for ViewModel access

```csharp
// ❌ VIOLATION: Code-behind has UI/business logic
private void colTreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
{
    if (e.NewValue is FileNode fileNode)
    {
        var vm = (MainViewModel)DataContext;
        vm.LoadColCommand.Execute(fileNode);  // Could be data binding instead
    }
}

// ❌ VIOLATION: Timer/watcher logic in code-behind
private void StartAccentWatcher()
{
    var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(300) };
    timer.Tick += (s, e) => { ReloadAllTrees(); };
    timer.Start();
}

// ❌ VIOLATION: Direct tree control manipulation
private void ReloadAllTrees()
{
    _colTree.SaveState();
    _cgxTree.SaveState();
    // ...
}
```

**Recommendation:**
Move all logic to ViewModel:
- Tree refresh should be a ViewModel command
- Accent watcher should be part of ViewModel initialization
- Selection changed should use Attached Behavior + DataBinding

### 2. **ViewModel Instantiation in Code-Behind** (MEDIUM PRIORITY)

**Location:** `MainWindow.xaml.cs` constructor

```csharp
// ❌ VIOLATION: Manual ViewModel creation in View
public MainWindow()
{
    InitializeComponent();
    ViewModel = new MainViewModel();
    DataContext = ViewModel;
    // ...
}
```

**Problems:**
- Tight coupling between View and ViewModel
- Difficult to test View (ViewModel cannot be injected)
- Difficult to use design-time ViewModel in XAML preview

**Recommendation:**
- Use ViewModel-First or View-First with Dependency Injection
- Implement ViewModel locator pattern
- Or use XAML markup extension for ViewModel resolution

```csharp
// ✅ BETTER: Allow dependency injection
public MainWindow(MainViewModel viewModel)
{
    InitializeComponent();
    DataContext = viewModel;
}

// ✅ OR: Use XAML resource reference
// <Window.Resources>
//   <vm:MainViewModel x:Key="MainViewModel" />
// </Window.Resources>
// DataContext="{StaticResource MainViewModel}"
```

### 3. **HexWindow Code-Behind Logic** (MEDIUM PRIORITY)

**Location:** `HexWindow.xaml.cs`

```csharp
// ❌ VIOLATION: Business logic (clipboard copy) in code-behind
private void btnCopyHex_Click(object sender, RoutedEventArgs e)
{
    string raw = txtHex.Text.Replace(" ", "").Replace("\r", "").Replace("\n", "");
    Clipboard.SetText(raw);
}
```

**Recommendation:**
- Create ViewModel for HexWindow with CopyHexCommand
- Bind button to command instead of event handler

### 4. **Inconsistent ViewModel Implementation** (MEDIUM PRIORITY)

**Issues:**
- `CgxViewerViewModel` implements `INotifyPropertyChanged` directly instead of inheriting `ViewModelBase`
- Some ViewModels use `ViewModelBase`, others don't
- Inconsistent property declaration patterns

```csharp
// ❌ VIOLATION: Not inheriting ViewModelBase
public class CgxViewerViewModel : INotifyPropertyChanged
{
    // Duplicate OnPropertyChanged implementation
    public event PropertyChangedEventHandler PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

// ✅ CORRECT: Inheriting ViewModelBase
public class PaletteViewModel : ViewModelBase
{
    public bool ForceSingleRow
    {
        get => _forceSingleRow;
        set => SetProperty(ref _forceSingleRow, value);
    }
}
```

### 5. **UserControl (Swatch) Using DependencyProperties Instead of ViewModel Binding** (LOW PRIORITY)

**Location:** `UserControls/Swatch.xaml.cs`

```csharp
// ⚠️  MIXED PATTERN: DependencyProperties on UserControl
public String RGBColorHex
{
    get { return (String)GetValue(RGBColorHexProperty); }
    set { SetValue(RGBColorHexProperty, value); }
}
```

**Note:** This is acceptable for reusable UI components, but if Swatch needs internal logic, consider a ViewModel instead.

### 6. **TreeViewController Helper Class Design** (LOW PRIORITY)

**Issue:** 
- `TreeViewController` mixes TreeView manipulation with state management
- Could be better abstracted as an Attached Behavior

**Current:**
```csharp
private TreeViewController _colTree;
_colTree = new TreeViewController(colTreeView);
_colTree.SaveState();
```

**Better:**
- Create Attached Behavior for TreeView state management
- Use pure MVVM without helper classes

### 7. **Event Handlers in XAML** (LOW PRIORITY)

**Location:** `MainWindow.xaml`

```xaml
<!-- ❌ VIOLATION: Direct event handler instead of command/binding -->
<EventSetter Event="PreviewMouseDown" Handler="PaletteRow_PreviewMouseDown" />
```

**Problems:**
- Requires code-behind logic
- Cannot be tested independently
- Creates coupling between View and code-behind

**Recommendation:**
- Use InputBindings with commands
- Or use Attached Behaviors with ICommand

```xaml
<!-- ✅ BETTER: Use binding to command -->
<i:Interaction.Triggers>
    <i:EventTrigger EventName="PreviewMouseDown">
        <i:InvokeCommandAction Command="{Binding PaletteRowPreviewCommand}" />
    </i:EventTrigger>
</i:Interaction.Triggers>
```

### 8. **No Dependency Injection Container** (MEDIUM PRIORITY)

**Issues:**
- Services are instantiated manually
- No centralized object graph
- Difficult to mock for unit testing
- No interface-based dependency injection

**Current:**
```csharp
private AssetScannerService _scanner = new AssetScannerService();
```

**Recommendation:**
- Use Microsoft.Extensions.DependencyInjection
- Or Autofac, Ninject, etc.

---

## Compliance Scorecard

| Category | Score | Notes |
| --- | --- | --- |
| **Model Layer Separation** | ✅ 95% | Clean POCOs, proper folder structure |
| **ViewModel Layer** | ⚠️  75% | Good base class, but inconsistent implementation |
| **View Layer** | ❌ 60% | Too much code-behind, direct DataContext casts |
| **Commands/Events** | ⚠️  70% | RelayCommand good, but mixed with event handlers |
| **Data Binding** | ✅ 85% | Proper use of ObservableCollection and bindings |
| **Testability** | ❌ 40% | Hard to test Views due to code-behind; no DI |
| **Service Layer** | ⚠️  70% | Exists but not interface-based |
| **Overall MVVM** | ⚠️  **70%** | Solid foundation, needs refinement |

---

## Recommendations (Priority Order)

### 🔴 HIGH PRIORITY

1. **Move all code-behind logic to ViewModel**
   - `ReloadAllTrees()` → ViewModel method
   - `StartAccentWatcher()` → ViewModel initialization
   - Tree selection → Use Attached Behavior + command binding

2. **Implement Attached Behavior for TreeView Selection**
   ```csharp
   // Create TreeViewSelectionBehavior.cs
   public static class TreeViewSelectionBehavior
   {
       public static ICommand GetSelectedItemCommand(DependencyObject obj)
       public static void SetSelectedItemCommand(DependencyObject obj, ICommand value)
   }
   ```

3. **Use Dependency Injection (DI) Container**
   - Add Microsoft.Extensions.DependencyInjection
   - Create ServiceCollection in App.xaml.cs
   - Resolve MainViewModel via DI container

### 🟡 MEDIUM PRIORITY

4. **Standardize all ViewModels**
   - Make CgxViewerViewModel inherit ViewModelBase
   - Remove duplicate INotifyPropertyChanged implementations
   - Ensure consistent property declaration pattern

5. **Create ViewModel for HexWindow**
   - Add HexWindowViewModel with CopyHexCommand
   - Remove code-behind event handler

6. **Use ViewModel-First Approach with XAML Registration**
   ```xaml
   <Window.DataContext>
       <vm:MainViewModel />
   </Window.DataContext>
   ```

### 🟢 LOW PRIORITY

7. **Replace event handlers with Attached Behaviors**
   - `PaletteRow_PreviewMouseDown` → Custom Attached Behavior

8. **Refactor TreeViewController to Attached Behavior**
   - Eliminate mixin pattern
   - Use pure Attached Behavior for state management

9. **Interface-based Services**
   - Inject IAssetScannerService instead of concrete class
   - Improves testability and modularity

---

## MVVM Best Practices Checklist

- [ ] All ViewModels inherit from ViewModelBase or INotifyPropertyChanged
- [x] Models are simple, serializable POCOs
- [x] Services are responsible for business logic
- [ ] Views have no code-behind (or minimal binding infrastructure)
- [ ] Commands are used instead of event handlers
- [ ] No direct access to Model from View
- [ ] Dependency Injection container for loose coupling
- [x] ObservableCollection for dynamic collections
- [ ] Unit tests for ViewModel logic
- [ ] Design-time ViewModel support

---

## Example: Refactored Code-Behind Pattern

### Current (❌ Violates MVVM)

```csharp
public partial class MainWindow : Window
{
    private TreeViewController _colTree;

    public MainWindow()
    {
        InitializeComponent();
        ViewModel = new MainViewModel();
        DataContext = ViewModel;
        _colTree = new TreeViewController(colTreeView);
        colTreeView.ItemContainerGenerator.StatusChanged += OnTreeContainersGenerated;
    }

    private void colTreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
    {
        if (e.NewValue is FileNode fileNode)
        {
            var vm = (MainViewModel)DataContext;
            vm.LoadColCommand.Execute(fileNode);
        }
    }
}
```

### Refactored (✅ MVVM Compliant)

```csharp
// MainWindow.xaml.cs - Minimal code-behind
public partial class MainWindow : Window
{
    public MainWindow(MainViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}

// MainWindow.xaml - Behavior-based binding
<Window DataContext="{Binding MainViewModel, Source={StaticResource ViewModelLocator}}">
    <i:Interaction.Behaviors>
        <local:TreeViewSelectionBehavior Command="{Binding LoadColCommand}" />
    </i:Interaction.Behaviors>
    <TreeView x:Name="colTreeView" ItemsSource="{Binding ColTree.Items}" />
</Window>

// MainViewModel.cs - All logic
public class MainViewModel : ViewModelBase
{
    public ICommand LoadColCommand { get; }

    public MainViewModel()
    {
        LoadColCommand = new RelayCommand<FileNode>(LoadCol);
        InitializeWatchers();
    }

    private void InitializeWatchers()
    {
        var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(300) };
        timer.Tick += (s, e) => RefreshTrees();
        timer.Start();
    }
}
```

---

## Conclusion

The SNESassetsWPF project has a **good foundation** in MVVM but needs refinement in several key areas:

1. **Code-behind should be minimal** - Currently too much logic
2. **ViewModels need standardization** - Use consistent base class
3. **Dependency Injection is critical** - For testability and loose coupling
4. **Commands over events** - Already using RelayCommand, extend to all interactions
5. **Service layer strengthening** - Make services interface-based

With these improvements, the project can reach **90%+ MVVM compliance** and significantly improve testability, maintainability, and reusability.

---

*Analysis completed for SNESassetsWPF (.NET 10 WPF)*
