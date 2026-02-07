using System.Drawing;

namespace ImageComparator.Models
{
    /// <summary>
    /// Represents all hash data and metadata for a single image
    /// </summary>
    public class ImageHashData
    {
        /// <summary>
        /// Full path to the image file
        /// </summary>
        public string FilePath { get; set; }

        /// <summary>
        /// Image resolution (width x height)
        /// </summary>
        public Size Resolution { get; set; }

        /// <summary>
        /// Image orientation (horizontal or vertical)
        /// </summary>
        public Orientation Orientation { get; set; }

        /// <summary>
        /// SHA-256 hash for exact duplicate detection
        /// </summary>
        public string Sha256Hash { get; set; }

        /// <summary>
        /// Perceptual hash (DCT-based 64-bit hash, robust to minor modifications)
        /// Array of 64 integers (0 or 1)
        /// </summary>
        public int[] PerceptualHash { get; set; }

        /// <summary>
        /// Horizontal difference hash (72-bit, compares right neighbors)
        /// Array of 72 integers (0 or 1)
        /// </summary>
        public int[] HorizontalDifferenceHash { get; set; }

        /// <summary>
        /// Vertical difference hash (72-bit, compares bottom neighbors)
        /// Array of 72 integers (0 or 1)
        /// </summary>
        public int[] VerticalDifferenceHash { get; set; }

        /// <summary>
        /// Average hash (64-bit, fast layout comparison)
        /// Array of 64 integers (0 or 1)
        /// </summary>
        public int[] AverageHash { get; set; }

        public ImageHashData()
        {
            PerceptualHash = new int[64];
            HorizontalDifferenceHash = new int[72];
            VerticalDifferenceHash = new int[72];
            AverageHash = new int[64];
        }
    }
}
