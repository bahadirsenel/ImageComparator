namespace ImageComparator.Models
{
    /// <summary>
    /// Represents the result of comparing two images
    /// </summary>
    public class ComparisonResult
    {
        /// <summary>
        /// Index of the first image in the files array
        /// </summary>
        public int ImageIndex1 { get; set; }

        /// <summary>
        /// Index of the second image in the files array
        /// </summary>
        public int ImageIndex2 { get; set; }

        /// <summary>
        /// Confidence level of similarity
        /// </summary>
        public Confidence ConfidenceLevel { get; set; }

        /// <summary>
        /// Perceptual hash hamming distance
        /// </summary>
        public int PerceptualHashDistance { get; set; }

        /// <summary>
        /// Horizontal difference hash hamming distance
        /// </summary>
        public int HorizontalDifferenceHashDistance { get; set; }

        /// <summary>
        /// Vertical difference hash hamming distance
        /// </summary>
        public int VerticalDifferenceHashDistance { get; set; }

        /// <summary>
        /// Average hash hamming distance
        /// </summary>
        public int AverageHashDistance { get; set; }

        /// <summary>
        /// SHA-256 hash of the first image
        /// </summary>
        public string Sha256Hash { get; set; }

        /// <summary>
        /// Whether the images are duplicates (identical SHA-256)
        /// </summary>
        public bool IsDuplicate { get; set; }
    }
}
