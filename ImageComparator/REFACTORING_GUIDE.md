# Refactoring Guide: Migrating to MVVM Architecture

## Overview

This guide provides step-by-step instructions for gradually migrating the ImageComparator application from its current code-behind heavy architecture to a clean MVVM pattern. The migration strategy ensures backward compatibility while introducing modern practices.

## Migration Strategy

### Phase 1: New Features Use MVVM ✅
**Status: COMPLETE**

All new infrastructure is in place:
- Commands (RelayCommand, AsyncRelayCommand)
- BaseViewModel with INotifyPropertyChanged
- Services (DialogService, FileService)
- Models (ImageComparisonResult, ComparisonSettings, ImageData)
- ViewModels (MainWindowViewModel)
- Utilities (ImageUtility, HashUtility)
- Constants for magic strings

### Phase 2: Parallel Implementation (Current Phase)
**Goal: Run old and new code side-by-side**

The existing MainWindow.xaml.cs will continue to work as-is. New features and bug fixes should use the MVVM pattern.

#### How to Use the ViewModel Alongside Existing Code:

1. **Access the ViewModel**
```csharp
// In MainWindow.xaml.cs constructor or method
var viewModel = ViewModelLocator.Instance.MainWindowViewModel;
```

2. **Sync Data Between Old and New**
```csharp
// When adding a directory in old code
directories.Add(folderPath);
// Also add to ViewModel
ViewModelLocator.Instance.MainWindowViewModel.Directories.Add(folderPath);

// Or listen to ViewModel changes
viewModel.PropertyChanged += (s, e) => {
    if (e.PropertyName == nameof(viewModel.ProgressPercentage))
    {
        // Update old UI elements
        percentage.Value = viewModel.ProgressPercentage;
    }
};
```

### Phase 3: Gradual Method Migration

#### Pattern for Migrating Event Handlers

**Before (Code-Behind):**
```csharp
private void AddFolderButton_Click(object sender, RoutedEventArgs e)
{
    folderBrowserDialog.Description = LocalizationManager.GetString("Dialog.AddFolderTitle");
    
    if ((bool)folderBrowserDialog.ShowDialog())
    {
        if (directories.Count == 0)
        {
            console.Clear();
            console.Add(LocalizationManager.GetString("Label.DragDropFolders"));
        }
        
        if (!directories.Contains(folderBrowserDialog.SelectedPath))
        {
            directories.Add(folderBrowserDialog.SelectedPath);
            console.Insert(console.Count - 1, 
                LocalizationManager.GetString("Console.DirectoryAdded", folderBrowserDialog.SelectedPath));
        }
    }
}
```

**After (MVVM with Command):**

In ViewModel:
```csharp
private void ExecuteAddFolder(object parameter)
{
    var folderPath = _dialogService.ShowFolderBrowserDialog(
        LocalizationManager.GetString("Dialog.AddFolderTitle"));
    
    if (!string.IsNullOrEmpty(folderPath))
    {
        if (Directories.Count == 0)
        {
            ConsoleMessages.Clear();
            ConsoleMessages.Add(LocalizationManager.GetString("Label.DragDropFolders"));
        }
        
        if (!Directories.Contains(folderPath))
        {
            Directories.Add(folderPath);
            ConsoleMessages.Insert(ConsoleMessages.Count - 1,
                LocalizationManager.GetString("Console.DirectoryAdded", folderPath));
        }
    }
}
```

In XAML:
```xml
<!-- Instead of Click="AddFolderButton_Click" -->
<Button Command="{Binding AddFolderCommand}" Content="Add Folder" />
```

In Code-Behind (during transition):
```csharp
private void AddFolderButton_Click(object sender, RoutedEventArgs e)
{
    // Delegate to ViewModel
    var viewModel = ViewModelLocator.Instance.MainWindowViewModel;
    viewModel.AddFolderCommand.Execute(null);
}
```

### Phase 4: Extract Business Logic to Services

#### Pattern for Creating Services

