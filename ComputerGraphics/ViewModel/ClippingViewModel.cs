using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.CommandWpf;
using ImageEditor.ViewModel.Helpers;
using OxyPlot.Wpf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Color = System.Drawing.Color;

namespace ImageEditor.ViewModel
{
    public class ClippingViewModel : ViewModelBase
    {
        #region private fields
        private const int CanvasColor = byte.MaxValue;
        private WriteableBitmap _bitmap;
        private const int BitmapWidth = 1000;
        private const int BitmapHeight = 1000;
        private readonly List<(List<Point> points, bool isClosed)> _polygons;
        private readonly List<(Point a, Point b)> _edges;
        private readonly byte[,,] _pixels;
        private int _stride;
        private Point? _lastPoint;
        private Dictionary<int, List<ScanLine>> _aet;
        private Dictionary<int, List<ScanLine>> _et;
        #endregion

        #region commands
        public RelayCommand<object> ClickCommand { get; }
        public RelayCommand ClearCanvasCommand { get; set; }
        public RelayCommand ApplyFillingCommand { get; set; }
        #endregion

        #region properties
        public Color SelectedColor { get; set; } = Color.Red;
        public List<Color> Colors => new List<Color>
        {
            Color.Red, Color.Blue, Color.Yellow
        };
        public WriteableBitmap Bitmap
        {
            get => _bitmap;
            set
            {
                _bitmap = value;
                RaisePropertyChanged("Bitmap");
            }
        }
        public List<ChoiceViewModel> Choices { get; set; }
        #endregion

        public ClippingViewModel()
        {
            ClickCommand = new RelayCommand<object>(Click);
            ClearCanvasCommand = new RelayCommand(ResetBitmap);
            ApplyFillingCommand = new RelayCommand(ApplyFilling);

            _polygons = new List<(List<Point>, bool)>();
            _edges = new List<(Point a, Point b)>();
            _pixels = new byte[BitmapHeight, BitmapWidth, 4];
            Choices = new List<ChoiceViewModel>
            {
                new ChoiceViewModel("Draw convex polygon", true, DrawConvexPolygon),
                new ChoiceViewModel("Draw polygon", false, DrawPolygon),
                new ChoiceViewModel("Draw clipping line", false, DrawLine)
            };

            ResetBitmap();
        }

        private void Click(object obj)
        {
            var e = obj as MouseButtonEventArgs;
            var p = e.GetPosition(((IInputElement)e.Source));
            var option = Choices.FirstOrDefault(r => r.IsEnabled);
            option?.Action(p);
            SetBitmap();
        }

        private void DrawConvexPolygon(Point point)
        {
            if (_polygons.Count == 0 || _polygons.All(p => p.isClosed))
            {
                _polygons.Add((new List<Point>(), false));
            }

            var polygon = _polygons.First(p => !p.isClosed);
            var points = polygon.points;

            if (points.Count == 0)
            {
                points.Add(point);
                DrawPoint(point, 3);
            }
            else if (points.First().DistanceTo(point) < 5)
            {
                DrawWuLine(points.First(), points.Last());
                points.ForEach(p => DrawPoint(p, 3, 127));
                polygon.isClosed = true;
                _polygons.RemoveAt(_polygons.Count - 1);

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
                    _polygons.Add(polygon);
                }
            }
            else
            {
                DrawPoint(point, 3);
                DrawWuLine(points.Last(), point);
                points.Add(point);
            }
        }

        private void DrawPolygon(Point point)
        {
            if (_polygons.Count == 0 || _polygons.All(p => p.isClosed))
            {
                _polygons.Add((new List<Point>(), false));
            }

            var polygon = _polygons.First(p => !p.isClosed);
            var points = polygon.points;

            if (points.Count == 0)
            {
                points.Add(point);
                DrawPoint(point, 3);
            }
            else if (points.First().DistanceTo(point) < 5)
            {
                DrawWuLine(points.First(), points.Last());
                points.ForEach(p => DrawPoint(p, 3, 127));
                polygon.isClosed = true;
                _polygons.RemoveAt(_polygons.Count - 1);
                _polygons.Add(polygon);

            }
            else
            {
                DrawPoint(point, 3);
                DrawWuLine(points.Last(), point);
                points.Add(point);
            }
        }

        private void ApplyFilling()
        {
            FillPolygons();
            SetBitmap();
        }

        private void FillPolygons()
        {
            foreach (var polygon in _polygons.Where(p => p.isClosed))
            {
                FillPolygon(polygon.points);
            }

        }

        private void FillPolygon(List<Point> points)
        {
            var edgeTable = InitializeEdgeTable(
                points.Select(p => new System.Drawing.Point((int)p.X, (int)p.Y)).ToList());

            var activeEdgeTable = new List<EdgeData>();

            for (var i = 0; i < BitmapHeight; i++)
            {
                UpdateActiveEdgeTable(edgeTable, activeEdgeTable, i);

                activeEdgeTable.Sort();

                FillBetweenIntersections(activeEdgeTable, i);
            }
        }

        private static List<EdgeData> InitializeEdgeTable(List<System.Drawing.Point> pointList)
        {
            var list = new List<EdgeData>();
            for (int i = 0; i < pointList.Count; i++)
            {
                var ed = new EdgeData();
                var point1 = pointList[i];
                var point2 = i != pointList.Count - 1 ? pointList[i + 1] : pointList[0];
                if (point1.Y <= point2.Y)
                {
                    ed.SetStartPoint(point1);
                    ed.SetEndPoint(point2);
                }
                else
                {
                    ed.SetStartPoint(point2);
                    ed.SetEndPoint(point1);
                }
                ed.CalculateRatio();
                list.Add(ed);
            }
            return list;
        }

