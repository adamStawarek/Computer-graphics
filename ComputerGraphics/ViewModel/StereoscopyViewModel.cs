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
        private double r = 1, theta = (Math.PI / 1.5), zoom = 1.5, alphaX, alphaY;
        private double[][] t = {
            new Double[]{ 1, 0, 0, 0 },
            new Double[]{ 0, 1, 0, 0 },
            new Double[]{ 0, 0, 1, 2 },
            new Double[]{ 0, 0, 0, 1 }
        }, P1, P2, T;
        private Vertex[] _verticesL, _verticesR;
        private Triangle[] _trianglesL, _trianglesR;
        private int m = 50, n = 50;
        private double _cx = BitmapWidth / 3, Cy = BitmapHeight / 3, e = 50;
        private double _cuboidWidth=2, _cuboidHeight=2, _cuboidDepth=2;
        private double _cylinderH = 2, _cylinderR = 1;
        private int  _cylinderN = 10;
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
            new ConfigurationViewModel("Distance between the eyes (mm)", 50),
            new ConfigurationViewModel("Cuboid width", 2),
            new ConfigurationViewModel("Cuboid height", 2),
            new ConfigurationViewModel("Cuboid depth", 2),
            new ConfigurationViewModel("Cylinder height", 2),
            new ConfigurationViewModel("Cylinder base radius", 1),
            new ConfigurationViewModel("Cylinder #sides prism", 10)
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
            DrawShapesCommand = new RelayCommand(DrawCylinder);
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
            _cx = Configuration.First(c => c.Description == "Center X").Value;
            Cy = Configuration.First(c => c.Description == "Center Y").Value;
            e = Configuration.First(c => c.Description == "Distance between the eyes (mm)").Value;
            _cuboidWidth = Configuration.First(c => c.Description == "Cuboid width").Value;
            _cuboidHeight = Configuration.First(c => c.Description == "Cuboid height").Value;
            _cuboidDepth = Configuration.First(c => c.Description == "Cuboid depth").Value;
            _cylinderN = (int)Configuration.First(c => c.Description == "Cylinder #sides prism").Value;
            _cylinderH = Configuration.First(c => c.Description == "Cylinder height").Value;
            _cylinderR = Configuration.First(c => c.Description == "Cylinder base radius").Value; 

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
            double s = _cx * (1 / Math.Tan(theta / 2));
            e = e / 1000.0;
            P1 = new[]
            {
                new[] {s, 0, _cx, (s*e)/2},
                new[] {0, -s, Cy, 0},
                new double[] {0, 0, 0, 1},
                new double[] {0, 0, 1, 0}
            };
            P2 = new[]
            {
                new[] {s, 0, _cx, -(s*e)/2},
                new[] {0, -s, Cy, 0},
                new double[] {0, 0, 0, 1},
                new double[] {0, 0, 1, 0}
            };

            _verticesL = CreateSphereVertices();
            _verticesR = CreateSphereVertices();

            TransformAndProject(P1, _verticesL);
            TransformAndProject(P2, _verticesR);

            _trianglesL = CreateSphereTriangles(_verticesL);
            _trianglesR = CreateSphereTriangles(_verticesR);

            DrawMesh(_trianglesL, Color.FromRgb(255, 0, 0));
            DrawMesh(_trianglesR, Color.FromRgb(0, 255, 255));
            SetBitmap();
        }

        public void DrawCuboid()
        {
            ResetBitmap();
            double s = _cx * (1 / Math.Tan(theta / 2));
            e = e / 1000.0;
            P1 = new[]
            {
                new[] {s, 0, _cx, (s*e)/2},
                new[] {0, -s, Cy, 0},
                new double[] {0, 0, 0, 1},
                new double[] {0, 0, 1, 0}
            };
            P2 = new[]
            {
                new[] {s, 0, _cx, -(s*e)/2},
                new[] {0, -s, Cy, 0},
                new double[] {0, 0, 0, 1},
                new double[] {0, 0, 1, 0}
            };

            _verticesL = CreateCuboidVertices();
            //_verticesR = CreateCuboidVertices();

            TransformAndProject(P1, _verticesL);
            //TransformAndProject(P2, _verticesR);

            _trianglesL = CreateCuboidTriangles(_verticesL);
            //_trianglesR = CreateCuboidTriangles(_verticesR);

            DrawMesh(_trianglesL, Color.FromRgb(255, 0, 0));
            //DrawMesh(_trianglesR, Color.FromRgb(0, 255, 255));
            SetBitmap();
        }

        public void DrawCylinder()
        {
            ResetBitmap();
            double s = _cx * (1 / Math.Tan(theta / 2));
            e = e / 1000.0;
            P1 = new[]
            {
                new[] {s, 0, _cx, (s*e)/2},
                new[] {0, -s, Cy, 0},
                new double[] {0, 0, 0, 1},
                new double[] {0, 0, 1, 0}
            };
            P2 = new[]
            {
                new[] {s, 0, _cx, -(s*e)/2},
                new[] {0, -s, Cy, 0},
                new double[] {0, 0, 0, 1},
                new double[] {0, 0, 1, 0}
            };

            _verticesL = CreateCylinderVertices();
            _verticesR = CreateCylinderVertices();

            TransformAndProject(P1, _verticesL);
            TransformAndProject(P2, _verticesR);

            _trianglesL = CreateCylinderTriangles(_verticesL);
            _trianglesR = CreateCylinderTriangles(_verticesR);

            DrawMesh(_trianglesL, Color.FromRgb(255, 0, 0));
            DrawMesh(_trianglesR, Color.FromRgb(0, 255, 255));
            SetBitmap();
        }

        private Vertex[] CreateCuboidVertices()
        {
            var nRight = new[]
            {
                new double[] {1},
                new double[] {0},
                new double[] {0},
                new double[] {0}
            };
            var nLeft = new[]
            {
                new double[] {-1},
                new double[] {0},
                new double[] {0},
                new double[] {0}
            };
            var nTop = new[]
            {
                new double[] {0},
                new double[] {1},
                new double[] {0},
                new double[] {0}
            };
            var nBottom = new[]
            {
                new double[] {0},
                new double[] {-1},
                new double[] {0},
                new double[] {0}
            };
            var nFront = new[]
            {
                new double[] {0},
                new double[] {0},
                new double[] {1},
                new double[] {0}
            };
            var nBack = new[]
            {
                new double[] {0},
                new double[] {0},
                new double[] {-1},
                new double[] {0}
            };

            Vertex[] v = new Vertex[24];
            //front
            v[0] = new Vertex(0, 0, 0, 1);
            v[1] = new Vertex(_cuboidWidth, 0, 0, 1);
            v[2] = new Vertex(_cuboidWidth, _cuboidHeight, 0, 1);
            v[3] = new Vertex(0, _cuboidHeight, 0, 1);
            //back
            v[4] = new Vertex(0, 0, _cuboidDepth, 1);
            v[5] = new Vertex(_cuboidWidth, 0, _cuboidDepth, 1);
            v[6] = new Vertex(_cuboidWidth, _cuboidHeight, _cuboidDepth, 1);
            v[7] = new Vertex(0, _cuboidHeight, _cuboidDepth, 1);           
            //left
            v[8]=new Vertex(0,0,0,1);
            v[9]=new Vertex(0,0,_cuboidDepth,1);
            v[10]=new Vertex(0,_cuboidHeight,_cuboidDepth,1);
            v[11]=new Vertex(0,_cuboidHeight,0,1);
            //right            
            v[12]=new Vertex(_cuboidWidth,0,0,1);
            v[13]=new Vertex(_cuboidWidth,0,_cuboidDepth,1);
            v[14]=new Vertex(_cuboidWidth,_cuboidHeight,_cuboidDepth,1);
            v[15]=new Vertex(_cuboidWidth,_cuboidHeight,0,1);
            //bottom
            v[16]=new Vertex(0,0,0,1);
            v[17]=new Vertex(_cuboidWidth,0,0,1);
            v[18]=new Vertex(_cuboidWidth,0,_cuboidDepth,1);
            v[19]=new Vertex(0,0,_cuboidDepth,1);
            //top
            v[20]=new Vertex(0,_cuboidHeight,0,1);
            v[21]=new Vertex(_cuboidWidth,_cuboidHeight,0,1);
            v[22]=new Vertex(_cuboidWidth,_cuboidHeight,_cuboidDepth,1);
            v[23]=new Vertex(0,_cuboidHeight,_cuboidDepth,1);
            return v;
        }

        private Vertex[] CreateSphereVertices()
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

        private Vertex[] CreateCylinderVertices()
        {
            Vertex[] v = new Vertex[ 4*_cylinderN + 2];
            double a, b, c;
            v[0] = new Vertex(0, _cylinderH, 0, 1);
           
            for (int i = 0; i <= _cylinderN-1; i++)
            {
                a = _cylinderR * Math.Cos((2 * Math.PI * i) / _cylinderN);
                b = _cylinderH;
                c = _cylinderR * Math.Sin((2 * Math.PI * i) / _cylinderN);
                v[i+1]=new Vertex(a,b,c,1);
            }
            for (int i = 0; i <= _cylinderN - 1; i++)
            {
                a = _cylinderR * Math.Cos((2 * Math.PI * i) / _cylinderN);
                b = 0;
                c = _cylinderR * Math.Sin((2 * Math.PI * i) / _cylinderN);
                v[3*_cylinderN+i+1] = new Vertex(a, b, c, 1);
            }

            v[4 * _cylinderN + 1] = new Vertex(0, 0, 0, 1);
            for (int i = _cylinderN+1; i <= 2*_cylinderN; i++)
            {
                v[i] = v[i - _cylinderN];
            }
            

            for (int i = 2*_cylinderN + 1; i <= 3 * _cylinderN; i++)
            {
                v[i] = v[i +_cylinderN];
            }
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

        private Triangle[] CreateCuboidTriangles(Vertex[] v)
        {
            var t = new Triangle[12];
            //front
            t[0]=new Triangle(v[0],v[1],v[2]);
            t[1] = new Triangle(v[0], v[2], v[3]);
            //back
            t[2] = new Triangle(v[4], v[5], v[6]);
            t[3] = new Triangle(v[4], v[6], v[7]);
            //left
            t[4] = new Triangle(v[8], v[9], v[10]);
            t[5] = new Triangle(v[8], v[10], v[11]);
            //right
            t[6] = new Triangle(v[12], v[13], v[14]);
            t[7] = new Triangle(v[12], v[14], v[15]);

            t[8] = new Triangle(v[16], v[17], v[18]);
            t[9] = new Triangle(v[16], v[18], v[19]);

            t[10] = new Triangle(v[20], v[21], v[22]);
            t[11] = new Triangle(v[20], v[22], v[23]);
            return t;
        }

        private Triangle[] CreateSphereTriangles(Vertex[] v)
        {
            var t = new Triangle[2 * m * n];
            for (int i = 0; i < m - 1; i++)
            {
                t[i] = new Triangle(v[0], v[i + 2], v[i + 1]);
                t[2 * (n - 1) * m + i + m] = new Triangle(v[m * n + 1], v[(n - 1) * m + i + 1], v[(n - 1) * m + i + 2]);
            }
            t[m - 1] = new Triangle(v[0], v[1], v[m]);
            t[2 * (n - 1) * m + m - 1 + m] = new Triangle(v[m * n + 1], v[m * n], v[(n - 1) * m + 1]);
            for (int i = 0; i < n - 1; i++)
            {
                for (int j = 1; j < m; j++)
                {
                    t[(2 * i + 1) * m + j - 1] = new Triangle(v[i * m + j], v[i * m + j + 1], v[(i + 1) * m + j + 1]);
                    t[(2 * i + 2) * m + j - 1] = new Triangle(v[i * m + j], v[(i + 1) * m + j + 1], v[(i + 1) * m + j]);
                }
                t[(2 * i + 1) * m + m - 1] = new Triangle(v[(i + 1) * m], v[i * m + 1], v[(i + 1) * m + 1]);
                t[(2 * i + 2) * m + m - 1] = new Triangle(v[(i + 1) * m], v[(i + 1) * m + 1], v[(i + 2) * m]);
            }
            return t;
        }

        private Triangle[] CreateCylinderTriangles(Vertex[] v)
        {
            var t = new Triangle[4 * _cylinderN];
            for (int i = 0; i <= _cylinderN-2; i++)
            {
                t[i]=new Triangle(v[0],v[i+2],v[i+1]);
            }
            t[_cylinderN-1]=new Triangle(v[0],v[1],v[_cylinderN]);
            for (int i = 3*_cylinderN; i <= 4*_cylinderN - 2; i++)
            {
                t[i] = new Triangle(v[4*_cylinderN+1], v[i+1], v[i + 2]);
            }
            t[4*_cylinderN - 1] = new Triangle(v[4*_cylinderN+1], 
                v[4*_cylinderN], v[3*_cylinderN+1]);

            for (int i = _cylinderN; i <= 2 * _cylinderN - 2; i++)
            {
                t[i] = new Triangle(v[i + 1], v[i + 2], v[i + 1+_cylinderN]);
            }
            t[2 * _cylinderN - 1] = new Triangle(v[2 * _cylinderN],
                v[_cylinderN+1], v[3 * _cylinderN]);
            for (int i = 2*_cylinderN; i <= 3 * _cylinderN - 2; i++)
            {
                t[i] = new Triangle(v[i + 1], v[i + 2-_cylinderN],
                    v[i + 2]);
            }
            t[3 * _cylinderN - 1] = new Triangle(v[3 * _cylinderN],
                v[_cylinderN + 1], v[2 * _cylinderN+1]);

            return t;
        }

        private void DrawMesh(Triangle[] triangles, Color color)
        {
            foreach (var t in triangles)
            { // Check if third component of cross product is positive
                if ((t.GetV2().GetX() - t.GetV1().GetX())
                    * (t.GetV3().GetY() - t.GetV1().GetY())
                    - (t.GetV2().GetY() - t.GetV1().GetY())
                    * (t.GetV3().GetX() - t.GetV1().GetX()) > 0)
                {
                    DrawTriangle(t, color);
                }
            }
        }

        private void DrawTriangle(Triangle t, Color color)
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