**Identify Reusable Logic:**
```csharp
// In MainWindow.xaml.cs - BEFORE
private void ProcessThreadStart()
{
    // 200+ lines of hash calculation and image processing
    // ...
}
```

**Extract to Service - AFTER:**

```csharp
// In Services/ImageHashService.cs
public class ImageHashService : IImageHashService
{
    public ImageHashData CalculateHashes(string filePath, bool duplicatesOnly)
    {
        var result = new ImageHashData();
        
        using (var image = new Bitmap(filePath))
        {
            result.Resolution = image.Size;
            result.Orientation = ImageUtility.GetOrientation(image.Width, image.Height);
            result.Sha256 = HashUtility.CalculateSHA256(filePath);
            
            if (!duplicatesOnly)
            {
                result.PHash = CalculatePHash(image);
                result.HdHash = CalculateHdHash(image);
                result.VdHash = CalculateVdHash(image);
                result.AHash = CalculateAHash(image);
            }
        }
        
        return result;
    }
    
    private int[] CalculatePHash(Bitmap image)
    {
        // Extracted pHash logic
    }
    
    // ... other hash methods
}
```

**Use in ViewModel:**
```csharp
public class MainWindowViewModel : BaseViewModel
{
    private readonly IImageHashService _hashService;
    
    public MainWindowViewModel(IDialogService dialogService, IFileService fileService, IImageHashService hashService)
    {
        _dialogService = dialogService;
        _fileService = fileService;
        _hashService = hashService;
    }
    
    private async Task ProcessImagesAsync()
    {
        foreach (var file in Files)
        {
            var hashes = await Task.Run(() => _hashService.CalculateHashes(file, Settings.DuplicatesOnly));
            // Use hash data...
        }
    }
}
```

### Phase 5: Update XAML Bindings

#### Converting to Data Binding

**Before:**
```xml
<ProgressBar Name="progressBar" />
```

```csharp
// In code-behind
progressBar.Value = 50;
```

**After:**
```xml
<Window DataContext="{Binding Source={StaticResource ViewModelLocator}, Path=MainWindowViewModel}">
    <ProgressBar Value="{Binding ProgressPercentage}" />
</Window>
```

```csharp
// In ViewModel
public int ProgressPercentage
{
    get => _progressPercentage;
    set => SetProperty(ref _progressPercentage, value);
}

// Anywhere in ViewModel
ProgressPercentage = 50; // UI updates automatically
```

#### Add ViewModelLocator to App.xaml

```xml
<Application x:Class="ImageComparator.App"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:local="clr-namespace:ImageComparator">
    <Application.Resources>
        <local:ViewModelLocator x:Key="ViewModelLocator" />
    </Application.Resources>
</Application>
```

### Phase 6: Modernize Threading

#### Replace Thread with async/await

**Before:**
```csharp
processThread = new Thread(Run);
processThread.Start();

private void Run()
{
    // Long running operation
    // Updates UI using Dispatcher.Invoke
}
```

**After:**
```csharp
private async Task RunAsync(CancellationToken cancellationToken)
{
    await Task.Run(() => {
        // Long running operation
        // Report progress
    }, cancellationToken);
}

// In command
private async void ExecuteFindDuplicates(object parameter)
{
    _cancellationTokenSource = new CancellationTokenSource();
    try
    {
        await RunAsync(_cancellationTokenSource.Token);
    }
    catch (OperationCanceledException)
    {
        // Handle cancellation
    }
}
```

### Phase 7: Testing Strategy

#### Unit Testing ViewModels

```csharp
[Test]
public void AddFolder_ValidPath_AddsToDirectories()
{
    // Arrange
    var mockDialogService = new Mock<IDialogService>();
    mockDialogService.Setup(x => x.ShowFolderBrowserDialog(It.IsAny<string>()))
                     .Returns("C:\\TestFolder");
    
    var fileService = new FileService();
    var viewModel = new MainWindowViewModel(mockDialogService.Object, fileService);
    
    // Act
    viewModel.AddFolderCommand.Execute(null);
    
    // Assert
    Assert.Contains("C:\\TestFolder", viewModel.Directories);
    Assert.True(viewModel.ConsoleMessages.Count > 0);
}
```

