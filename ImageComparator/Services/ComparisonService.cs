using ImageComparator.Models;
using System;

namespace ImageComparator.Services
{
    /// <summary>
    /// Service for comparing images and detecting similarities
    /// </summary>
    public class ComparisonService : IComparisonService
    {
        // Hash comparison thresholds
        private const int EXACT_DUPLICATE_THRESHOLD = 1;
        private const int PHASH_HIGH_CONFIDENCE_THRESHOLD = 9;
        private const int PHASH_MEDIUM_CONFIDENCE_THRESHOLD = 12;
        private const int PHASH_LOW_CONFIDENCE_THRESHOLD = 21;
        private const int HDHASH_HIGH_CONFIDENCE_THRESHOLD = 10;
        private const int HDHASH_MEDIUM_CONFIDENCE_THRESHOLD = 13;
        private const int HDHASH_LOW_CONFIDENCE_THRESHOLD = 18;
        private const int VDHASH_HIGH_CONFIDENCE_THRESHOLD = 10;
        private const int VDHASH_MEDIUM_CONFIDENCE_THRESHOLD = 13;
        private const int VDHASH_LOW_CONFIDENCE_THRESHOLD = 18;
        private const int AHASH_HIGH_CONFIDENCE_THRESHOLD = 9;
        private const int AHASH_MEDIUM_CONFIDENCE_THRESHOLD = 12;

