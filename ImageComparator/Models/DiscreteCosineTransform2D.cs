using System;
using System.Drawing;
using System.Drawing.Imaging;

namespace DiscreteCosineTransform
{
    /// <summary>
    /// Provides static methods for 2D Discrete Cosine Transform (DCT) operations.
    /// </summary>
    /// <remarks>
    /// <para>
    /// DCT is used for perceptual image hashing (pHash) to detect similar images.
    /// The transform converts spatial domain image data into frequency domain,
    /// allowing comparison of images based on their low-frequency components.
    /// </para>
    /// <para>
    /// This implementation uses the standard DCT-II formula for forward transform
    /// and DCT-III formula for inverse transform.
    /// </para>
    /// </remarks>
    public static class DiscreteCosineTransform2D
    {
        /// <summary>
        /// Initializes the coefficient matrix for DCT calculations.
        /// </summary>
        /// <param name="dim">The dimension of the square matrix.</param>
        /// <returns>A coefficient matrix used in DCT calculations.</returns>
        private static double[,] InitCoefficientsMatrix(int dim)
        {
            double[,] coefficientsMatrix = new double[dim, dim];

            for (int i = 0; i < dim; i++)
            {
                coefficientsMatrix[i, 0] = Math.Sqrt(2.0) / dim;
                coefficientsMatrix[0, i] = Math.Sqrt(2.0) / dim;
            }

            coefficientsMatrix[0, 0] = 1.0 / dim;

            for (int i = 1; i < dim; i++)
            {
                for (int j = 1; j < dim; j++)
                {
                    coefficientsMatrix[i, j] = 2.0 / dim;
                }
            }
            return coefficientsMatrix;
        }

        /// <summary>
        /// Checks if a matrix is square.
        /// </summary>
        /// <typeparam name="T">The type of elements in the matrix.</typeparam>
        /// <param name="matrix">The matrix to check.</param>
        /// <returns><c>true</c> if the matrix is square; otherwise, <c>false</c>.</returns>
        private static bool IsQuadricMatrix<T>(T[,] matrix)
        {
            int columnsCount = matrix.GetLength(0);
            int rowsCount = matrix.GetLength(1);
            return (columnsCount == rowsCount);
        }

        /// <summary>
        /// Performs forward 2D Discrete Cosine Transform on the input matrix.
        /// </summary>
        /// <param name="input">The input matrix to transform. Must be square.</param>
        /// <returns>The DCT coefficients matrix.</returns>
        /// <exception cref="ArgumentException">
        /// Thrown when the input matrix is not square.
        /// </exception>
        /// <remarks>
        /// Converts spatial domain data to frequency domain.
        /// Used for computing perceptual hashes (pHash) in image comparison.
        /// </remarks>
        public static double[,] ForwardDCT(double[,] input)
        {
            if (IsQuadricMatrix(input) == false)
            {
                throw new ArgumentException("Matrix must be quadric");
            }

            int N = input.GetLength(0);
            double sqrtOfLength = Math.Sqrt(input.Length);
            double[,] coefficientsMatrix = InitCoefficientsMatrix(N);
            double[,] output = new double[N, N];

            for (int u = 0; u <= N - 1; u++)
            {
                for (int v = 0; v <= N - 1; v++)
                {
                    double sum = 0.0;

                    for (int x = 0; x <= N - 1; x++)
                    {
                        for (int y = 0; y <= N - 1; y++)
                        {
                            sum += input[x, y] * Math.Cos(((2.0 * x + 1.0) / (2.0 * N)) * u * Math.PI) * Math.Cos(((2.0 * y + 1.0) / (2.0 * N)) * v * Math.PI);
                        }
                    }
                    sum *= coefficientsMatrix[u, v];
                    output[u, v] = Math.Round(sum);
                }
            }
            return output;
        }

        /// <summary>
        /// Performs inverse 2D Discrete Cosine Transform on the input matrix.
        /// </summary>
        /// <param name="input">The DCT coefficients matrix to transform. Must be square.</param>
        /// <returns>The reconstructed spatial domain matrix.</returns>
        /// <exception cref="ArgumentException">
        /// Thrown when the input matrix is not square.
        /// </exception>
        /// <remarks>
        /// Converts frequency domain data back to spatial domain.
        /// </remarks>
        public static double[,] InverseDCT(double[,] input)
        {
            if (IsQuadricMatrix(input) == false)
            {
                throw new ArgumentException("Matrix must be quadric");
            }

            int N = input.GetLength(0);
            double sqrtOfLength = Math.Sqrt(input.Length);
            double[,] coefficientsMatrix = InitCoefficientsMatrix(N);
            double[,] output = new double[N, N];

            for (int x = 0; x <= N - 1; x++)
            {
                for (int y = 0; y <= N - 1; y++)
                {
                    double sum = 0.0;

                    for (int u = 0; u <= N - 1; u++)
                    {
                        for (int v = 0; v <= N - 1; v++)
                        {
                            sum += coefficientsMatrix[u, v] * input[u, v] * Math.Cos(((2.0 * x + 1.0) / (2.0 * N)) * u * Math.PI) * Math.Cos(((2.0 * y + 1.0) / (2.0 * N)) * v * Math.PI);
                        }
                    };
                    output[x, y] = Math.Round(sum);
                }
            }
            return output;
        }
    }

