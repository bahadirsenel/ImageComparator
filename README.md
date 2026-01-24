# Image Comparator

A powerful Windows desktop application for finding and managing duplicate images using advanced image comparison algorithms.

## Overview

Image Comparator is a WPF-based tool that helps you identify duplicate and similar images across multiple folders. Using Discrete Cosine Transform (DCT) algorithms, it can detect both exact duplicates and visually similar images, even if they have been resized, compressed, or slightly modified.

## Features

### Core Functionality
- **Smart Duplicate Detection**: Find exact duplicates and similar images using DCT-based perceptual hashing
- **Multi-Format Support**: Works with JPEG, BMP, PNG, GIF, TIFF, and ICO formats
- **Recursive Folder Scanning**: Include subfolders in your search
- **Side-by-Side Comparison**: Preview matched images with zoom and pan capabilities
- **Batch Operations**: Select and manage multiple files at once

### Advanced Options
- **Confidence Levels**: Color-coded indicators show the similarity confidence:
  - 🔵 Blue: Duplicate (exact match)
  - 🔴 Red: High confidence of similarity
  - 🟡 Yellow: Medium confidence of similarity
  - 🟢 Green: Low confidence of similarity
- **Orientation Filtering**: Option to skip images with different orientations (ignores portrait vs landscape matches)
- **Exact Match Mode**: Find only byte-perfect duplicates (similar files won't be shown)
- **False Positive Management**: Mark incorrect matches to improve future scans - these pairs will be remembered and won't appear in future scans

### File Management
- **Safe Deletion**: Send files to Recycle Bin (default) or delete permanently
- **Delete Selected**: Removes checked files from your system
- **Mark For Deletion**: Highlights files for later deletion (shown in red background)
- **Remove From List**: Removes items from results without deleting files from your system
- **Save/Load Results**: Save your current findings to continue later
- **File Explorer Integration**: Open files or navigate to their location with right-click menu

### User Experience
- **Multilingual Interface**: Supports 18 languages including English, Türkçe, 日本語, Español, Français, Deutsch, Italiano, Português (Brasil), Русский, 简体中文, 한국어, العربية, हिन्दी, Nederlands, Polski, Svenska, Norsk, and Dansk. Switch languages via Options > Language, and the interface will update immediately
- **Drag-and-Drop Support**: Add folders by dragging them into the output list at the bottom
- **Keyboard Navigation**: Use arrow keys to browse through results quickly
- **Progress Tracking**: Monitor scan progress with pause/resume/stop functionality using the respective buttons

## Requirements

- Windows 7 or later
- .NET Framework 4.7.2 or higher
- Sufficient disk space for temporary file operations

## Installation

1. Download the latest release from the [Releases](https://github.com/bahadirsenel/ImageComparator/releases) page
2. Extract the ZIP file to your desired location
3. Run `ImageComparator.exe`

## Building from Source

### Prerequisites
- Visual Studio 2017 or later
- .NET Framework 4.7.2 SDK

### Build Steps
1. Clone the repository:
   ```bash
   git clone https://github.com/bahadirsenel/ImageComparator.git
   ```
2. Open `ImageComparator.sln` in Visual Studio
3. Restore NuGet packages
4. Build the solution (F6 or Build → Build Solution)
5. The executable will be in `ImageComparator/bin/Debug/` or `ImageComparator/bin/Release/`

## Usage

### Quick Start
1. **Add Folders**: Click "Add Folder" button or drag-and-drop folders into the output list at the bottom
2. **Configure Options**: Set search formats and other preferences in the Options menu
3. **Find Duplicates**: Click "Find Duplicates" to start the comparison process
4. **Review Results**: Duplicate pairs are shown in two side-by-side lists. Click on any file to preview it in the center panel
5. **Take Action**: Select files using checkboxes to delete, mark as false positives, or save results

### Options Menu

#### Search Formats
Go to Options > Search Formats to select which image types to scan:
- JPEG
- BMP
- PNG
- GIF
- TIFF
- ICO

All formats are enabled by default.

#### Deletion Method
Go to Options > Deletion Method to choose:
- **Send To Recycle Bin** (Default): Safer option, allows file recovery
- **Delete Permanently**: Files are deleted permanently without recovery option

#### Additional Settings
- **Include Subfolders**: Scans all subfolders within selected folders
- **Skip Files With Different Orientation**: Ignores portrait vs landscape matches
- **Find Exact Duplicates Only**: Only finds exact duplicate files, similar files won't be shown
- **Clear False Positive Database**: Clears the remembered false positive pairs (Options > Clear False Positive Database)

### Keyboard Shortcuts
- **Arrow Keys**: Navigate through results quickly
- **Space**: Toggle checkbox selection
- **Double-Click**: Open file in default image viewer
- **Right-Click**: Context menu to open files or view their location

### Preview and Zoom
- **Mouse Wheel**: Zoom in/out on preview images
- **+/- Buttons**: Alternative zoom controls
- **Click and Drag**: Pan around zoomed images

### Saving and Loading Results
- **File > Save Results**: Saves your current findings to continue later
- **File > Load Results**: Loads previously saved results

## How It Works

Image Comparator uses the Discrete Cosine Transform (DCT) algorithm to create perceptual hashes of images. This technique:

1. Resizes images to a standardized dimension
2. Converts to grayscale
3. Applies DCT to extract frequency components
4. Generates a compact hash representing the image's visual features
5. Compares hashes using Hamming distance to determine similarity

This approach allows the application to detect:
- Exact duplicates
- Images with different resolutions
- Re-compressed or slightly edited images
- Images with minor color adjustments

## Technologies

- **C# / .NET Framework 4.7.2**: Core application framework
- **WPF (Windows Presentation Foundation)**: User interface
- **Discrete Cosine Transform**: Image comparison algorithm
- **Ookii Dialogs**: Modern Windows dialogs
- **System.Drawing**: Image processing

## Project Structure

```
ImageComparator/
├── ImageComparator/          # Main application code
│   ├── Models/              # Core algorithms (DCT, etc.)
│   ├── Resources/           # Images, localization, documentation
│   ├── MainWindow.xaml      # Main application window
│   └── LocalizationManager.cs # Language support
├── ImageComparator.sln      # Visual Studio solution
└── README.md               # This file
```

## License

This project is licensed under the GNU General Public License v3.0 - see the [LICENSE.txt](LICENSE.txt) file for details.

## Contributing

Contributions are welcome! Please feel free to submit issues or pull requests.

1. Fork the repository
2. Create your feature branch (`git checkout -b feature/amazing-feature`)
3. Commit your changes (`git commit -m 'Add some amazing feature'`)
4. Push to the branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request

## Acknowledgments

- DCT algorithm implementation for perceptual image hashing
- Ookii Dialogs for modern Windows UI components

## Support

If you encounter any issues or have questions:
- Open an issue on [GitHub Issues](https://github.com/bahadirsenel/ImageComparator/issues)
- Check the built-in "How To Use" guide (Help → How To Use)

---

**Note**: This application is designed for personal use to help manage image collections. Always review files before deletion and maintain backups of important data.