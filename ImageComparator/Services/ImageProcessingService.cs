using Common.Helpers;
using DiscreteCosineTransform;
using ImageComparator.Models;
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Security.Cryptography;
using System.Threading;

namespace ImageComparator.Services
{
    /// <summary>
    /// Service for processing images and calculating perceptual hashes
    /// </summary>
    public class ImageProcessingService : IImageProcessingService
    {
        // Hash calculation constants
        private const int PHASH_RESIZE_DIMENSION = 32;
        private const int DHASH_RESIZE_DIMENSION = 9;
        private const int AHASH_RESIZE_DIMENSION = 8;

        /// <summary>
        /// Process a single image and calculate all hashes
        /// </summary>
        public ImageHashData ProcessImage(string filePath, bool calculateAllHashes, CancellationToken cancellationToken)
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                var hashData = new ImageHashData
                {
                    FilePath = filePath
                };

                using (var sha = SHA256.Create())
                using (var image = new Bitmap(filePath))
                {
                    // Get resolution and orientation
                    hashData.Resolution = image.Size;
                    hashData.Orientation = image.Width > image.Height 
                        ? Orientation.Horizontal 
                        : Orientation.Vertical;

                    if (calculateAllHashes)
                    {
                        // Calculate perceptual hash (pHash)
                        using (var resized32 = ResizeImage(image, PHASH_RESIZE_DIMENSION, PHASH_RESIZE_DIMENSION))
                        {
                            var fastDCT2D = new FastDCT2D(resized32, 32);
                            var result = fastDCT2D.FastDCT();

                            // pHash (Perceptual Hash) Calculation
                            // Applies Discrete Cosine Transform (DCT) to capture frequency patterns
                            // Compares each DCT coefficient against the average (excluding DC component)
                            // Creates 64-bit hash representing low-frequency image structure
                            // Result: robust to minor image modifications (scaling, compression, brightness)
                            double average = 0;
                            for (int j = 0; j < 8; j++)
                            {
                                for (int k = 0; k < 8; k++)
                                {
                                    average += result[j, k];
                                }
                            }

                            average -= result[0, 0];  // Exclude DC component (overall brightness)
                            average /= 63;

                            for (int j = 0; j < 8; j++)
                            {
                                for (int k = 0; k < 8; k++)
                                {
                                    hashData.PerceptualHash[j * 8 + k] = result[j, k] < average ? 0 : 1;
                                }
                            }

                            // Calculate difference hashes and average hash
                            using (var resized9 = ResizeImage(resized32, DHASH_RESIZE_DIMENSION, DHASH_RESIZE_DIMENSION))
                            using (var grayscale = ConvertToGrayscale(resized9))
                            {
                                // hdHash (Horizontal Difference Hash) Calculation
                                // Compares each pixel with its RIGHT neighbor: GetPixel(j, k) vs GetPixel(j+1, k)
                                // Creates 8x9=72 bits by comparing columns (j to j+1) across 9 rows (k)
                                // Result: horizontal gradient detection
                                for (int j = 0; j < 8; j++)
                                {
                                    for (int k = 0; k < 9; k++)
                                    {
                                        hashData.HorizontalDifferenceHash[j * 8 + k] = 
                                            grayscale.GetPixel(j, k).R < grayscale.GetPixel(j + 1, k).R ? 0 : 1;
                                    }
                                }

                                // vdHash (Vertical Difference Hash) Calculation
                                // Compares each pixel with its BOTTOM neighbor: GetPixel(j, k) vs GetPixel(j, k+1)
                                // Creates 9x8=72 bits by comparing rows (k to k+1) across 9 columns (j)
                                // Result: vertical gradient detection
                                for (int j = 0; j < 9; j++)
                                {
                                    for (int k = 0; k < 8; k++)
                                    {
                                        hashData.VerticalDifferenceHash[j * 8 + k] = 
                                            grayscale.GetPixel(j, k).R < grayscale.GetPixel(j, k + 1).R ? 0 : 1;
                                    }
                                }

                                // aHash (Average Hash) Calculation
                                // Compares each pixel's brightness against the average brightness of all pixels
                                // Creates 64-bit hash where each bit represents above/below average
                                // Result: fast computation, good for finding similar layouts and structures
                                using (var resized8 = ResizeImage(grayscale, AHASH_RESIZE_DIMENSION, AHASH_RESIZE_DIMENSION))
                                {
                                    average = 0;

                                    for (int j = 0; j < 8; j++)
                                    {
                                        for (int k = 0; k < 8; k++)
                                        {
                                            average += resized8.GetPixel(j, k).R;
                                        }
                                    }

                                    average /= 64;  // Calculate mean brightness

                                    for (int j = 0; j < 8; j++)
                                    {
                                        for (int k = 0; k < 8; k++)
                                        {
                                            hashData.AverageHash[j * 8 + k] = 
                                                resized8.GetPixel(j, k).R < average ? 0 : 1;
                                        }
                                    }
                                }
                            }
                        }
                    }

                    // Calculate SHA-256 hash
                    using (var stream = File.OpenRead(filePath))
                    {
                        byte[] hash = sha.ComputeHash(stream);
                        hashData.Sha256Hash = BitConverter.ToString(hash).Replace("-", string.Empty);
                    }
                }

