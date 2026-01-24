# ImageComparator

A WPF application for finding duplicate and similar images using perceptual hashing algorithms.

## 🎯 Recent Updates - MVVM Refactoring

The project has been refactored to follow MVVM (Model-View-ViewModel) architecture and modern OOP best practices. **All existing functionality remains intact and backward compatible.**

### What's New

✅ **Complete MVVM Infrastructure**
- Commands (RelayCommand, AsyncRelayCommand)
- ViewModels with INotifyPropertyChanged
- Services layer with dependency injection
- Utility classes for reusable functionality

✅ **Enhanced Architecture**
- Clear separation of concerns (Models, Views, ViewModels, Services)
- SOLID principles applied throughout
- Testable code structure
- Comprehensive XML documentation

✅ **Developer Documentation**
- [MVVM_ARCHITECTURE.md](ImageComparator/MVVM_ARCHITECTURE.md) - Architecture overview
- [REFACTORING_GUIDE.md](ImageComparator/REFACTORING_GUIDE.md) - Step-by-step migration guide
- [REFACTORING_SUMMARY.md](ImageComparator/REFACTORING_SUMMARY.md) - Complete summary (Turkish/English)

### Project Structure

```
ImageComparator/
├── Commands/           # Command pattern implementations
├── Models/             # Data models and business entities
├── ViewModels/         # UI logic and state management
├── Services/           # Reusable business logic
├── Utilities/          # Helper functions
├── Views/              # XAML views (to be organized)
└── Resources/          # Localization and assets
```

### For Developers

See the comprehensive documentation:
- **[MVVM_ARCHITECTURE.md](ImageComparator/MVVM_ARCHITECTURE.md)** - Learn about the new architecture
- **[REFACTORING_GUIDE.md](ImageComparator/REFACTORING_GUIDE.md)** - How to work with the new structure
- **[REFACTORING_SUMMARY.md](ImageComparator/REFACTORING_SUMMARY.md)** - Detailed summary of changes

## Features

- Find duplicate images using SHA256 checksums
- Find similar images using perceptual hashing (pHash, aHash, hdHash, vdHash)
- Support for multiple image formats (JPEG, PNG, GIF, BMP, TIFF, ICO)
- Configurable similarity thresholds
- Batch operations (delete, mark as false positive)
- Multi-threaded processing
- Localization support (English, Turkish)

## Getting Started

### Prerequisites

- .NET Framework 4.7.2 or higher
- Windows OS

### Building

Open `ImageComparator.sln` in Visual Studio and build the solution.

## Contributing

When contributing new code, please follow the MVVM architecture patterns documented in the project. See [REFACTORING_GUIDE.md](ImageComparator/REFACTORING_GUIDE.md) for guidelines.

## License

See LICENSE.txt for details.