        private static void UpdateActiveEdgeTable(List<EdgeData> edgeTable, List<EdgeData> activeEdgeTable, int i)
        {
            foreach (var ed in edgeTable)
            {
                // Checking if an edge intersects a scanned line
                if (ed.GetStartPoint().Y <= i && ed.GetEndPoint().Y >= i)
                {
                    // If the Active Edge Table does not contain the edge - add
                    if (!activeEdgeTable.Contains(ed))
                        activeEdgeTable.Add(ed);
                }
                // If the edge does not intersect a scanned line and the Active Edge Table contains it - remove
                else
                {
                    activeEdgeTable.Remove(ed);
                }
            }
        }

        private void FillBetweenIntersections(List<EdgeData> activeEdgeTable, int i)
        {
            for (int j = 0; j < activeEdgeTable.Count; j++)
            {               
                if (j == activeEdgeTable.Count - 1 || j % 2 != 0) continue;
                for (int x = activeEdgeTable[j].CalculateX(i); x <= activeEdgeTable[j + 1].CalculateX(i); x++)
                {
                    DrawPoint(new Point(x, i), SelectedColor);
                }
            }
        }

        private void Clip(Point rp, Point q)
        {
            var points = _polygons[0].points.Select(p => new System.Drawing.Point((int)p.X, (int)p.Y)).ToList();
            var polygon = new CyrusBeck.Polygon()
            {
                nPoints = points.Count(),
                v = points
            };
            var n = CyrusBeck.CalcNormals(points);
            var p1 = points[0];
            var p2 = points[1];
            var visible = CyrusBeck.CBClip(p1, p2, n, polygon, false, new System.Drawing.Point((int)rp.X, (int)rp.Y), new System.Drawing.Point((int)q.X, (int)q.Y));

            if (p1.X != rp.X || p2.X != q.X)
            {
                DrawWuLine(new Point(p1.X, p1.Y), new Point(rp.X, rp.Y));
                DrawWuLine(new Point(q.X, q.Y), new Point(p2.X, p2.Y));
            }
            else
            {
                DrawWuLine(new Point(p1.X, p1.Y), new Point(p2.X, p2.Y));
            }
        }

        #region helpers
        public class ScanLine
        {
            public ScanLine(int yMax, int xMin, double slope)
            {
                YMax = yMax;
                XMin = xMin;
                Slope = slope;
            }

            public int YMax { get; set; }
            public int XMin { get; set; }
            public double Slope { get; set; }
        }
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
        public bool IsPolygonConvex(List<Point> points)
        {
            // For each set of three adjacent points A, B, C,
            // find the cross product AB · BC. If the sign of
            // all the cross products is the same, the angles
            // are all positive or negative (depending on the
            // order in which we visit them) so the polygon
            // is convex.
            var gotNegative = false;
            var gotPositive = false;
            var numPoints = points.Count;
            for (var a = 0; a < numPoints; a++)
            {
                var b = (a + 1) % numPoints;
                var c = (b + 1) % numPoints;

                var crossProduct =
                    CrossProductLength(points[a], points[b], points[c]);
                if (crossProduct < 0)
                {
                    gotNegative = true;
                }
                else if (crossProduct > 0)
                {
                    gotPositive = true;
                }

                if (gotNegative && gotPositive) return false;
            }

            // If we got this far, the polygon is convex.
            return true;
        }
        public double CrossProductLength(Point a, Point b, Point c)
        {
            // Get the vectors' coordinates.
            var bAx = a.X - b.X;
            var bAy = a.Y - b.Y;
            var bCx = c.X - b.X;
            var bCy = c.Y - b.Y;

            // Calculate the Z coordinate of the cross product.
            return (bAx * bCy - bAy * bCx);
        }
        #endregion

        #region drawing lines & circles   
        private void DrawLine(Point point)
        {
            DrawPoint(point, 3);
            if (_lastPoint == null)
            {
                _lastPoint = point;
            }
            else
            {
                DrawWuLine(point, (Point)_lastPoint);
                _edges.Add((point, (Point)_lastPoint));
                Clip(point, (Point)_lastPoint);
                _lastPoint = null;

            }
        }
        private void DrawWuLine(Point p1, Point p2)
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
        private void ResetWuLine(Point p1, Point p2)
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
        private void DrawPoint(Point p, int offset = 1, byte intensity = 0)
        {
            if (offset == 0)
            {
                for (int k = 0; k < 3; k++)
                    _pixels[(int)Math.Round(p.Y), (int)Math.Round(p.X), k] = intensity;
                return;
            }

            for (int i = -offset; i < offset; i++)
                for (int j = -offset; j < offset; j++)
                    for (int k = 0; k < 3; k++)
                        _pixels[(int)Math.Round(p.Y) + j, (int)Math.Round(p.X) + i, k] = intensity;
        }
        private void DrawPoint(Point p, Color color)
        {
            _pixels[(int)Math.Round(p.Y), (int)Math.Round(p.X), 0] = color.B;
            _pixels[(int)Math.Round(p.Y), (int)Math.Round(p.X), 1] = color.R;
            _pixels[(int)Math.Round(p.Y), (int)Math.Round(p.X), 2] = color.G;
        }
        #endregion

        #region bitmap initalization & clear
        private void ResetBitmap()
        {
            _polygons.Clear();
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

                        _pixels[row, col, i] = CanvasColor;
                    }

                    _pixels[row, col, 3] = byte.MaxValue;
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
                        pixels1d[index++] = _pixels[row, col, i];
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
