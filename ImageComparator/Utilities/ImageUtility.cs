using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

namespace ImageComparator.Utilities
{
    /// <summary>
    /// Utility class for image manipulation operations.
    /// </summary>
    public static class ImageUtility
    {
        /// <summary>
        /// Resizes an image to the specified width and height.
        /// </summary>
        /// <param name="image">The image to resize.</param>
        /// <param name="width">Target width.</param>
        /// <param name="height">Target height.</param>
        /// <returns>Resized image.</returns>
        public static Bitmap ResizeImage(Bitmap image, int width, int height)
        {
            var destRect = new Rectangle(0, 0, width, height);
            var destImage = new Bitmap(width, height);

            destImage.SetResolution(image.HorizontalResolution, image.VerticalResolution);

            using (var graphics = Graphics.FromImage(destImage))
            {
                graphics.CompositingMode = CompositingMode.SourceCopy;
                graphics.CompositingQuality = CompositingQuality.HighQuality;
                graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                graphics.SmoothingMode = SmoothingMode.HighQuality;
                graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;

                using (var wrapMode = new ImageAttributes())
                {
                    wrapMode.SetWrapMode(WrapMode.TileFlipXY);
                    graphics.DrawImage(image, destRect, 0, 0, image.Width, image.Height, GraphicsUnit.Pixel, wrapMode);
                }
            }

            return destImage;
        }

        /// <summary>
        /// Converts an image to grayscale.
        /// </summary>
        /// <param name="image">The image to convert.</param>
        /// <returns>Grayscale image.</returns>
        public static Bitmap ConvertToGrayscale(Bitmap image)
        {
            var grayscale = new Bitmap(image.Width, image.Height);

            for (int i = 0; i < image.Width; i++)
            {
                for (int j = 0; j < image.Height; j++)
                {
                    Color color = image.GetPixel(i, j);
                    int gray = (int)(color.R * 0.3 + color.G * 0.59 + color.B * 0.11);
                    grayscale.SetPixel(i, j, Color.FromArgb(gray, gray, gray));
                }
            }

            return grayscale;
        }

        /// <summary>
        /// Gets the orientation of an image based on its dimensions.
        /// </summary>
        /// <param name="width">Image width.</param>
        /// <param name="height">Image height.</param>
        /// <returns>Image orientation.</returns>
        public static Models.ImageOrientation GetOrientation(int width, int height)
        {
            return width > height 
                ? Models.ImageOrientation.Horizontal 
                : Models.ImageOrientation.Vertical;
        }
    }
}
