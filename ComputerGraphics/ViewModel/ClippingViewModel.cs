using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.CommandWpf;
using OxyPlot.Wpf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace ImageEditor.ViewModel
{
    public class ClippingViewModel : ViewModelBase
    {      
        #region private fields
        private const int CanvasColor = byte.MaxValue;
        private WriteableBitmap _bitmap;
        private const int BitmapWidth = 1000;
        private const int BitmapHeight = 1000;
        static readonly List<(List<Point> points, bool isClosed)> Polygons = new List<(List<Point>, bool)>();
        private static readonly byte[,,] Pixels = new byte[BitmapHeight, BitmapWidth, 4];
        private int _stride;
        private static Point? _lastPoint;
        #endregion

        #region commands
        public RelayCommand<object> ClickCommand { get; }
        public RelayCommand ClearCanvasCommand { get; set; }
        #endregion

        #region properties
        public WriteableBitmap Bitmap
        {
            get => _bitmap;
            set
            {
                _bitmap = value;
                RaisePropertyChanged("Bitmap");
            }
        }
        public List<ChoiceViewModel> Choices { get; set; } = new List<ChoiceViewModel>
        {
           new ChoiceViewModel("Draw convex polygon",true,DrawPolygon),
           new ChoiceViewModel("Draw clipping line",false,DrawLine)
        };
        #endregion

        public ClippingViewModel()
        {
            ClickCommand = new RelayCommand<object>(Click);
            ClearCanvasCommand = new RelayCommand(ResetBitmap);
            ResetBitmap();
        }

        private void Click(object obj)
        {
            var e = obj as MouseButtonEventArgs;
            var p = e.GetPosition(((IInputElement)e.Source));
            var option=Choices.FirstOrDefault(r => r.IsEnabled);
            option?.Action(p);
            SetBitmap();
        }

        private static void DrawPolygon(Point point)
        {
            if (Polygons.Count == 0 || Polygons.All(p => p.isClosed))
            {
                Polygons.Add((new List<Point>(), false));
            }

            var polygon = Polygons.First(p => !p.isClosed);
            var points = polygon.points;

            if (points.Count == 0)
            {
                points.Add(point);
                DrawPoint(point, 3);
            }
            else if (points.First().DistanceTo(point) < 5)
            {
                WuLine(points.First(), points.Last());
                points.ForEach(p => DrawPoint(p, 3, 127));
                polygon.isClosed = true;
                Polygons.RemoveAt(Polygons.Count - 1);

                var isConvex = IsPolygonConvex(points);
                if (!isConvex)
                {
                    MessageBox.Show("Polygon is not convex, draw again");

                    foreach (var p in points)
                    {
                        DrawPoint(p, 3, 255);
                    }

                    for (int i = 0; i < points.Count - 1; i++)
                    {
                        ResetWuLine(points[i], points[i + 1]);
                    }

                    ResetWuLine(points.First(), points.Last());
                }
                else
                {
                    Polygons.Add(polygon);
                }
            }
            else
            {
                DrawPoint(point, 3);
                WuLine(points.Last(), point);
                points.Add(point);
            }
        }

        #region helpers
        public class ChoiceViewModel
        {
            public ChoiceViewModel(string description, bool isEnabled, Action<Point> action)
            {
                Description = description;
                IsEnabled = isEnabled;
                Action = action;
            }

            public string Description { get; set; }
            public Action<Point> Action { get; set; }
            public bool IsEnabled { get; set; }
        }
        public static bool IsPolygonConvex(List<Point> points)
        {
            // For each set of three adjacent points A, B, C,
            // find the cross product AB · BC. If the sign of
            // all the cross products is the same, the angles
            // are all positive or negative (depending on the
            // order in which we visit them) so the polygon
            // is convex.
            bool got_negative = false;
            bool got_positive = false;
            int num_points = points.Count;
            int B, C;
            for (int A = 0; A < num_points; A++)
            {
                B = (A + 1) % num_points;
                C = (B + 1) % num_points;

                double cross_product =
                    CrossProductLength(points[A], points[B], points[C]);
                if (cross_product < 0)
                {
                    got_negative = true;
                }
                else if (cross_product > 0)
                {
                    got_positive = true;
                }

                if (got_negative && got_positive) return false;
            }

            // If we got this far, the polygon is convex.
            return true;
        }
        public static double CrossProductLength(Point A, Point B, Point C)
        {
            // Get the vectors' coordinates.
            double BAx = A.X - B.X;
            double BAy = A.Y - B.Y;
            double BCx = C.X - B.X;
            double BCy = C.Y - B.Y;

            // Calculate the Z coordinate of the cross product.
            return (BAx * BCy - BAy * BCx);
        }
        #endregion

        #region drawing lines & circles   
        private static void DrawLine(Point point)
        {
            DrawPoint(point, 3);
            if (_lastPoint == null)
            {
                _lastPoint = point;
            }
            else
            {
                WuLine(point,(Point)_lastPoint);
                _lastPoint = null;
            }
        }
        private static void WuLine(Point p1,Point p2)
        {          
            byte L = 0;
            byte B = CanvasColor;
            double dx = p2.X - p1.X;
            double dy = p2.Y - p1.Y;
            double m = dy / dx;
            if (Math.Abs(m) < 1)
            {
                double y = (int)p1.X < (int)p2.X ? p1.Y : p2.Y;
                int beginX;
                int endX;
                if ((int)p1.X < (int)p2.X)
                {
                    beginX = (int)p1.X;
                    endX = (int)p2.X;
                }
                else
                {
                    beginX = (int)p2.X;
                    endX = (int)p1.X;
                }

                for (var x = beginX; x <= endX; ++x)
                {
                    var c1 = L * (1 - (y - (int)y)) + B * (y - (int)y);
                    var c2 = L * (y - (int)y) + B * (1 - (y - (int)y));
                    DrawPoint(new Point(x, Math.Floor(y)), 0, (byte)c1);
                    DrawPoint(new Point(x, Math.Floor(y) + 1), 0, (byte)c2);
                    y += m;
                }
            }
            else
            {
                double x = (int)p1.Y < (int)p2.Y ? p1.X : p2.X;
                int beginY;
                int endY;
                if ((int)p1.Y < (int)p2.Y)
                {
                    beginY = (int)p1.Y;
                    endY = (int)p2.Y;
                }
                else
                {
                    beginY = (int)p2.Y;
                    endY = (int)p1.Y;
                }

                for (var y = beginY; y <= endY; ++y)
                {
                    var c1 = L * (1 - (x - (int)x)) + B * (x - (int)x);
                    var c2 = L * (x - (int)x) + B * (1 - (x - (int)x));
                    DrawPoint(new Point(Math.Floor(x), y), 0, (byte)c1);
                    DrawPoint(new Point(Math.Floor(x) + 1, y), 0, (byte)c2);
                    x += 1 / m;
                }
            }         
        }
        private static void ResetWuLine(Point p1, Point p2)
        {
            byte L = 0;
            byte B = CanvasColor; /*Background Color*/
            double dx = p2.X - p1.X;
            double dy = p2.Y - p1.Y;
            double m = dy / dx;
            if (Math.Abs(m) < 1)
            {
                double y = (int)p1.X < (int)p2.X ? p1.Y : p2.Y;
                int beginX;
                int endX;
                if ((int)p1.X < (int)p2.X)
                {
                    beginX = (int)p1.X;
                    endX = (int)p2.X;
                }
                else
                {
                    beginX = (int)p2.X;
                    endX = (int)p1.X;
                }

                for (var x = beginX; x <= endX; ++x)
                {
                    DrawPoint(new Point(x, Math.Floor(y)), 0, CanvasColor);
                    DrawPoint(new Point(x, Math.Floor(y) + 1), 0, CanvasColor);
                    y += m;
                }
            }
            else
            {
                double x = (int)p1.Y < (int)p2.Y ? p1.X : p2.X;
                int beginY;
                int endY;
                if ((int)p1.Y < (int)p2.Y)
                {
                    beginY = (int)p1.Y;
                    endY = (int)p2.Y;
                }
                else
                {
                    beginY = (int)p2.Y;
                    endY = (int)p1.Y;
                }

                for (var y = beginY; y <= endY; ++y)
                {
                    DrawPoint(new Point(Math.Floor(x), y), 0, CanvasColor);
                    DrawPoint(new Point(Math.Floor(x) + 1, y), 0, CanvasColor);
                    x += 1 / m;
                }
            }
        }
        private static void DrawPoint(Point p, int offset = 1, byte intensity = 0)
        {
            if (offset == 0)
            {
                for (int k = 0; k < 3; k++)
                    Pixels[(int)Math.Round(p.Y), (int)Math.Round(p.X), k] = intensity;
                return;
            }

            for (int i = -offset; i < offset; i++)
                for (int j = -offset; j < offset; j++)
                    for (int k = 0; k < 3; k++)
                        Pixels[(int)Math.Round(p.Y) + j, (int)Math.Round(p.X) + i, k] = intensity;
        }
        #endregion

        #region bitmap initalization & clear
        private void ResetBitmap()
        {
            _lastPoint = null;
            SetPixelArray();
            SetBitmap();
        }
        private void SetPixelArray()
        {
            for (int row = 0; row < BitmapHeight; row++)
            {
                for (int col = 0; col < BitmapWidth; col++)
                {
                    for (int i = 0; i < 3; i++)
                    {

                        Pixels[row, col, i] = CanvasColor;
                    }

                    Pixels[row, col, 3] = byte.MaxValue;
                }
            }
        }
        private void SetBitmap()
        {
            var tmp = new WriteableBitmap(
                BitmapWidth, BitmapHeight, 96, 96, PixelFormats.Bgra32, null);
            // Copy the data into a one-dimensional array.
            byte[] pixels1d = new byte[BitmapHeight * BitmapWidth * 4];
            int index = 0;
            for (int row = 0; row < BitmapHeight; row++)
            {
                for (int col = 0; col < BitmapWidth; col++)
                {
                    for (int i = 0; i < 4; i++)
                        pixels1d[index++] = Pixels[row, col, i];
                }
            }

            // Update writeable bitmap with the colorArray to the image.
            Int32Rect rect = new Int32Rect(0, 0, BitmapWidth, BitmapHeight);
            _stride = 4 * ((BitmapWidth * tmp.Format.BitsPerPixel + 31) / 32);
            tmp.WritePixels(rect, pixels1d, _stride, 0);
            Bitmap = tmp;
        }
        #endregion
    }
}