## Common Patterns

### 1. Property with Validation

```csharp
private string _searchPattern;
public string SearchPattern
{
    get => _searchPattern;
    set
    {
        if (SetProperty(ref _searchPattern, value))
        {
            // Validation or side effects
            OnPropertyChanged(nameof(CanSearch));
        }
    }
}

public bool CanSearch => !string.IsNullOrEmpty(SearchPattern);
```

### 2. Async Command with Progress

```csharp
public ICommand ProcessCommand { get; }

public MainWindowViewModel()
{
    ProcessCommand = new AsyncRelayCommand(ExecuteProcessAsync, CanExecuteProcess);
}

private async Task ExecuteProcessAsync(object parameter)
{
    IsProcessing = true;
    ProgressPercentage = 0;
    
    try
    {
        var progress = new Progress<int>(percent => ProgressPercentage = percent);
        await _service.ProcessAsync(progress);
    }
    finally
    {
        IsProcessing = false;
    }
}
```

### 3. Collection Changes

```csharp
// ObservableCollection automatically notifies UI
public ObservableCollection<string> Items { get; } = new ObservableCollection<string>();

public void AddItem(string item)
{
    Items.Add(item); // UI updates automatically
}
```

### 4. Dependent Properties

```csharp
private int _selectedItemsCount;
public int SelectedItemsCount
{
    get => _selectedItemsCount;
    set
    {
        if (SetProperty(ref _selectedItemsCount, value))
        {
            // Notify dependent properties
            OnPropertyChanged(nameof(HasSelection));
            OnPropertyChanged(nameof(SelectionSummary));
        }
    }
}

public bool HasSelection => SelectedItemsCount > 0;
public string SelectionSummary => $"{SelectedItemsCount} items selected";
```

## Best Practices Checklist

- [ ] Keep ViewModels free of UI element references (no `Button`, `TextBox`, etc.)
- [ ] Use Commands instead of event handlers
- [ ] Use Services for reusable business logic
- [ ] Use async/await instead of Thread
- [ ] Implement INotifyPropertyChanged for all bindable properties
- [ ] Use ObservableCollection for collections bound to UI
- [ ] Add XML documentation to public APIs
- [ ] Follow naming conventions (PascalCase for public, _camelCase for private fields)
- [ ] Use Constants instead of magic strings
- [ ] Inject dependencies through constructor
- [ ] Keep methods focused and small (Single Responsibility)
- [ ] Handle exceptions appropriately
- [ ] Use CancellationToken for long-running operations

## Troubleshooting

### "Property not updating in UI"
- Ensure property raises `OnPropertyChanged`
- Check DataContext is set correctly
- Verify binding path matches property name

### "Command not executing"
- Check `CanExecute` returns true
- Verify Command is bound correctly in XAML
- Ensure CommandManager.InvalidateRequerySuggested() is called if needed

### "Collection changes not reflecting in UI"
- Use `ObservableCollection` instead of `List`
- Ensure collection changes happen on UI thread

## References

- See `MVVM_ARCHITECTURE.md` for architecture overview
- See individual class XML documentation for API details
- [Microsoft MVVM Documentation](https://docs.microsoft.com/en-us/archive/msdn-magazine/2009/february/patterns-wpf-apps-with-the-model-view-viewmodel-design-pattern)

## Next Steps

1. Create `ImageComparisonService` to encapsulate hash calculation logic
2. Migrate one complex event handler (e.g., `FindDuplicatesButton_Click`) to ViewModel
3. Update MainWindow.xaml to use data binding for console output
4. Add unit tests for ViewModel logic
5. Gradually migrate remaining event handlers

Remember: The goal is gradual improvement, not a complete rewrite. Each small step makes the codebase better!
