using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Threading.Tasks;
using ImageEditor.Filters.Interfaces;
using ImageEditor.Helpers;
using MoreLinq;

namespace ImageEditor.Filters.Functional
{
    public class AverageDithering : FunctionalFilterBase, IParameterized
    {
        public override string Name => "Average Dithering";
        public byte ThresholdR { get; set; }
        public byte ThresholdG { get; set; }
        public byte ThresholdB { get; set; }

        private List<byte> greyLevels;
        //private byte thresholdR;       
        //private byte thresholdG;
        //private byte thresholdB;
        private byte threshold;

        protected override void SetUpBeforeFiltering()
        {
            //(ThresholdR,ThresholdG,ThresholdB) = FindAverageColorThreshold(0, 255);
            threshold = FindAverageThreshold(0, 255);
            var numberOfGreyLevels = byte.Parse(Parameters["Grey levels"]);
            greyLevels = GetGreyLevels(numberOfGreyLevels);

        }
        public override Color Transform(byte r,byte g,byte b)
        {
            var (lowerBound, upperBound) = GetClosestGreyLevel(greyLevels, Color.FromArgb(r, g, b));
            var intensity = (r + g + b) / 3;
            if (intensity < threshold)
                return Color.FromArgb(lowerBound,lowerBound,lowerBound);
            return Color.FromArgb(upperBound,upperBound,upperBound);
        }

        private byte FindAverageThreshold(int min, int max)
        {
            int threshold = 0, count = 0;
          
            unsafe
            {
                BitmapData bitmapData = ProcessedBitmap.LockBits(new Rectangle(0, 0, ProcessedBitmap.Width, ProcessedBitmap.Height), ImageLockMode.ReadWrite, ProcessedBitmap.PixelFormat);

                int bytesPerPixel = Image.GetPixelFormatSize(ProcessedBitmap.PixelFormat) / 8;
                int heightInPixels = bitmapData.Height;
                int widthInBytes = bitmapData.Width * bytesPerPixel;
                byte* ptrFirstPixel = (byte*)bitmapData.Scan0;

                Parallel.For(0, heightInPixels, y =>
                {
                    byte* currentLine = ptrFirstPixel + (y * bitmapData.Stride);
                    for (int x = 0; x < widthInBytes; x = x + bytesPerPixel)
                    {
                        int blue = currentLine[x];
                        int green = currentLine[x + 1];
                        int red = currentLine[x + 2];
                        var intensity = (red + green + blue) / 3;
                        if ((intensity >= min) && (intensity <= max))
                        {
                            threshold += intensity;
                            count++;
                        }                       
                    }
                });
                ProcessedBitmap.UnlockBits(bitmapData);
            }

            return (byte)(threshold/count).TruncateRgb();
        }

        private (byte thresholdR,byte thresholdG,byte thresholdB) FindAverageColorThreshold(int min, int max)
        {
            int thresholdR = 0, countR = 0;
            int thresholdG = 0, countG = 0;
            int thresholdB = 0, countB = 0;
            unsafe
            {
                BitmapData bitmapData = ProcessedBitmap.LockBits(new Rectangle(0, 0, ProcessedBitmap.Width, ProcessedBitmap.Height), ImageLockMode.ReadWrite, ProcessedBitmap.PixelFormat);

                int bytesPerPixel = Image.GetPixelFormatSize(ProcessedBitmap.PixelFormat) / 8;
                int heightInPixels = bitmapData.Height;
                int widthInBytes = bitmapData.Width * bytesPerPixel;
                byte* ptrFirstPixel = (byte*)bitmapData.Scan0;

                Parallel.For(0, heightInPixels, y =>
                {
                    byte* currentLine = ptrFirstPixel + (y * bitmapData.Stride);
                    for (int x = 0; x < widthInBytes; x = x + bytesPerPixel)
                    {
                        int blue = currentLine[x];
                        int green = currentLine[x + 1];
                        int red = currentLine[x + 2];
                        if ((red >= min) && (red <= max))
                        {
                            thresholdR += red;
                            countR++;
                        }
                        if ((green >= min) && (green <= max))
                        {
                            thresholdG += green;
                            countG++;
                        }
                        if ((blue >= min) && (blue <= max))
                        {
                            thresholdB += blue;
                            countB++;
                        }
                    }
                });
                ProcessedBitmap.UnlockBits(bitmapData);
            }

            var avgThresholdR = (byte)(thresholdR / countR).TruncateRgb();
            var avgThresholdG = (byte)(thresholdG / countG).TruncateRgb();
            var avgThresholdB = (byte)(thresholdB / countB).TruncateRgb();
            return (avgThresholdR,avgThresholdG,avgThresholdB);
        }

        public List<byte> GetGreyLevels(byte numberOfGreyLevels)
        {
            var levels = new List<byte>();
            var step = 255.0 / (double)(numberOfGreyLevels - 1);
            for (double i = 0; i <= 255; i += step)
            {
                levels.Add((byte)i);
            }

            return levels;
        }

        public (byte lowerbound, byte upperbound) GetClosestGreyLevel(List<byte> levels, Color color)
        {
            
            var intensity = (byte)((color.R + color.G + color.B) / 3);
            if (intensity == byte.MaxValue)
                return (levels.OrderByDescending(l => l).ElementAt(1), byte.MaxValue);
            var max = levels.Where(l => l - intensity > 0).MinBy(l => l - intensity).First();
            var min = levels.Where(l => l - intensity <= 0).MaxBy(l => l - intensity).First();
            return ((byte)min, (byte)max);
        }

        public Dictionary<string, string> Parameters { get; set; } = new Dictionary<string, string>
        {
            {"Grey levels","2" }
        };
    }
}
