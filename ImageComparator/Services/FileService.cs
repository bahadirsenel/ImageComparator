using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ImageComparator.Services
{
    /// <summary>
    /// Service for file system operations including scanning directories
    /// and filtering image files.
    /// </summary>
    public interface IFileService
    {
        /// <summary>
        /// Scans a directory for image files based on file type filters.
        /// </summary>
        /// <param name="directoryPath">The directory to scan.</param>
        /// <param name="includeSubfolders">Whether to include subfolders.</param>
        /// <param name="jpegEnabled">Include JPEG files.</param>
        /// <param name="gifEnabled">Include GIF files.</param>
        /// <param name="pngEnabled">Include PNG files.</param>
        /// <param name="bmpEnabled">Include BMP files.</param>
        /// <param name="tiffEnabled">Include TIFF files.</param>
        /// <param name="icoEnabled">Include ICO files.</param>
        /// <returns>List of image file paths.</returns>
        List<string> ScanDirectory(string directoryPath, bool includeSubfolders,
            bool jpegEnabled, bool gifEnabled, bool pngEnabled, bool bmpEnabled,
            bool tiffEnabled, bool icoEnabled);

        /// <summary>
        /// Gets all directories from a list of paths (expands subdirectories if needed).
        /// </summary>
        /// <param name="paths">List of paths to process.</param>
        /// <param name="includeSubfolders">Whether to include subfolders.</param>
        /// <returns>List of directory paths.</returns>
        List<string> GetDirectories(List<string> paths, bool includeSubfolders);
    }

    /// <summary>
    /// Implementation of file service.
    /// </summary>
    public class FileService : IFileService
    {
        private static readonly string[] JpegExtensions = { ".jpg", ".jpeg" };
        private static readonly string[] GifExtensions = { ".gif" };
        private static readonly string[] PngExtensions = { ".png" };
        private static readonly string[] BmpExtensions = { ".bmp" };
        private static readonly string[] TiffExtensions = { ".tif", ".tiff" };
        private static readonly string[] IcoExtensions = { ".ico" };

        public List<string> ScanDirectory(string directoryPath, bool includeSubfolders,
            bool jpegEnabled, bool gifEnabled, bool pngEnabled, bool bmpEnabled,
            bool tiffEnabled, bool icoEnabled)
        {
            var files = new List<string>();

            if (!Directory.Exists(directoryPath))
            {
                return files;
            }

            var extensions = new List<string>();
            if (jpegEnabled) extensions.AddRange(JpegExtensions);
            if (gifEnabled) extensions.AddRange(GifExtensions);
            if (pngEnabled) extensions.AddRange(PngExtensions);
            if (bmpEnabled) extensions.AddRange(BmpExtensions);
            if (tiffEnabled) extensions.AddRange(TiffExtensions);
            if (icoEnabled) extensions.AddRange(IcoExtensions);

            var searchOption = includeSubfolders ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;

            try
            {
                var allFiles = Directory.GetFiles(directoryPath, "*.*", searchOption);
                files.AddRange(allFiles.Where(f => extensions.Contains(Path.GetExtension(f).ToLower())));
            }
            catch (UnauthorizedAccessException)
            {
                // Skip directories we don't have access to
            }
            catch (Exception)
            {
                // Handle other exceptions gracefully
            }

            return files;
        }

        public List<string> GetDirectories(List<string> paths, bool includeSubfolders)
        {
            var directories = new List<string>();

            foreach (var path in paths)
            {
                if (Directory.Exists(path))
                {
                    directories.Add(path);

                    if (includeSubfolders)
                    {
                        try
                        {
                            directories.AddRange(Directory.GetDirectories(path, "*", SearchOption.AllDirectories));
                        }
                        catch (UnauthorizedAccessException)
                        {
                            // Skip directories we don't have access to
                        }
                    }
                }
            }

            return directories.Distinct().ToList();
        }
    }
}
