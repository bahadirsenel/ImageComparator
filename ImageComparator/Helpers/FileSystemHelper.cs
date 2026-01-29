using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;

namespace ImageComparator.Helpers
{
    /// <summary>
    /// Provides safe file system operations with validation
    /// </summary>
    public static class FileSystemHelper
    {
        private static readonly string[] AllowedImageExtensions = 
        {
            ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".tiff", ".tif", ".ico"
        };

        /// <summary>
        /// Safely opens a file with comprehensive validation
        /// </summary>
        /// <param name="filePath">Path to the file to open</param>
        /// <returns>True if successful, false otherwise</returns>
        public static bool SafeOpenFile(string filePath)
        {
            try
            {
                // Validate input
                if (string.IsNullOrWhiteSpace(filePath))
                {
                    return false;
                }

                // Normalize path (prevents traversal attacks)
                string fullPath = Path.GetFullPath(filePath);

                // Check if file exists
                if (!File.Exists(fullPath))
                {
                    MessageBox.Show(
                        LocalizationManager.GetString("Error.FileNotFound", Path.GetFileName(fullPath)),
                        LocalizationManager.GetString("Error.Title"),
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning
                    );
                    return false;
                }

                // Validate file extension
                string extension = Path.GetExtension(fullPath).ToLowerInvariant();
                if (!AllowedImageExtensions.Contains(extension))
                {
                    MessageBox.Show(
                        LocalizationManager.GetString("Error.UnsupportedFileType", extension),
                        LocalizationManager.GetString("Error.Title"),
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning
                    );
                    return false;
                }

                // Validate file size (prevent loading huge files accidentally)
                var fileInfo = new FileInfo(fullPath);
                if (fileInfo.Length > 100 * 1024 * 1024) // 100MB limit
                {
                    var result = MessageBox.Show(
                        LocalizationManager.GetString("Warning.LargeFile", fileInfo.Length / (1024 * 1024)),
                        LocalizationManager.GetString("Warning.Title"),
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Warning
                    );
                    
                    if (result != MessageBoxResult.Yes)
                    {
                        return false;
                    }
                }

                // Use ProcessStartInfo for better control and security
                var startInfo = new ProcessStartInfo
                {
                    FileName = fullPath,
                    UseShellExecute = true,
                    Verb = "open",
                    ErrorDialog = false
                };

                Process.Start(startInfo);
                return true;
            }
            catch (System.ComponentModel.Win32Exception ex)
            {
                // No default application for file type
                MessageBox.Show(
                    LocalizationManager.GetString("Error.NoDefaultApp", Path.GetExtension(filePath), ex.Message),
                    LocalizationManager.GetString("Error.Title"),
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
                return false;
            }
            catch (Exception ex) when (!(ex is OutOfMemoryException))
            {
                MessageBox.Show(
                    LocalizationManager.GetString("Error.CannotOpenFile", ex.Message),
                    LocalizationManager.GetString("Error.Title"),
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
                return false;
            }
        }

        /// <summary>
        /// Safely opens a folder in Windows Explorer with validation
        /// </summary>
        /// <param name="folderPath">Path to the folder to open</param>
        /// <returns>True if successful, false otherwise</returns>
        public static bool SafeOpenFolder(string folderPath)
        {
            try
            {
                // Validate input
                if (string.IsNullOrWhiteSpace(folderPath))
                {
                    return false;
                }

                // Normalize path
                string fullPath = Path.GetFullPath(folderPath);

                // Check if directory exists
                if (!Directory.Exists(fullPath))
                {
                    MessageBox.Show(
                        LocalizationManager.GetString("Error.FolderNotFound", fullPath),
                        LocalizationManager.GetString("Error.Title"),
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning
                    );
                    return false;
                }

                // Use explorer.exe explicitly (more secure than UseShellExecute)
                var startInfo = new ProcessStartInfo
                {
                    FileName = "explorer.exe",
                    Arguments = $"\"{fullPath}\"",
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                Process.Start(startInfo);
                return true;
            }
            catch (Exception ex) when (!(ex is OutOfMemoryException))
            {
                MessageBox.Show(
                    LocalizationManager.GetString("Error.CannotOpenFolder", ex.Message),
                    LocalizationManager.GetString("Error.Title"),
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
                return false;
            }
        }

        /// <summary>
        /// Safely extracts directory from file path
        /// </summary>
        /// <param name="filePath">Full file path</param>
        /// <returns>Directory path or null if invalid</returns>
        public static string SafeGetDirectory(string filePath)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(filePath))
                {
                    return null;
                }

                string fullPath = Path.GetFullPath(filePath);
                return Path.GetDirectoryName(fullPath);
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Validates if path is a safe image file
        /// </summary>
        public static bool IsValidImagePath(string filePath)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(filePath))
                    return false;

                string extension = Path.GetExtension(filePath).ToLowerInvariant();
                return AllowedImageExtensions.Contains(extension);
            }
            catch
            {
                return false;
            }
        }
    }
}