    /// <summary>
    /// Provides fast 2D Discrete Cosine Transform implementation for image processing.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This class performs DCT operations on images for perceptual hashing.
    /// It uses matrix multiplication with precomputed DCT kernels for improved performance.
    /// </para>
    /// <para>
    /// Typical workflow:
    /// <list type="number">
    /// <item>Create instance from Bitmap or pixel data</item>
    /// <item>Call <see cref="FastDCT"/> to compute DCT coefficients</item>
    /// <item>Use low-frequency coefficients for perceptual hashing</item>
    /// <item>Optionally call <see cref="FastInverseDCT"/> to reconstruct image</item>
    /// </list>
    /// </para>
    /// </remarks>
    public class FastDCT2D
    {
        int Width, Height, Order;
        
        /// <summary>
        /// Gets or sets the grayscale image data as integer array.
        /// </summary>
        public int[,] GreyImage;
        
        /// <summary>
        /// Gets or sets the input matrix, DCT coefficients, inverse DCT coefficients, and DCT kernel.
        /// </summary>
        public double[,] Input, DCTCoefficients, IDTCoefficients, DCTkernel;
        
        /// <summary>
        /// Gets or sets the original bitmap, DCT visualization, and reconstructed image.
        /// </summary>
        public Bitmap Obj, DCTMap, IDCTImage;