        /// <summary>
        /// Find if two images are similar based on their hash data
        /// </summary>
        public ComparisonResult FindSimilarity(ImageHashData image1, ImageHashData image2, bool duplicatesOnly, bool skipDifferentOrientations)
        {
            // Check if both images are valid
            if (image1.PerceptualHash[0] == -1 || image2.PerceptualHash[0] == -1)
            {
                return null;
            }

            // Skip comparison if orientations differ and flag is set
            if (skipDifferentOrientations && image1.Orientation != image2.Orientation)
            {
                return null;
            }

            if (duplicatesOnly)
            {
                // For duplicates-only mode, only check SHA-256
                if (image1.Sha256Hash != image2.Sha256Hash)
                {
                    return null;
                }

                return new ComparisonResult
                {
                    ConfidenceLevel = Confidence.Duplicate,
                    Sha256Hash = image1.Sha256Hash,
                    IsDuplicate = true,
                    PerceptualHashDistance = 0,
                    HorizontalDifferenceHashDistance = 0,
                    VerticalDifferenceHashDistance = 0,
                    AverageHashDistance = 0
                };
            }
            else
            {
                // Calculate hamming distances for all hash types
                int pHashDistance = CalculateHammingDistance(image1.PerceptualHash, image2.PerceptualHash, 1, 64);
                int hdHashDistance = CalculateHammingDistance(image1.HorizontalDifferenceHash, image2.HorizontalDifferenceHash, 0, 72);
                int vdHashDistance = CalculateHammingDistance(image1.VerticalDifferenceHash, image2.VerticalDifferenceHash, 0, 72);
                int aHashDistance = CalculateHammingDistance(image1.AverageHash, image2.AverageHash, 0, 64);

                // Determine confidence level based on thresholds
                Confidence? confidenceLevel = null;

                // Check for exact duplicate
                if (image1.Sha256Hash == image2.Sha256Hash && 
                    pHashDistance < EXACT_DUPLICATE_THRESHOLD && 
                    hdHashDistance < EXACT_DUPLICATE_THRESHOLD && 
                    vdHashDistance < EXACT_DUPLICATE_THRESHOLD && 
                    aHashDistance < EXACT_DUPLICATE_THRESHOLD)
                {
                    confidenceLevel = Confidence.Duplicate;
                }
                // Check for high confidence match
                else if (pHashDistance < PHASH_HIGH_CONFIDENCE_THRESHOLD && 
                         hdHashDistance < HDHASH_HIGH_CONFIDENCE_THRESHOLD && 
                         vdHashDistance < VDHASH_HIGH_CONFIDENCE_THRESHOLD && 
                         aHashDistance < AHASH_HIGH_CONFIDENCE_THRESHOLD)
                {
                    confidenceLevel = Confidence.High;
                }
                // Check for medium confidence match
                else if ((pHashDistance < PHASH_HIGH_CONFIDENCE_THRESHOLD && 
                          hdHashDistance < HDHASH_MEDIUM_CONFIDENCE_THRESHOLD && 
                          vdHashDistance < VDHASH_MEDIUM_CONFIDENCE_THRESHOLD && 
                          aHashDistance < AHASH_MEDIUM_CONFIDENCE_THRESHOLD) || 
                         (hdHashDistance < HDHASH_HIGH_CONFIDENCE_THRESHOLD && 
                          pHashDistance < PHASH_MEDIUM_CONFIDENCE_THRESHOLD && 
                          vdHashDistance < VDHASH_MEDIUM_CONFIDENCE_THRESHOLD && 
                          aHashDistance < AHASH_MEDIUM_CONFIDENCE_THRESHOLD) || 
                         (vdHashDistance < VDHASH_HIGH_CONFIDENCE_THRESHOLD && 
                          pHashDistance < PHASH_MEDIUM_CONFIDENCE_THRESHOLD && 
                          hdHashDistance < HDHASH_MEDIUM_CONFIDENCE_THRESHOLD && 
                          aHashDistance < AHASH_MEDIUM_CONFIDENCE_THRESHOLD) || 
                         (aHashDistance < AHASH_HIGH_CONFIDENCE_THRESHOLD && 
                          pHashDistance < PHASH_MEDIUM_CONFIDENCE_THRESHOLD && 
                          hdHashDistance < HDHASH_MEDIUM_CONFIDENCE_THRESHOLD && 
                          vdHashDistance < VDHASH_MEDIUM_CONFIDENCE_THRESHOLD))
                {
                    confidenceLevel = Confidence.Medium;
                }
                // Check for low confidence match
                else if ((pHashDistance < PHASH_HIGH_CONFIDENCE_THRESHOLD || 
                          hdHashDistance < HDHASH_HIGH_CONFIDENCE_THRESHOLD || 
                          vdHashDistance < VDHASH_HIGH_CONFIDENCE_THRESHOLD) && 
                         aHashDistance < AHASH_HIGH_CONFIDENCE_THRESHOLD && 
                         pHashDistance < PHASH_LOW_CONFIDENCE_THRESHOLD && 
                         hdHashDistance < HDHASH_LOW_CONFIDENCE_THRESHOLD && 
                         vdHashDistance < VDHASH_LOW_CONFIDENCE_THRESHOLD)
                {
                    confidenceLevel = Confidence.Low;
                }

                // Return result if a match was found
                if (confidenceLevel.HasValue)
                {
                    return new ComparisonResult
                    {
                        ConfidenceLevel = confidenceLevel.Value,
                        Sha256Hash = image1.Sha256Hash,
                        IsDuplicate = confidenceLevel == Confidence.Duplicate,
                        PerceptualHashDistance = pHashDistance,
                        HorizontalDifferenceHashDistance = hdHashDistance,
                        VerticalDifferenceHashDistance = vdHashDistance,
                        AverageHashDistance = aHashDistance
                    };
                }

                return null;
            }
        }

        /// <summary>
        /// Calculate hamming distance between two hash arrays
        /// </summary>
        public int CalculateHammingDistance(int[] hash1, int[] hash2)
        {
            if (hash1 == null || hash2 == null || hash1.Length != hash2.Length)
            {
                throw new ArgumentException("Hash arrays must be non-null and of equal length");
            }

            int distance = 0;
            for (int i = 0; i < hash1.Length; i++)
            {
                if (hash1[i] != hash2[i])
                {
                    distance++;
                }
            }

            return distance;
        }

        /// <summary>
        /// Calculate hamming distance with custom start index and length
        /// </summary>
        private int CalculateHammingDistance(int[] hash1, int[] hash2, int startIndex, int length)
        {
            int distance = 0;
            for (int i = startIndex; i < length; i++)
            {
                if (hash1[i] != hash2[i])
                {
                    distance++;
                }
            }

            return distance;
        }
    }
}
