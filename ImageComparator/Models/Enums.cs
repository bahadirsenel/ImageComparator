namespace ImageComparator.Models
{
    /// <summary>
    /// Image orientation
    /// </summary>
    public enum Orientation
    {
        Horizontal,
        Vertical
    }

    /// <summary>
    /// Confidence level for image similarity
    /// </summary>
    public enum Confidence
    {
        Low,
        Medium,
        High,
        Duplicate
    }

    /// <summary>
    /// State of a list view item
    /// </summary>
    public enum State
    {
        Normal,
        MarkedForDeletion,
        MarkedAsFalsePositive
    }
}