        /// <summary>
        /// Initializes a new instance of the <see cref="FastDCT2D"/> class from a bitmap.
        /// </summary>
        /// <param name="Input">The input bitmap to process.</param>
        /// <param name="DCTOrder">The order (dimension) of the DCT matrix.</param>
        /// <remarks>
        /// Converts the bitmap to grayscale and prepares it for DCT processing.
        /// </remarks>
        public FastDCT2D(Bitmap Input, int DCTOrder)
        {
            Obj = Input;
            Width = Input.Width;
            Height = Input.Height;
            Order = DCTOrder;
            ReadImage();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FastDCT2D"/> class from integer pixel data.
        /// </summary>
        /// <param name="InputImageData">The input grayscale image data as 2D integer array.</param>
        /// <param name="order">The order (dimension) of the DCT matrix.</param>
        public FastDCT2D(int[,] InputImageData, int order)
        {
            int i, j;
            GreyImage = InputImageData;
            Width = InputImageData.GetLength(0);
            Height = InputImageData.GetLength(1);

            for (i = 0; i <= Width - 1; i++)
            {
                for (j = 0; j <= Height - 1; j++)
                {
                    Input[i, j] = InputImageData[i, j];
                }
            }
            Order = order;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FastDCT2D"/> class from DCT coefficients.
        /// </summary>
        /// <param name="DCTCoeffInput">The DCT coefficients matrix.</param>
        /// <remarks>
        /// Use this constructor when you already have DCT coefficients and want to perform inverse DCT.
        /// </remarks>
        public FastDCT2D(double[,] DCTCoeffInput)
        {
            DCTCoefficients = DCTCoeffInput;
            Width = DCTCoeffInput.GetLength(0);
            Height = DCTCoeffInput.GetLength(1);
        }

        private void ReadImage()
        {
            int i, j;
            GreyImage = new int[Width, Height];
            Input = new double[Width, Height];
            Bitmap image = Obj;
            BitmapData bitmapData1 = image.LockBits(new Rectangle(0, 0, image.Width, image.Height), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);

            unsafe
            {
                byte* imagePointer1 = (byte*)bitmapData1.Scan0;

                for (i = 0; i < bitmapData1.Height; i++)
                {
                    for (j = 0; j < bitmapData1.Width; j++)
                    {
                        GreyImage[j, i] = (int)((imagePointer1[0] + imagePointer1[1] + imagePointer1[2]) / 3.0);
                        Input[j, i] = GreyImage[j, i];
                        imagePointer1 += 4;
                    }
                    imagePointer1 += bitmapData1.Stride - (bitmapData1.Width * 4);
                }
            }
            image.UnlockBits(bitmapData1);
            return;
        }

        /// <summary>
        /// Converts a double array image to a bitmap for display.
        /// </summary>
        /// <param name="image">The image data as 2D double array.</param>
        /// <returns>A grayscale bitmap representation of the image.</returns>
        public Bitmap Displayimage(double[,] image)
        {
            int i, j;
            Bitmap output = new Bitmap(image.GetLength(0), image.GetLength(1));
            BitmapData bitmapData1 = output.LockBits(new Rectangle(0, 0, image.GetLength(0), image.GetLength(1)), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);

            unsafe
            {
                byte* imagePointer1 = (byte*)bitmapData1.Scan0;

                for (i = 0; i < bitmapData1.Height; i++)
                {
                    for (j = 0; j < bitmapData1.Width; j++)
                    {
                        imagePointer1[0] = (byte)image[j, i];
                        imagePointer1[1] = (byte)image[j, i];
                        imagePointer1[2] = (byte)image[j, i];
                        imagePointer1[3] = 255;
                        imagePointer1 += 4;
                    }
                    imagePointer1 += (bitmapData1.Stride - (bitmapData1.Width * 4));
                }
            }
            output.UnlockBits(bitmapData1);
            return output;
        }

        /// <summary>
        /// Converts an integer array to a color-coded bitmap for DCT visualization.
        /// </summary>
        /// <param name="output">The DCT coefficient data as 2D integer array.</param>
        /// <returns>A color-coded bitmap where colors represent coefficient magnitudes.</returns>
        /// <remarks>
        /// Uses color mapping to visualize DCT coefficients:
        /// <list type="bullet">
        /// <item>Negative values: Green</item>
        /// <item>0-50: Red gradient</item>
        /// <item>50-100: Cyan gradient</item>
        /// <item>100-255: Green gradient</item>
        /// <item>&gt;255: Blue gradient</item>
        /// </list>
        /// </remarks>
        public Bitmap Displaymap(int[,] output)
        {
            int i, j;
            Bitmap image = new Bitmap(output.GetLength(0), output.GetLength(1));
            BitmapData bitmapData1 = image.LockBits(new Rectangle(0, 0, output.GetLength(0), output.GetLength(1)), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);

            unsafe
            {
                byte* imagePointer1 = (byte*)bitmapData1.Scan0;

                for (i = 0; i < bitmapData1.Height; i++)
                {
                    for (j = 0; j < bitmapData1.Width; j++)
                    {
                        if (output[j, i] < 0)
                        {
                            imagePointer1[0] = 0;
                            imagePointer1[1] = 255;
                            imagePointer1[2] = 0;
                        }
                        else if ((output[j, i] >= 0) && (output[j, i] < 50))
                        {
                            imagePointer1[0] = (byte)((output[j, i]) * 4);
                            imagePointer1[1] = 0;
                            imagePointer1[2] = 0;
                        }
                        else if ((output[j, i] >= 50) && (output[j, i] < 100))
                        {
                            imagePointer1[0] = 0;
                            imagePointer1[1] = (byte)(output[j, i] * 2);
                            imagePointer1[2] = (byte)(output[j, i] * 2);
                        }
                        else if ((output[j, i] >= 100) && (output[j, i] < 255))
                        {
                            imagePointer1[0] = 0;
                            imagePointer1[1] = (byte)(output[j, i]);
                            imagePointer1[2] = 0;
                        }
                        else if ((output[j, i] > 255))
                        {
                            imagePointer1[0] = 0;
                            imagePointer1[1] = 0;
                            imagePointer1[2] = (byte)((output[j, i]) * 0.7);
                        }
                        imagePointer1[3] = 255;
                        imagePointer1 += 4;
                    }
                    imagePointer1 += (bitmapData1.Stride - (bitmapData1.Width * 4));
                }
            }
            image.UnlockBits(bitmapData1);
            return image;
        }

        /// <summary>
        /// Performs fast 2D DCT using matrix multiplication.
        /// </summary>
        /// <returns>Integer array representation of DCT coefficients for visualization.</returns>
        /// <remarks>
        /// <para>
        /// Uses the formula: DCT = K * I * K^T, where:
        /// <list type="bullet">
        /// <item>K is the DCT kernel matrix</item>
        /// <item>I is the input image matrix</item>
        /// <item>K^T is the transpose of the DCT kernel</item>
        /// </list>
        /// </para>
        /// <para>
        /// This method is significantly faster than the standard DCT-II formula
        /// for large matrices due to optimized matrix operations.
        /// </para>
        /// </remarks>
        public int[,] FastDCT()
        {
            double[,] temp = new double[Width, Height];
            DCTCoefficients = new double[Width, Height];
            DCTkernel = new double[Width, Height];
            DCTkernel = GenerateDCTmatrix(Order);
            temp = multiply(DCTkernel, Input);
            DCTCoefficients = multiply(temp, Transpose(DCTkernel));
            return DCTPlotGenerate();
        }

        /// <summary>
        /// Performs fast inverse 2D DCT using matrix multiplication.
        /// </summary>
        /// <remarks>
        /// Uses the formula: Image = K^T * DCT * K, where:
        /// <list type="bullet">
        /// <item>K^T is the transpose of the DCT kernel matrix</item>
        /// <item>DCT is the DCT coefficients matrix</item>
        /// <item>K is the DCT kernel matrix</item>
        /// </list>
        /// The reconstructed image is stored in <see cref="IDCTImage"/>.
        /// </remarks>
        public void FastInverseDCT()
        {
            double[,] temp = new double[Width, Height];
            IDTCoefficients = new double[Width, Height];
            DCTkernel = new double[Width, Height];
            DCTkernel = Transpose(GenerateDCTmatrix(Order));
            temp = multiply(DCTkernel, DCTCoefficients);
            IDTCoefficients = multiply(temp, Transpose(DCTkernel));
            IDCTImage = Displayimage(IDTCoefficients);
            return;
        }

        /// <summary>
        /// Generates the DCT kernel matrix for the specified order.
        /// </summary>
        /// <param name="order">The dimension (order) of the DCT matrix.</param>
        /// <returns>The DCT kernel matrix used for transform computations.</returns>
        /// <remarks>
        /// The DCT kernel is precomputed to improve performance of multiple DCT operations.
        /// </remarks>
        public double[,] GenerateDCTmatrix(int order)
        {
            int i, j, N = order;
            double alpha, denominator;
            double[,] DCTCoeff = new double[N, N];

            for (j = 0; j <= N - 1; j++)
            {
                DCTCoeff[0, j] = Math.Sqrt(1 / (double)N);
            }

            alpha = Math.Sqrt(2 / (double)N);
            denominator = (double)2 * N;

            for (j = 0; j <= N - 1; j++)
            {
                for (i = 1; i <= N - 1; i++)
                {
                    DCTCoeff[i, j] = alpha * Math.Cos(((2 * j + 1) * i * 3.14159) / denominator);
                }
            }
            return (DCTCoeff);
        }

        private double[,] multiply(double[,] m1, double[,] m2)
        {
            int row, col, i, j, k;
            double sum;
            row = col = m1.GetLength(0);
            double[,] m3 = new double[row, col];

            for (i = 0; i <= row - 1; i++)
            {
                for (j = 0; j <= col - 1; j++)
                {
                    sum = 0;

                    for (k = 0; k <= row - 1; k++)
                    {
                        sum = sum + m1[i, k] * m2[k, j];
                    }
                    m3[i, j] = sum;
                }
            }
            return m3;
        }

        private double[,] Transpose(double[,] m)
        {
            int i, j, Width, Height;
            Width = m.GetLength(0);
            Height = m.GetLength(1);

            double[,] mt = new double[m.GetLength(0), m.GetLength(1)];

            for (i = 0; i <= Height - 1; i++)
            {
                for (j = 0; j <= Width - 1; j++)
                {
                    mt[j, i] = m[i, j];
                }
            }
            return (mt);
        }

        private int[,] DCTPlotGenerate()
        {
            int i, j;
            int[,] temp = new int[Width, Height];
            double[,] DCTLog = new double[Width, Height];

            for (i = 0; i <= Width - 1; i++)
            {
                for (j = 0; j <= Height - 1; j++)
                {
                    DCTLog[i, j] = Math.Log(1 + Math.Abs((int)DCTCoefficients[i, j]));
                }
            }

            double min, max;
            min = max = DCTLog[1, 1];

            for (i = 1; i <= Width - 1; i++)
            {
                for (j = 1; j <= Height - 1; j++)
                {
                    if (DCTLog[i, j] > max)
                    {
                        max = DCTLog[i, j];
                    }

                    if (DCTLog[i, j] < min)
                    {
                        min = DCTLog[i, j];
                    }
                }
            }

            for (i = 0; i <= Width - 1; i++)
            {
                for (j = 0; j <= Height - 1; j++)
                {
                    temp[i, j] = (int)(((float)(DCTLog[i, j] - min) / (float)(max - min)) * 750);
                }
            }
            return temp;
        }
    }
}