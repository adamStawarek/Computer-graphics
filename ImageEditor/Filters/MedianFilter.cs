using ImageEditor.Filters.Interfaces;
using ImageEditor.Helpers;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Color = System.Drawing.Color;

namespace ImageEditor.Filters
{
    public class MedianFilter : IFilter
    {
        private byte[,] MatrixR { get; set; }
        private byte[,] MatrixG { get; set; }
        private byte[,] MatrixB { get; set; }
        private double Bias => 0;
        public string Name => "Median";

        public Bitmap Filter(Bitmap image)
        {
            var pointColorDict = new Dictionary<Point, Color>();
            int filterOffset = 1;// 3x3 matrix

            for (int y = 0; y < image.Height; y++)
            {
                for (int x = 0; x < image.Width; x++)
                {
                    MatrixR = new byte[3, 3];
                    MatrixG = new byte[3, 3];
                    MatrixB = new byte[3, 3];
                    for (int filterY = -filterOffset, i = 0; filterY <= filterOffset; filterY++, i++)
                    {
                        for (int filterX = -filterOffset, j = 0; filterX <= filterOffset; filterX++, j++)
                        {
                            //handling edge pixels
                            var cordX = x + filterX;
                            var cordY = y + filterY;
                            if (cordX < 0 || cordX >= image.Width)
                                cordX = x;
                            if (cordY < 0 || cordY >= image.Height)
                                cordY = y;

                            var pixel = image.GetPixel(cordX, cordY);
                            MatrixR[i, j] = pixel.R;
                            MatrixG[i, j] = pixel.G;
                            MatrixB[i, j] = pixel.B;
                        }
                    }

                    var medR = GetMatrixMedian(MatrixR);
                    var medG = GetMatrixMedian(MatrixG);
                    var medB = GetMatrixMedian(MatrixB);

                    var red = (int)(medR + Bias);
                    var green = (int)(medG + Bias);
                    var blue = (int)(medB + Bias);
                    pointColorDict.Add(new Point(x, y), Color.FromArgb(red.TruncateRgb(), green.TruncateRgb(), blue.TruncateRgb()));
                }
            }

            foreach (var c in pointColorDict)
            {
                image.SetPixel(c.Key.X, c.Key.Y, c.Value);
            }

            return image;
        }

        private byte GetMatrixMedian(byte[,] xs)
        {
            var lst = xs.Cast<byte>().ToList();
            lst.Sort();
            return lst[lst.Count / 2];
        }        
    }
}
