# Changelog

All notable changes to Image Comparator will be documented in this file.

## [1.1.0] - 2026-01-30

### Added
- Persian (Farsi) language support - Application now supports 19 languages total
- Error logging system - Application errors are now automatically logged to help diagnose issues
- File deletion feedback - Users are now informed when file deletions fail

### Fixed
- **Security**: Replaced insecure BinaryFormatter with JSON serialization (addresses CVE-2017-8759)
- **Security**: Added path validation to file/folder operations to prevent unauthorized access
- **Crash**: Fixed application crash when closing with active processes
- **Performance**: Optimized duplicate deletion algorithm (540x faster for large result sets)
- **Memory**: Fixed memory leaks in image processing that could cause resource exhaustion
- **Bug**: Fixed ColorMatrix grayscale conversion producing incorrect results
- **Bug**: Fixed pixel coordinate issue in vdHash algorithm
- **Bug**: File deletion now correctly removes only successfully deleted items from the list

### Changed
- Improved error handling throughout the application with better user feedback
- Reorganized project structure with new Common library for shared utilities

## [1.0.1] - 2026-01-24

Initial tagged release.
