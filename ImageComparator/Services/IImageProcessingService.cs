using ImageComparator.Models;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;

namespace ImageComparator.Services
{
    /// <summary>
    /// Service for processing images and calculating perceptual hashes
    /// </summary>
    public interface IImageProcessingService
    {
        /// <summary>
        /// Process a single image and calculate all hashes
        /// </summary>
        /// <param name="filePath">Path to the image file</param>
        /// <param name="calculateAllHashes">If false, only calculates SHA-256, resolution, and orientation (for duplicates-only mode)</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Image hash data</returns>
        ImageHashData ProcessImage(string filePath, bool calculateAllHashes, CancellationToken cancellationToken);

        /// <summary>
        /// Resize an image to specified dimensions
        /// </summary>
        /// <param name="image">Source image</param>
        /// <param name="width">Target width</param>
        /// <param name="height">Target height</param>
        /// <returns>Resized bitmap</returns>
        Bitmap ResizeImage(Image image, int width, int height);

        /// <summary>
        /// Convert an image to grayscale
        /// </summary>
        /// <param name="inputImage">Source image</param>
        /// <returns>Grayscale bitmap</returns>
        Bitmap ConvertToGrayscale(Bitmap inputImage);
    }
}
