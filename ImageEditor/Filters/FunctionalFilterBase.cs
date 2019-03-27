using System.Drawing;
using System.Drawing.Imaging;
using System.Threading.Tasks;
using ImageEditor.Filters.Interfaces;

namespace ImageEditor.Filters
{

    /// <summary>
    /// source: http://csharpexamples.com/fast-image-processing-c/
    /// </summary>
    public abstract class FunctionalFilterBase:IFilter
    {
        public abstract string Name { get; }
        public abstract Color Transform(byte r,byte g, byte b);
        protected Bitmap ProcessedBitmap { get; private set; }
        public Bitmap Filter(Bitmap processedBitmap)
        {
            ProcessedBitmap = processedBitmap;
            SetUpBeforeFiltering();           
            unsafe
            {
                BitmapData bitmapData = processedBitmap.LockBits(new Rectangle(0, 0, processedBitmap.Width, processedBitmap.Height), ImageLockMode.ReadWrite, processedBitmap.PixelFormat);

                int bytesPerPixel = Image.GetPixelFormatSize(processedBitmap.PixelFormat) / 8;
                int heightInPixels = bitmapData.Height;
                int widthInBytes = bitmapData.Width * bytesPerPixel;
                byte* ptrFirstPixel = (byte*)bitmapData.Scan0;

                Parallel.For(0, heightInPixels, y =>
                {
                    byte* currentLine = ptrFirstPixel + (y * bitmapData.Stride);
                    for (int x = 0; x < widthInBytes; x = x + bytesPerPixel)
                    {
                        int oldBlue = currentLine[x];
                        int oldGreen = currentLine[x + 1];
                        int oldRed = currentLine[x + 2];
                        var result=Transform((byte)oldRed, (byte)oldGreen,(byte)oldBlue);
                        currentLine[x] = result.B;
                        currentLine[x + 1] = result.G;
                        currentLine[x + 2] = result.R;
                    }
                });
                processedBitmap.UnlockBits(bitmapData);
                return processedBitmap;
            }
        }
        protected virtual void SetUpBeforeFiltering(){}
    }
}