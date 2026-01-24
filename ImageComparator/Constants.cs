namespace ImageComparator
{
    /// <summary>
    /// Application-wide constants.
    /// </summary>
    public static class Constants
    {
        /// <summary>
        /// File extension for saved results.
        /// </summary>
        public const string ResultsFileExtension = "mff";

        /// <summary>
        /// Filter for file dialogs.
        /// </summary>
        public const string ResultsFileFilter = "*.mff|*.mff";

        /// <summary>
        /// Saved state file name.
        /// </summary>
        public const string SavedStateFileName = "Image Comparator.imc";

        /// <summary>
        /// Temporary results file name.
        /// </summary>
        public const string TempResultsFileName = "Results.imc";

        /// <summary>
        /// Default image hash size for perceptual hash calculations.
        /// </summary>
        public const int HashSize = 8;

        /// <summary>
        /// Image resize dimension for hash calculations.
        /// </summary>
        public const int HashResizeDimension = 32;

        /// <summary>
        /// Smaller image resize dimension for difference hash.
        /// </summary>
        public const int DiffHashResizeDimension = 9;

        /// <summary>
        /// Language codes.
        /// </summary>
        public static class LanguageCodes
        {
            public const string English = "en-US";
            public const string Turkish = "tr-TR";
        }

        /// <summary>
        /// Confidence thresholds for image similarity.
        /// </summary>
        public static class ConfidenceThresholds
        {
            /// <summary>
            /// Maximum Hamming distance for duplicate images.
            /// </summary>
            public const int Duplicate = 0;

            /// <summary>
            /// Maximum Hamming distance for high confidence similarity.
            /// </summary>
            public const int High = 5;

            /// <summary>
            /// Maximum Hamming distance for medium confidence similarity.
            /// </summary>
            public const int Medium = 10;

            /// <summary>
            /// Maximum Hamming distance for low confidence similarity.
            /// </summary>
            public const int Low = 15;
        }

        /// <summary>
        /// Warning messages.
        /// </summary>
        public static class Warnings
        {
            public const string ScanningSystemDirectory = "Are you sure?";
        }
    }
}
