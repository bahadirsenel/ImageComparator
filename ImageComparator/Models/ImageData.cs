using System.Drawing;

namespace ImageComparator.Models
{
    /// <summary>
    /// Represents metadata about an image file.
    /// </summary>
    public class ImageData
    {
        public string FilePath { get; set; }
        public Size Resolution { get; set; }
        public ImageOrientation Orientation { get; set; }
        public int PHash { get; set; }
        public int HdHash { get; set; }
        public int VdHash { get; set; }
        public int AHash { get; set; }
        public string Sha256 { get; set; }
    }

    /// <summary>
    /// Represents the orientation of an image.
    /// </summary>
    public enum ImageOrientation
    {
        Horizontal,
        Vertical
    }

    /// <summary>
    /// Represents the confidence level of an image match.
    /// </summary>
    public enum ConfidenceLevel
    {
        Low,
        Medium,
        High,
        Duplicate
    }

    /// <summary>
    /// Represents the state of an image in the comparison results.
    /// </summary>
    public enum ImageState
    {
        Normal,
        MarkedForDeletion,
        MarkedAsFalsePositive
    }
}
