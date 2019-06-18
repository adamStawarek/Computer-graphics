using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using ImageEditor.ViewModel.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
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
            new Double[]{ 0, 0, 1, 2 },
            new Double[]{ 0, 0, 0, 1 }
        }, P1, P2, T;
        private Vertex[] _vertices, _verticesL, _verticesR;
        private Triangle[] _triangles, _trianglesL, _trianglesR;
        private int m = 50, n = 50;
        private double Cx = BitmapWidth / 3, Cy = BitmapHeight / 3, d = 1800, e = 50;
        #endregion

        #region properties
        public List<ConfigurationViewModel> Configuration { get; set; } = new List<ConfigurationViewModel>
        {
            new ConfigurationViewModel("Radius",1 ),
            new ConfigurationViewModel("Zoom",1.5 ),
            new ConfigurationViewModel("Number of meridians",50),
            new ConfigurationViewModel("Number of parallels",50 ),
            new ConfigurationViewModel("Rotate X",0),
            new ConfigurationViewModel("Rotate Y",0),
            new ConfigurationViewModel("Center X",BitmapWidth/3),
            new ConfigurationViewModel("Center Y",BitmapHeight/3),
            new ConfigurationViewModel("Distance between the eyes (mm)", 50)
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
        #endregion

        #region commands
        public RelayCommand DrawShapesCommand { get; set; }
        public RelayCommand ChangeConfigurationCommand { get; set; }
        #endregion

        public StereoscopyViewModel()
        {
            _pixels = new byte[BitmapHeight, BitmapWidth, 4];
            ResetBitmap();
            DrawShapesCommand = new RelayCommand(DrawSphere);
            ChangeConfigurationCommand = new RelayCommand(ChangeConfiguration);
        }

        private void ChangeConfiguration()
        {
            r = Configuration.First(c => c.Description == "Radius").Value;
            zoom = Configuration.First(c => c.Description == "Zoom").Value;
            m = (int)Configuration.First(c => c.Description == "Number of meridians").Value;
            n = (int)Configuration.First(c => c.Description == "Number of parallels").Value;
            alphaX = Configuration.First(c => c.Description == "Rotate X").Value;
            alphaY = Configuration.First(c => c.Description == "Rotate Y").Value;
            Cx = Configuration.First(c => c.Description == "Center X").Value;
            Cy = Configuration.First(c => c.Description == "Center Y").Value;
            e = Configuration.First(c => c.Description == "Distance between the eyes (mm)").Value;
            t = new[]
            {
                new Double[] {1, 0, 0, 0},
                new Double[] {0, 1, 0, 0},
                new Double[] {0, 0, 1, zoom},
                new Double[] {0, 0, 0, 1}
            };
        }

        public void DrawSphere()
        {
            ResetBitmap();
            double s = Cx * (1 / Math.Tan(theta / 2));
            e = e / 1000.0;
            P1 = new[]
            {
                new[] {s, 0, Cx, (s*e)/2},
                new[] {0, -s, Cy, 0},
                new double[] {0, 0, 0, 1},
                new double[] {0, 0, 1, 0}
            };
            P2 = new[]
            {
                new[] {s, 0, Cx, -(s*e)/2},
                new[] {0, -s, Cy, 0},
                new double[] {0, 0, 0, 1},
                new double[] {0, 0, 1, 0}
            };
           
            _verticesL = CreateVertices();
            _verticesR = CreateVertices();

            TransformAndProject(P1, _verticesL);
            TransformAndProject(P2, _verticesR);

            _trianglesL = CreateTriangles(_verticesL);
            _trianglesR = CreateTriangles(_verticesR);

            DrawMesh(_trianglesL, Color.FromRgb(255, 0, 0));
            DrawMesh(_trianglesR,Color.FromRgb(0, 255, 255));
            SetBitmap();
        }

        private Vertex[] CreateVertices()
        {
            Vertex[] v = new Vertex[m * n + 2];
            double a, b, c;
            v[0] = new Vertex(0, r, 0, 1);
            v[0].SetT(new[] { new double[] { 1 }, new[] { 0.5 } });
            for (int i = 0; i < n; i++)
            {
                for (int j = 0; j < m; j++)
                {
                    a = r * Math.Cos((2 * Math.PI * j) / m) * Math.Sin((Math.PI * (i + 1)) / (n + 1));
                    b = r * Math.Cos((Math.PI * (i + 1)) / (n + 1));
                    c = r * Math.Sin((2 * Math.PI * j) / m) * Math.Sin((Math.PI * (i + 1)) / (n + 1));
                    v[i * m + j + 1] = new Vertex(a, b, c, 1);
                    v[i * m + j + 1].SetT(new[]
                    {new double[]{  j / (m - 1)}
                        ,new double[]{ (i + 1) / (n + 1) }});
                }
            }
            v[m * n + 1] = new Vertex(0, -r, 0, 1);
            v[m * n + 1].SetT(new[] { new double[] { 0 }, new double[] { 0.5 } });
            return v;
        }

        private void TransformAndProject(double[][] p, Vertex[] vertices)
        {
            double[][] rx ={
                new[]{ 1.0, 0, 0, 0 },
                new[] { 0, Math.Cos(alphaX), -Math.Sin(alphaX), 0 },
                new[] { 0, Math.Sin(alphaX), Math.Cos(alphaX), 0 },
                new[] { 0, 0, 0, 1.0 }
            };
            double[][] ry = {
                new[]{ Math.Cos(alphaY), 0, Math.Sin(alphaY), 0 },
                new[] { 0, 1.0, 0, 0 },
                new[] { -Math.Sin(alphaY), 0, Math.Cos(alphaY), 0 },
                new[] { 0, 0, 0, 1.0 }
            };
            T = MatrixMultiply(MatrixMultiply(t, rx), ry);
            foreach (var v in vertices)
            {
                var pointMatrix = MatrixMultiply(MatrixMultiply(p, T), v.GetP());
                v.SetXY(pointMatrix[0][0] / pointMatrix[3][0], pointMatrix[1][0] / pointMatrix[3][0]);
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

        private Triangle[] CreateTriangles(Vertex[] vertices)
        {
            var t = new Triangle[2 * m * n];
            for (int i = 0; i < m - 1; i++)
            {
                t[i] = new Triangle(vertices[0], vertices[i + 2], vertices[i + 1]);
                t[2 * (n - 1) * m + i + m] = new Triangle(vertices[m * n + 1], vertices[(n - 1) * m + i + 1], vertices[(n - 1) * m + i + 2]);
            }
            t[m - 1] = new Triangle(vertices[0], vertices[1], vertices[m]);
            t[2 * (n - 1) * m + m - 1 + m] = new Triangle(vertices[m * n + 1], vertices[m * n], vertices[(n - 1) * m + 1]);
            for (int i = 0; i < n - 1; i++)
            {
                for (int j = 1; j < m; j++)
                {
                    t[(2 * i + 1) * m + j - 1] = new Triangle(vertices[i * m + j], vertices[i * m + j + 1], vertices[(i + 1) * m + j + 1]);
                    t[(2 * i + 2) * m + j - 1] = new Triangle(vertices[i * m + j], vertices[(i + 1) * m + j + 1], vertices[(i + 1) * m + j]);
                }
                t[(2 * i + 1) * m + m - 1] = new Triangle(vertices[(i + 1) * m], vertices[i * m + 1], vertices[(i + 1) * m + 1]);
                t[(2 * i + 2) * m + m - 1] = new Triangle(vertices[(i + 1) * m], vertices[(i + 1) * m + 1], vertices[(i + 2) * m]);
            }
            return t;
        }

        private void DrawMesh(Triangle[] triangles,Color color)
        {
            foreach (var t in triangles)
            { // Check if third component of cross product is positive
                if ((t.GetV2().GetX() - t.GetV1().GetX())
                    * (t.GetV3().GetY() - t.GetV1().GetY())
                    - (t.GetV2().GetY() - t.GetV1().GetY())
                    * (t.GetV3().GetX() - t.GetV1().GetX()) > 0)
                {
                    DrawTriangle(t,color);
                }
            }
        }

        private void DrawTriangle(Triangle t,Color color)
        {            
            DrawWuLine(new Point(t.GetV1().GetX(), t.GetV1().GetY()),
                new Point(t.GetV2().GetX(), t.GetV2().GetY()), color);
            DrawWuLine(new Point(t.GetV2().GetX(), t.GetV2().GetY()),
                new Point(t.GetV3().GetX(), t.GetV3().GetY()), color);
            DrawWuLine(new Point(t.GetV3().GetX(), t.GetV3().GetY()),
                new Point(t.GetV1().GetX(), t.GetV1().GetY()), color);
        }

        #region draw line & point
        private void DrawWuLine(Point p1, Point p2, Color color)
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
                    DrawPoint(new Point(x, Math.Floor(y)), color);
                    DrawPoint(new Point(x, Math.Floor(y) + 1), color);
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
                    DrawPoint(new Point(Math.Floor(x), y), color);
                    DrawPoint(new Point(Math.Floor(x) + 1, y), color);
                    x += 1 / m;
                }
            }
        }

        private void DrawPoint(Point p, Color color)
        {
            try
            {
                _pixels[(int)Math.Round(p.Y), (int)Math.Round(p.X), 0] = color.B;
                _pixels[(int)Math.Round(p.Y), (int)Math.Round(p.X), 1] = color.G;
                _pixels[(int)Math.Round(p.Y), (int)Math.Round(p.X), 2] = color.R;
            }
            catch (Exception)
            {

                //throw;
            }
        } 
        #endregion

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