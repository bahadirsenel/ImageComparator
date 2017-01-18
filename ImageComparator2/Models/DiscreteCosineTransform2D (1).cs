using System;
using System.Drawing;
using System.Drawing.Imaging;

namespace DiscreteCosineTransform {

    public static class DiscreteCosineTransform2D {

        private static double[,] InitCoefficientsMatrix(int dim) {

            double[,] coefficientsMatrix = new double[dim, dim];

            for (int i = 0; i < dim; i++) {
                coefficientsMatrix[i, 0] = Math.Sqrt(2.0) / dim;
                coefficientsMatrix[0, i] = Math.Sqrt(2.0) / dim;
            }

            coefficientsMatrix[0, 0] = 1.0 / dim;

            for (int i = 1; i < dim; i++) {
                for (int j = 1; j < dim; j++) {
                    coefficientsMatrix[i, j] = 2.0 / dim;
                }
            }
            return coefficientsMatrix;
        }

        private static bool IsQuadricMatrix<T>(T[,] matrix) {

            int columnsCount = matrix.GetLength(0);
            int rowsCount = matrix.GetLength(1);
            return (columnsCount == rowsCount);
        }

        public static double[,] ForwardDCT(double[,] input) {

            double sqrtOfLength = Math.Sqrt(input.Length);

            if (IsQuadricMatrix(input) == false) {
                throw new ArgumentException("Matrix must be quadric");
            }

            int N = input.GetLength(0);

            double[,] coefficientsMatrix = InitCoefficientsMatrix(N);
            double[,] output = new double[N, N];

            for (int u = 0; u <= N - 1; u++) {
                for (int v = 0; v <= N - 1; v++) {

                    double sum = 0.0;

                    for (int x = 0; x <= N - 1; x++) {
                        for (int y = 0; y <= N - 1; y++) {
                            sum += input[x, y] * Math.Cos(((2.0 * x + 1.0) / (2.0 * N)) * u * Math.PI) * Math.Cos(((2.0 * y + 1.0) / (2.0 * N)) * v * Math.PI);
                        }
                    }
                    sum *= coefficientsMatrix[u, v];
                    output[u, v] = Math.Round(sum);
                }
            }
            return output;
        }

        public static double[,] InverseDCT(double[,] input) {

            double sqrtOfLength = Math.Sqrt(input.Length);

            if (IsQuadricMatrix(input) == false) {
                throw new ArgumentException("Matrix must be quadric");
            }

            int N = input.GetLength(0);

            double[,] coefficientsMatrix = InitCoefficientsMatrix(N);
            double[,] output = new double[N, N];

            for (int x = 0; x <= N - 1; x++) {
                for (int y = 0; y <= N - 1; y++) {

                    double sum = 0.0;

                    for (int u = 0; u <= N - 1; u++) {
                        for (int v = 0; v <= N - 1; v++) {
                            sum += coefficientsMatrix[u, v] * input[u, v] * Math.Cos(((2.0 * x + 1.0) / (2.0 * N)) * u * Math.PI) * Math.Cos(((2.0 * y + 1.0) / (2.0 * N)) * v * Math.PI);
                        }
                    };
                    output[x, y] = Math.Round(sum);
                }
            }
            return output;
        }
    }

    public class FastDCT2D {

        public Bitmap Obj;
        public Bitmap DCTMap;
        public Bitmap IDCTImage;

        public int[,] GreyImage;
        public double[,] Input;

        public double[,] DCTCoefficients;
        public double[,] IDTCoefficients;
        private double[,] DCTkernel;

        int Width, Height;
        int Order;

        public FastDCT2D(Bitmap Input, int DCTOrder) {

            Obj = Input;
            Width = Input.Width;
            Height = Input.Height;
            Order = DCTOrder;
            ReadImage();
        }

        public FastDCT2D(int[,] InputImageData, int order) {

            int i, j;
            GreyImage = InputImageData;
            Width = InputImageData.GetLength(0);
            Height = InputImageData.GetLength(1);

            for (i = 0; i <= Width - 1; i++) {
                for (j = 0; j <= Height - 1; j++) {
                    Input[i, j] = InputImageData[i, j];
                }
            }
            Order = order;
        }

        public FastDCT2D(double[,] DCTCoeffInput) {

            DCTCoefficients = DCTCoeffInput;
            Width = DCTCoeffInput.GetLength(0);
            Height = DCTCoeffInput.GetLength(1);
        }

        private void ReadImage() {

            int i, j;
            GreyImage = new int[Width, Height];
            Input = new double[Width, Height];
            Bitmap image = Obj;
            BitmapData bitmapData1 = image.LockBits(new Rectangle(0, 0, image.Width, image.Height), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);

            unsafe {
                byte* imagePointer1 = (byte*) bitmapData1.Scan0;

                for (i = 0; i < bitmapData1.Height; i++) {
                    for (j = 0; j < bitmapData1.Width; j++) {

                        GreyImage[j, i] = (int) ((imagePointer1[0] + imagePointer1[1] + imagePointer1[2]) / 3.0);
                        Input[j, i] = GreyImage[j, i];
                        imagePointer1 += 4;
                    }
                    imagePointer1 += bitmapData1.Stride - (bitmapData1.Width * 4);
                }
            }
            image.UnlockBits(bitmapData1);
            return;
        }

