using ImageEditor.Filters.Interfaces;
using ImageEditor.Helpers;
using MoreLinq;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Threading.Tasks;

namespace ImageEditor.Filters.Functional
{
    public class AverageDithering : FunctionalFilterBase, IParameterized
    {
        public override string Name => "Average Dithering";

        private List<byte> greyLevels;

        private List<byte> thresholds;

        public AverageDithering()
        {
            SetUpParameters();
        }

        private void SetUpParameters()
        {
            Parameters = new List<Parameter>
            {
                new Parameter("Number of grey levels"){Value = "2"}
            };
            for (int i = 2; i < 255; i += 2)
            {
                Parameters[0].PossibleValues.Add(i.ToString());
            }
        }

        protected override void SetUpBeforeFiltering()
        {
            var numberOfGreyLevels = byte.Parse(Parameters[0].Value);
            greyLevels = GetGreyLevels(numberOfGreyLevels).OrderBy(g => g).ToList();
            thresholds = new List<byte>();
            for (int i = 0; i < greyLevels.Count - 1; i++)
            {
                thresholds.Add(FindAverageThreshold(greyLevels[i], greyLevels[i + 1]));
            }
        }

        public override Color Transform(byte r, byte g, byte b)
        {
            var (lowerBound, upperBound) = GetClosestGreyLevel(greyLevels, Color.FromArgb(r, g, b));
            var intensity = (byte)(0.3 * r + 0.6 * g + 0.1 * b);
            var index = greyLevels.IndexOf(lowerBound);
            if (intensity <= thresholds[index])
            {
                return Color.FromArgb(lowerBound, lowerBound, lowerBound);
            }
            return Color.FromArgb(upperBound, upperBound, upperBound);
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
                        var intensity = (byte)(0.3 * red + 0.6 * green + 0.1 * blue);
                        if ((intensity >= min) && (intensity <= max))
                        {
                            threshold += intensity;
                            count++;
                        }
                    }
                });
                ProcessedBitmap.UnlockBits(bitmapData);
            }

            return (byte)(threshold / count).TruncateRgb();
        }

        public List<byte> GetGreyLevels(byte numberOfGreyLevels)
        {
            var levels = new List<byte>();
            var step = 255.0 / (double)(numberOfGreyLevels - 1);
            levels.Add(0);
            for (double i = step; i < 255; i += step)
            {
                levels.Add((byte)i);
            }
            levels.Add(255);
            return levels;
        }

        public (byte lowerbound, byte upperbound) GetClosestGreyLevel(List<byte> levels, Color color)
        {

            var intensity = (byte)(0.3 * color.R + 0.6 * color.G + 0.1 * color.B);
            if (intensity == byte.MaxValue)
                return (levels.OrderByDescending(l => l).ElementAt(1), byte.MaxValue);
            var max = levels.Where(l => l - intensity > 0).MinBy(l => l - intensity).First();
            var min = levels.Where(l => l - intensity <= 0).MaxBy(l => l - intensity).First();
            return ((byte)min, (byte)max);
        }

        public List<Parameter> Parameters { get; set; }
    }
}