                return hashData;
            }
            catch (ArgumentException)
            {
                // Invalid image format - return null hash data
                ErrorLogger.LogWarning("ImageProcessingService", $"Invalid image format: {Path.GetFileName(filePath)}");
                return CreateInvalidHashData(filePath);
            }
            catch (OperationCanceledException)
            {
                // Expected when cancelled
                throw;
            }
            catch (OutOfMemoryException)
            {
                throw;
            }
            catch (Exception ex)
            {
                ErrorLogger.LogError($"ImageProcessingService.ProcessImage - {Path.GetFileName(filePath)}", ex);
                return CreateInvalidHashData(filePath);
            }
        }

        /// <summary>
        /// Create hash data for an invalid image
        /// </summary>
        private ImageHashData CreateInvalidHashData(string filePath)
        {
            var hashData = new ImageHashData
            {
                FilePath = filePath
            };
            // Mark as invalid by setting first element to -1
            hashData.PerceptualHash[0] = -1;
            return hashData;
        }

        /// <summary>
        /// Resize an image to specified dimensions
        /// </summary>
        public Bitmap ResizeImage(Image image, int width, int height)
        {
            Rectangle destRect = new Rectangle(0, 0, width, height);
            Bitmap destImage = new Bitmap(width, height);

            try
            {
                destImage.SetResolution(image.HorizontalResolution, image.VerticalResolution);

                using (Graphics graphics = Graphics.FromImage(destImage))
                using (ImageAttributes wrapMode = new ImageAttributes())
                {
                    graphics.CompositingMode = CompositingMode.SourceCopy;
                    graphics.CompositingQuality = CompositingQuality.HighQuality;
                    graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                    graphics.SmoothingMode = SmoothingMode.HighQuality;
                    graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;

                    wrapMode.SetWrapMode(WrapMode.TileFlipXY);
                    graphics.DrawImage(image, destRect, 0, 0, image.Width, image.Height, GraphicsUnit.Pixel, wrapMode);
                }

                return destImage;
            }
            catch
            {
                destImage?.Dispose();
                throw;
            }
        }

        /// <summary>
        /// Convert an image to grayscale
        /// </summary>
        public Bitmap ConvertToGrayscale(Bitmap inputImage)
        {
            Bitmap cloneImage = (Bitmap)inputImage.Clone();

            try
            {
                using (Graphics graphics = Graphics.FromImage(cloneImage))
                using (ImageAttributes attributes = new ImageAttributes())
                {
                    ColorMatrix colorMatrix = new ColorMatrix(new float[][]{
                        new float[] {0.299f, 0.299f, 0.299f, 0, 0},
                        new float[] {0.587f, 0.587f, 0.587f, 0, 0},
                        new float[] {0.114f, 0.114f, 0.114f, 0, 0},
                        new float[] {     0,      0,      0, 1, 0},
                        new float[] {     0,      0,      0, 0, 0}
                    });
                    attributes.SetColorMatrix(colorMatrix);
                    graphics.DrawImage(cloneImage, new Rectangle(0, 0, cloneImage.Width, cloneImage.Height), 
                        0, 0, cloneImage.Width, cloneImage.Height, GraphicsUnit.Pixel, attributes);
                }

                return cloneImage;
            }
            catch
            {
                cloneImage?.Dispose();
                throw;
            }
        }
    }
}
