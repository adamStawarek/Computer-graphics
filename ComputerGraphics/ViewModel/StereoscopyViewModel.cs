using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using ImageEditor.ViewModel.Helpers;
using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace ImageEditor.ViewModel
{
    public class StereoscopyViewModel : ViewModelBase
    {
        #region private fields
        private const int CanvasColor = byte.MaxValue;
        private WriteableBitmap _bitmap;
        private const int BitmapWidth = 1000;
        private const int BitmapHeight = 1000;
        private readonly byte[,,] _pixels;
        private int _stride;

        private double r = 1, theta = (Math.PI / 1.5), zoom = 1.5, alphaX = 0, alphaY = 0;
        private double[][] t = {
            new Double[]{ 1, 0, 0, 0 },
            new Double[]{ 0, 1, 0, 0 },
            new Double[]{ 0, 0, 1, 1.5 },
            new Double[]{ 0, 0, 0, 1 }
        }, P, T;
        private Vertex[] _vertices;
        private Triangle[] _triangles;
        private bool _drawn;
        private int m = 50, n = 50;
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
        #endregion

        #region commands
        public RelayCommand DrawShapesCommand { get; set; }
        #endregion

        public StereoscopyViewModel()
        {
            _pixels = new byte[BitmapHeight, BitmapWidth, 4];
            ResetBitmap();
            DrawShapesCommand = new RelayCommand(DrawSphere);
        }

        public void DrawSphere()
        {
            _drawn = true;
            double s = (BitmapWidth / 2) * (1 / Math.Tan(theta / 2));
            P = new[]
            {
                new double[] { -s, 0, BitmapWidth / 2, 0 },
                new double[] { 0, s, BitmapHeight / 2, 0 },
                new double[] { 0, 0, 0, 1 },
                new double[] { 0, 0, 1, 0 }
            };
            _vertices = CreateVertices();
            TransformAndProject();
            _triangles = CreateTriangles();
            DrawMesh();
            SetBitmap();
        }

        private Vertex[] CreateVertices()
        {
            Vertex[] v = new Vertex[m * n + 2];
            double a, b, c;
            v[0] = new Vertex(0, r, 0, r);
            v[0].setT(new double[][] { new double[] { 1 }, new double[] { 0.5 } });
            for (int i = 0; i < n; i++)
            {
                for (int j = 0; j < m; j++)
                {
                    a = r * Math.Cos((2 * Math.PI * j) / m) * Math.Sin((Math.PI * (i + 1)) / (n + 1));
                    b = r * Math.Cos((Math.PI * (i + 1)) / (n + 1));
                    c = r * Math.Sin((2 * Math.PI * j) / m) * Math.Sin((Math.PI * (i + 1)) / (n + 1));
                    v[i * m + j + 1] = new Vertex(a, b, c, r);
                    v[i * m + j + 1].setT(new double[][] {new double[]{  j / (m - 1)}
                        ,new double[]{ (i + 1) / (n + 1) }});
                }
            }
            v[m * n + 1] = new Vertex(0, -r, 0, r);
            v[m * n + 1].setT(new double[][] { new double[] { 0 }, new double[] { 0.5 } });
            return v;
        }

        private void TransformAndProject()
        {
            double[][] point;
            double[][] Rx ={
                new double[]{ 1, 0, 0, 0 },
                new double[] { 0, Math.Cos(alphaX), -Math.Sin(alphaX), 0 },
                new double[] { 0, Math.Sin(alphaX), Math.Cos(alphaX), 0 },
                new double[] { 0, 0, 0, 1 }
            };
            double[][] Ry = {
                new double[]{ Math.Cos(alphaY), 0, Math.Sin(alphaY), 0 },
                new double[] { 0, 1, 0, 0 },
                new double[] { -Math.Sin(alphaY), 0, Math.Cos(alphaY), 0 },
                new double[] { 0, 0, 0, 1 }
            };
            T = MatrixMultiply(MatrixMultiply(t, Rx), Ry);
            foreach (var v in _vertices)
            {
                point = MatrixMultiply(MatrixMultiply(P, T), v.getP());
                v.setXandY(point[0][0] / point[3][0], point[1][0] / point[3][0]);
            }
        }

        private static double[][] MatrixMultiply(double[][] a, double[][] b)
        {
            int aRows = a.Length;
            int aColumns = a[0].Length;
            int bRows = b.Length;
            int bColumns = b[0].Length;
            if (aColumns != bRows) throw new ArgumentException("matrices must have the same dimensions");
            double[][] C = new double[aRows][];
            for (int i = 0; i < aRows; i++)
            {
                C[i] = new double[bColumns];
                for (int j = 0; j < bColumns; j++)
                {
                    C[i][j] = 0.0;
                }
            }
            for (int i = 0; i < aRows; i++)
            {
                for (int j = 0; j < bColumns; j++)
                {
                    for (int k = 0; k < aColumns; k++)
                    {
                        C[i][j] += a[i][k] * b[k][j];
                    }
                }
            }
            return C;
        }

        private Triangle[] CreateTriangles()
        {
            Triangle[] t = new Triangle[2 * m * n];
            for (int i = 0; i < m - 1; i++)
            {
                t[i] = new Triangle(_vertices[0], _vertices[i + 2], _vertices[i + 1]);
                t[2 * (n - 1) * m + i + m] = new Triangle(_vertices[m * n + 1], _vertices[(n - 1) * m + i + 1], _vertices[(n - 1) * m + i + 2]);
            }
            t[m - 1] = new Triangle(_vertices[0], _vertices[1], _vertices[m]);
            t[2 * (n - 1) * m + m - 1 + m] = new Triangle(_vertices[m * n + 1], _vertices[m * n], _vertices[(n - 1) * m + 1]);
            for (int i = 0; i < n - 1; i++)
            {
                for (int j = 1; j < m; j++)
                {
                    t[(2 * i + 1) * m + j - 1] = new Triangle(_vertices[i * m + j], _vertices[i * m + j + 1], _vertices[(i + 1) * m + j + 1]);
                    t[(2 * i + 2) * m + j - 1] = new Triangle(_vertices[i * m + j], _vertices[(i + 1) * m + j + 1], _vertices[(i + 1) * m + j]);
                }
                t[(2 * i + 1) * m + m - 1] = new Triangle(_vertices[(i + 1) * m], _vertices[i * m + 1], _vertices[(i + 1) * m + 1]);
                t[(2 * i + 2) * m + m - 1] = new Triangle(_vertices[(i + 1) * m], _vertices[(i + 1) * m + 1], _vertices[(i + 2) * m]);
            }
            return t;
        }

        private void DrawMesh()
        {
            foreach (var t in _triangles)
            { // Check if third component of cross product is positive
                if ((t.GetV2().getX() - t.GetV1().getX())
                    * (t.GetV3().getY() - t.GetV1().getY())
                    - (t.GetV2().getY() - t.GetV1().getY())
                    * (t.GetV3().getX() - t.GetV1().getX()) > 0)
                {
                    DrawWuLine(new Point(t.GetV1().getX(), t.GetV1().getY()),
                        new Point(t.GetV2().getX(), t.GetV2().getY()));
                    DrawWuLine(new Point(t.GetV2().getX(), t.GetV2().getY()),
                        new Point(t.GetV3().getX(), t.GetV3().getY()));
                    DrawWuLine(new Point(t.GetV3().getX(), t.GetV3().getY()),
                        new Point(t.GetV1().getX(), t.GetV1().getY()));
                }
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

        #region bitmap initalization & clear
        private void ResetBitmap()
        {
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