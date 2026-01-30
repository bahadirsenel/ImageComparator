using System.Drawing;

namespace ImageComparator.Models
{
    /// <summary>
    /// Serializable wrapper for System.Drawing.Size
    /// </summary>
    public class SerializableSize
    {
        public int Width { get; set; }
        public int Height { get; set; }
        
        public SerializableSize() { }
        
        public SerializableSize(Size size)
        {
            Width = size.Width;
            Height = size.Height;
        }
        
        public Size ToSize() => new Size(Width, Height);
    }
}
