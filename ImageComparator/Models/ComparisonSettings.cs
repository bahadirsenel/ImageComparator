namespace ImageComparator.Models
{
    /// <summary>
    /// Represents comparison settings and configuration.
    /// </summary>
    public class ComparisonSettings
    {
        public bool SkipFilesWithDifferentOrientation { get; set; } = true;
        public bool DuplicatesOnly { get; set; } = false;
        public bool IncludeSubfolders { get; set; }
        public bool SendToRecycleBin { get; set; }

        // File type filters
        public bool JpegEnabled { get; set; } = true;
        public bool GifEnabled { get; set; } = true;
        public bool PngEnabled { get; set; } = true;
        public bool BmpEnabled { get; set; } = true;
        public bool TiffEnabled { get; set; } = true;
        public bool IcoEnabled { get; set; } = true;
    }
}
