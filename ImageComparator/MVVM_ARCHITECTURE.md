# MVVM Architecture Refactoring

## Overview

This document describes the MVVM (Model-View-ViewModel) architecture implementation for the ImageComparator application. The refactoring introduces proper separation of concerns and follows SOLID principles while maintaining backward compatibility with the existing codebase.

## Architecture Layers

### 1. Models (`Models/`)
Models represent the data and business entities of the application.

- **`ImageComparisonResult.cs`** - Represents a comparison result with similarity metrics
- **`ComparisonSettings.cs`** - Configuration for comparison operations
- **`ImageData.cs`** - Image metadata and enums (Orientation, Confidence, State)
- **`DiscreteCosineTransform2D.cs`** - DCT algorithm (existing)
- **`MyInt.cs`** - Observable integer (existing)
- **`ImageViewControl.cs`** - Image display control (existing)

### 2. ViewModels (`ViewModels/`)
ViewModels handle UI logic, state management, and expose data to Views through data binding.

- **`BaseViewModel.cs`** - Base class implementing INotifyPropertyChanged
- **`MainWindowViewModel.cs`** - Main application ViewModel

#### Key Features:
- Property change notification for UI updates
- Command pattern for user actions
- No direct UI element references
- Testable business logic

### 3. Views (`Views/`)
XAML files and their code-behind. Currently, views remain in the root folder but will be moved here in future iterations.

- MainWindow.xaml
- AboutWindow.xaml
- ApplyPopupWindow.xaml
- ClearPopupWindow.xaml
- HowToUseWindow.xaml

### 4. Services (`Services/`)
Services encapsulate reusable functionality and external dependencies.

- **`IDialogService` / `DialogService`** - File and folder dialogs
- **`IFileService` / `FileService`** - File system operations
- **`LocalizationManager`** - Internationalization (existing)

#### Benefits:
- Testability through interfaces
- Loose coupling
- Reusability across ViewModels

### 5. Commands (`Commands/`)
Command pattern implementations for binding UI actions.

- **`RelayCommand`** - Synchronous command execution
- **`AsyncRelayCommand`** - Asynchronous command execution with status tracking

### 6. Utilities (`Utilities/`)
Helper classes for common operations (to be populated in future iterations).

## Dependency Injection

The application uses a simple Service Locator pattern through `ViewModelLocator` for dependency management.

```csharp
// In App.xaml or View code-behind
var viewModel = ViewModelLocator.Instance.MainWindowViewModel;
```

## Migration Strategy

The refactoring follows an incremental approach to minimize disruption:

### Phase 1: Foundation (COMPLETED ✅)
- Created folder structure
- Implemented base infrastructure (Commands, BaseViewModel)
- Added Models and Services
- Established ViewModel architecture

### Phase 2: Gradual Migration (CURRENT)
- Existing `MainWindow.xaml.cs` remains functional
- New code should use ViewModels and Services
- Gradually extract methods from code-behind to ViewModels

### Phase 3: Full Migration (FUTURE)
- Complete extraction of business logic from code-behind
- Update XAML for full data binding
- Minimal code-behind (only View-specific code)

## Best Practices Applied

### SOLID Principles
- **Single Responsibility**: Each class has one clear purpose
- **Open/Closed**: Easy to extend without modifying existing code
- **Liskov Substitution**: Interfaces allow for easy substitution
- **Interface Segregation**: Focused interfaces (IDialogService, IFileService)
- **Dependency Inversion**: Depend on abstractions (interfaces) not implementations

### Design Patterns
- **MVVM**: Separation of UI, logic, and data
- **Command Pattern**: Encapsulated user actions
- **Observer Pattern**: INotifyPropertyChanged for UI updates
- **Service Locator**: ViewModelLocator for dependency management

### Code Quality
- XML documentation on public APIs
- Meaningful naming conventions
- Proper exception handling
- Resource management

## Usage Examples

### Creating a ViewModel

```csharp
public class MyViewModel : BaseViewModel
{
    private string _myProperty;
    
    public string MyProperty
    {
        get => _myProperty;
        set => SetProperty(ref _myProperty, value);
    }
    
    public ICommand MyCommand { get; }
    
    public MyViewModel()
    {
        MyCommand = new RelayCommand(ExecuteMyCommand, CanExecuteMyCommand);
    }
    
    private bool CanExecuteMyCommand(object parameter)
    {
        return !string.IsNullOrEmpty(MyProperty);
    }
    
    private void ExecuteMyCommand(object parameter)
    {
        // Command logic here
    }
}
```

### Using Services

```csharp
public class MyViewModel : BaseViewModel
{
    private readonly IDialogService _dialogService;
    private readonly IFileService _fileService;
    
    public MyViewModel(IDialogService dialogService, IFileService fileService)
    {
        _dialogService = dialogService;
        _fileService = fileService;
    }
    
    private void OpenFolder()
    {
        var path = _dialogService.ShowFolderBrowserDialog("Select a folder");
        if (!string.IsNullOrEmpty(path))
        {
            var files = _fileService.ScanDirectory(path, true, true, true, true, true, true, true);
            // Process files...
        }
    }
}
```

### Binding in XAML

```xaml
<Window xmlns:vm="clr-namespace:ImageComparator.ViewModels"
        DataContext="{Binding Source={StaticResource ViewModelLocator}, Path=MainWindowViewModel}">
    
    <TextBlock Text="{Binding ProgressPercentage}" />
    <Button Command="{Binding AddFolderCommand}" Content="Add Folder" />
    <ListBox ItemsSource="{Binding ConsoleMessages}" />
    
</Window>
```

## Testing

With the new architecture, components are easily testable:

```csharp
[Test]
public void AddFolder_AddsToDirectoryList()
{
    // Arrange
    var mockDialogService = new MockDialogService();
    var fileService = new FileService();
    var viewModel = new MainWindowViewModel(mockDialogService, fileService);
    
    mockDialogService.FolderToReturn = "C:\\TestFolder";
    
    // Act
    viewModel.AddFolderCommand.Execute(null);
    
    // Assert
    Assert.Contains("C:\\TestFolder", viewModel.Directories);
}
```

## Future Enhancements

1. **Dependency Injection Container**: Migrate from Service Locator to proper DI (Microsoft.Extensions.DependencyInjection)
2. **Unit Tests**: Add comprehensive test coverage
3. **Async/Await**: Replace Thread usage with modern async patterns
4. **Additional Services**: 
   - ImageComparisonService (hash calculation, comparison)
   - ImageHashService (pHash, aHash, vHash, hHash)
5. **More ViewModels**: Break down MainWindowViewModel into smaller, focused ViewModels

## Contributing

When adding new features:
1. Follow the established architecture patterns
2. Use Services for reusable logic
3. Implement Commands for user actions
4. Use ViewModels for UI state and logic
5. Keep Views (XAML and code-behind) minimal
6. Add XML documentation to public APIs
7. Consider testability in design

## Resources

- [MVVM Pattern Documentation](https://docs.microsoft.com/en-us/archive/msdn-magazine/2009/february/patterns-wpf-apps-with-the-model-view-viewmodel-design-pattern)
- [SOLID Principles](https://en.wikipedia.org/wiki/SOLID)
- [WPF Data Binding](https://docs.microsoft.com/en-us/dotnet/desktop/wpf/data/)
- [Command Pattern in WPF](https://docs.microsoft.com/en-us/dotnet/desktop/wpf/advanced/commanding-overview)
