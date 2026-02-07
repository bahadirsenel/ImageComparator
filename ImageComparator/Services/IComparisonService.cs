using ImageComparator.Models;
using System.Collections.Generic;

namespace ImageComparator.Services
{
    /// <summary>
    /// Service for comparing images and detecting similarities
    /// </summary>
    public interface IComparisonService
    {
        /// <summary>
        /// Find if two images are similar based on their hash data
        /// </summary>
        /// <param name="image1">First image hash data</param>
        /// <param name="image2">Second image hash data</param>
        /// <param name="duplicatesOnly">If true, only consider exact duplicates (SHA-256 match)</param>
        /// <param name="skipDifferentOrientations">If true, skip comparison of images with different orientations</param>
        /// <returns>Comparison result if images are similar, null otherwise</returns>
        ComparisonResult FindSimilarity(ImageHashData image1, ImageHashData image2, bool duplicatesOnly, bool skipDifferentOrientations);

        /// <summary>
        /// Calculate hamming distance between two hash arrays
        /// </summary>
        /// <param name="hash1">First hash array</param>
        /// <param name="hash2">Second hash array</param>
        /// <returns>Hamming distance (number of differing bits)</returns>
        int CalculateHammingDistance(int[] hash1, int[] hash2);
    }
}