        public Bitmap Displayimage(double[,] image) {

            int i, j;
            Bitmap output = new Bitmap(image.GetLength(0), image.GetLength(1));
            BitmapData bitmapData1 = output.LockBits(new Rectangle(0, 0, image.GetLength(0), image.GetLength(1)), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);

            unsafe {
                byte* imagePointer1 = (byte*) bitmapData1.Scan0;

                for (i = 0; i < bitmapData1.Height; i++) {
                    for (j = 0; j < bitmapData1.Width; j++) {

                        imagePointer1[0] = (byte) image[j, i];
                        imagePointer1[1] = (byte) image[j, i];
                        imagePointer1[2] = (byte) image[j, i];
                        imagePointer1[3] = 255;
                        imagePointer1 += 4;
                    }
                    imagePointer1 += (bitmapData1.Stride - (bitmapData1.Width * 4));
                }
            }
            output.UnlockBits(bitmapData1);
            return output;
        }

        public Bitmap Displaymap(int[,] output) {

            int i, j;
            Bitmap image = new Bitmap(output.GetLength(0), output.GetLength(1));
            BitmapData bitmapData1 = image.LockBits(new Rectangle(0, 0, output.GetLength(0), output.GetLength(1)), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);

            unsafe {
                byte* imagePointer1 = (byte*) bitmapData1.Scan0;

                for (i = 0; i < bitmapData1.Height; i++) {
                    for (j = 0; j < bitmapData1.Width; j++) {

                        if (output[j, i] < 0) {
                            imagePointer1[0] = 0;
                            imagePointer1[1] = 255;
                            imagePointer1[2] = 0;
                        } else if ((output[j, i] >= 0) && (output[j, i] < 50)) {
                            imagePointer1[0] = (byte) ((output[j, i]) * 4);
                            imagePointer1[1] = 0;
                            imagePointer1[2] = 0;
                        } else if ((output[j, i] >= 50) && (output[j, i] < 100)) {
                            imagePointer1[0] = 0;
                            imagePointer1[1] = (byte) (output[j, i] * 2);
                            imagePointer1[2] = (byte) (output[j, i] * 2);
                        } else if ((output[j, i] >= 100) && (output[j, i] < 255)) {
                            imagePointer1[0] = 0;
                            imagePointer1[1] = (byte) (output[j, i]);
                            imagePointer1[2] = 0;
                        } else if ((output[j, i] > 255)) {
                            imagePointer1[0] = 0;
                            imagePointer1[1] = 0;
                            imagePointer1[2] = (byte) ((output[j, i]) * 0.7);
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

        public int[,] FastDCT() {

            double[,] temp = new double[Width, Height];
            DCTCoefficients = new double[Width, Height];
            DCTkernel = new double[Width, Height];
            DCTkernel = GenerateDCTmatrix(Order);
            temp = multiply(DCTkernel, Input);
            DCTCoefficients = multiply(temp, Transpose(DCTkernel));
            return DCTPlotGenerate();
        }

        public void FastInverseDCT() {

            double[,] temp = new double[Width, Height];
            IDTCoefficients = new double[Width, Height];
            DCTkernel = new double[Width, Height];
            DCTkernel = Transpose(GenerateDCTmatrix(Order));
            temp = multiply(DCTkernel, DCTCoefficients);
            IDTCoefficients = multiply(temp, Transpose(DCTkernel));
            IDCTImage = Displayimage(IDTCoefficients);
            return;
        }

        public double[,] GenerateDCTmatrix(int order) {

            int i, j;
            int N;
            N = order;
            double alpha;
            double denominator;
            double[,] DCTCoeff = new double[N, N];

            for (j = 0; j <= N - 1; j++) {
                DCTCoeff[0, j] = Math.Sqrt(1 / (double) N);
            }

            alpha = Math.Sqrt(2 / (double) N);
            denominator = (double) 2 * N;

            for (j = 0; j <= N - 1; j++) {
                for (i = 1; i <= N - 1; i++) {
                    DCTCoeff[i, j] = alpha * Math.Cos(((2 * j + 1) * i * 3.14159) / denominator);
                }
            }
            return (DCTCoeff);
        }

        private double[,] multiply(double[,] m1, double[,] m2) {

            int row, col, i, j, k;
            row = col = m1.GetLength(0);
            double[,] m3 = new double[row, col];
            double sum;
            for (i = 0; i <= row - 1; i++) {
                for (j = 0; j <= col - 1; j++) {

                    sum = 0;

                    for (k = 0; k <= row - 1; k++) {
                        sum = sum + m1[i, k] * m2[k, j];
                    }
                    m3[i, j] = sum;
                }
            }
            return m3;
        }

        private double[,] Transpose(double[,] m) {

            int i, j;
            int Width, Height;
            Width = m.GetLength(0);
            Height = m.GetLength(1);

            double[,] mt = new double[m.GetLength(0), m.GetLength(1)];

            for (i = 0; i <= Height - 1; i++)
                for (j = 0; j <= Width - 1; j++) {
                    mt[j, i] = m[i, j];
                }
            return (mt);
        }

        private int[,] DCTPlotGenerate() {

            int i, j;
            int[,] temp = new int[Width, Height];
            double[,] DCTLog = new double[Width, Height];

            for (i = 0; i <= Width - 1; i++) {
                for (j = 0; j <= Height - 1; j++) {
                    DCTLog[i, j] = Math.Log(1 + Math.Abs((int) DCTCoefficients[i, j]));
                }
            }

            double min, max;
            min = max = DCTLog[1, 1];

            for (i = 1; i <= Width - 1; i++) {
                for (j = 1; j <= Height - 1; j++) {

                    if (DCTLog[i, j] > max)
                        max = DCTLog[i, j];
                    if (DCTLog[i, j] < min)
                        min = DCTLog[i, j];
                }
            }

            for (i = 0; i <= Width - 1; i++) {
                for (j = 0; j <= Height - 1; j++) {
                    temp[i, j] = (int) (((float) (DCTLog[i, j] - min) / (float) (max - min)) * 750);
                }
            }
            return temp;
        }
    }
}
