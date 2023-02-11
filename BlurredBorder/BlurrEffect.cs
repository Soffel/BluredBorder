using System.Runtime.InteropServices;

namespace BlurryBitmap
{
    public class BlurEffect
    {
        public Task ApplyAsync(Bitmap bitmap, int blurPixelRadius)
        {
            if (bitmap == null)
                throw new ArgumentNullException(nameof(bitmap));
            if (blurPixelRadius <= 0)
                throw new ArgumentException("Pixel radius must be positive", nameof(blurPixelRadius));
            if (blurPixelRadius >= bitmap.Width)
                throw new ArgumentException("Pixel radius must less than bitmap width", nameof(blurPixelRadius));
            if (blurPixelRadius >= bitmap.Height)
                throw new ArgumentException("Pixel radius must less than bitmap height", nameof(blurPixelRadius));

       
            return Task.Factory.StartNew(() => Apply(bitmap, blurPixelRadius), TaskCreationOptions.LongRunning);
        }


        void Apply(Bitmap originalBitmap, int blurPixelRadius)
        {
            unsafe
            {
                var width = originalBitmap.Width;
                var height = originalBitmap.Height;
                var workingRect = new Rectangle(0, 0, width, height);

                var originalBitmapData = originalBitmap.LockBits(workingRect, System.Drawing.Imaging.ImageLockMode.ReadWrite, originalBitmap.PixelFormat);
                var stride = originalBitmapData.Stride;
                var ptr = originalBitmapData.Scan0;


                var bytesCount = Math.Abs(stride) * height;
                byte[] blurredRgbValues = new byte[bytesCount];

                var coeffGrid = GetCircularCoeffGrid(blurPixelRadius);
                Parallel.For(0, height, BlurRow);
               
                Marshal.Copy(blurredRgbValues, 0, ptr, bytesCount);
                originalBitmap.UnlockBits(originalBitmapData);

                void BlurRow(int row)
                {
                    for (int column = 0; column < width; column++)
                    {
                        List<float> rPoints = new List<float>();
                        List<float> gPoints = new List<float>();
                        List<float> bPoints = new List<float>();

                        int coeffCnt = 0;
                        for (int dX = -blurPixelRadius; dX <= blurPixelRadius; dX++)
                        {
                            for (int dY = -blurPixelRadius; dY <= blurPixelRadius; dY++)
                            {
                                var coeff = coeffGrid[dX + blurPixelRadius, dY + blurPixelRadius];

                                if (coeff)
                                {
                                    coeffCnt++;
                                    var x = column + dX;
                                    var y = row + dY;

                                    if (x >= 0 && y >= 0 && x < width && y < height)
                                    {
                                        var rIndex = (byte*)(ptr + y * stride + x * 3);
                                        var r = rIndex[0];
                                        var g = rIndex[1];
                                        var b = rIndex[2];

                                        rPoints.Add(r);
                                        gPoints.Add(g);
                                        bPoints.Add(b);
                                    }
                                }
                            }
                        }

                        var rAvg = (byte)rPoints.Average();
                        var gAvg = (byte)gPoints.Average();
                        var bAvg = (byte)bPoints.Average();

                        var destR = row * stride + column * 3;
                        blurredRgbValues[destR] = rAvg;
                        blurredRgbValues[destR + 1] = gAvg;
                        blurredRgbValues[destR + 2] = bAvg;
                    }
                }
            }
        }

     

        static bool[,] GetCircularCoeffGrid(int radius)
        {
            var size = radius * 2 + 1;
            var grid = new bool[size, size];

            for (int x = 0; x < size; x++)
            {
                for (int y = 0; y < size; y++)
                {
                    var diffX = x - radius;
                    var diffY = y - radius;

                    var distance = (float)Math.Sqrt(Math.Pow(diffX, 2) + Math.Pow(diffY, 2));


                    grid[x, y] = distance <= radius;
                }
            }

            return grid;
        }

       
    }
    
}
