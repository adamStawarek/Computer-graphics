using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.CommandWpf;
using ImageEditor.ViewModel.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace ImageEditor.ViewModel
{
    public class CanvasViewModel : ViewModelBase
    {
        #region private fields
        private const int CanvasColor = byte.MaxValue;
        private WriteableBitmap _bitmap;
        private const int BitmapWidth = 1000;
        private const int BitmapHeight = 1000;
        private readonly byte[,,] _pixels = new byte[BitmapHeight, BitmapWidth, 4];
        private int _stride;
        private Point? _lastPoint;
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
        public List<RasterGraphicViewModel> RasterGraphics { get; set; } = new List<RasterGraphicViewModel>
        {
            new RasterGraphicViewModel("Digital Differential Analyzer", true),
            new RasterGraphicViewModel("Midpoint circle", false),
            new RasterGraphicViewModel("Pixel copying", false),
            new RasterGraphicViewModel("Antialiased Line Generation", false),
            new RasterGraphicViewModel("Antialiased Circle Generation", false)
        };
        #endregion

        public CanvasViewModel()
        {
            ClickCommand = new RelayCommand<object>(Click);
            ClearCanvasCommand = new RelayCommand(ResetBitmap);
            ResetBitmap();
        }

        private void Click(object obj)
        {
            var e = obj as MouseButtonEventArgs;
            var p = e.GetPosition(((IInputElement)e.Source));
            DrawPoint(p, 3);
            if (_lastPoint == null)
                _lastPoint = p;
            else
            {
                DrawShape(p);

                _lastPoint = null;
            }
            SetBitmap();
        }

        private void DrawShape(Point point)
        {
            var rasterGraphic = RasterGraphics.FirstOrDefault(r => r.IsSelected);
            switch (rasterGraphic?.Type)
            {
                case "Digital Differential Analyzer":
                    DDALine(point, (Point)_lastPoint);
                    break;
                case "Antialiased Line Generation":
                    WuLine(point, (Point)_lastPoint);
                    break;
                case "Antialiased Circle Generation":
                    WuCircle(point, (Point)_lastPoint);
                    break;
                case "Midpoint circle":
                    MidpointCircleV2(point, (Point)_lastPoint);
                    break;
                default:
                    break;
            }
        }

        private void DDALine(Point p1, Point p2)
        {
            double dy = p2.Y - p1.Y;
            double dx = p2.X - p1.X;
            double m = dy / dx;


            if (Math.Abs(m) < 1)//x is increasing more than y
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

                for (int x = beginX; x <= endX; ++x)
                {
                    DrawPoint(new Point(x, y), 0);
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

                for (int y = beginY; y <= endY; ++y)
                {
                    DrawPoint(new Point(x, y), 0);
                    x += 1 / m;
                }
            }

        }

        private void MidpointCircleV2(Point p1, Point p2)
        {
            var radius = (int)Math.Sqrt(Math.Pow(p1.X - p2.X, 2) + Math.Pow(p1.Y - p2.Y, 2));
            int centerX = (int)p2.X;
            int centerY = (int)p2.Y;

            int dE = 3;
            int dSE = 5 - 2 * radius;
            int d = 1 - radius;
            int x = 0;
            int y = radius;
            if (centerX + x >= 0 && centerX + x <= _bitmap.Width - 1 && centerY + y >= 0 && centerY + y <= _bitmap.Height - 1) DrawPoint(new Point(centerX + x, centerY + y), 0);
            if (centerX + x >= 0 && centerX + x <= _bitmap.Width - 1 && centerY - y >= 0 && centerY - y <= _bitmap.Height - 1) DrawPoint(new Point(centerX + x, centerY - y), 0);
            if (centerX - x >= 0 && centerX - x <= _bitmap.Width - 1 && centerY + y >= 0 && centerY + y <= _bitmap.Height - 1) DrawPoint(new Point(centerX - x, centerY + y), 0);
            if (centerX - x >= 0 && centerX - x <= _bitmap.Width - 1 && centerY - y >= 0 && centerY - y <= _bitmap.Height - 1) DrawPoint(new Point(centerX - x, centerY - y), 0);
            if (centerX + y >= 0 && centerX + y <= _bitmap.Width - 1 && centerY + x >= 0 && centerY + x <= _bitmap.Height - 1) DrawPoint(new Point(centerX + y, centerY + x), 0);
            if (centerX + y >= 0 && centerX + y <= _bitmap.Width - 1 && centerY - x >= 0 && centerY - x <= _bitmap.Height - 1) DrawPoint(new Point(centerX + y, centerY - x), 0);
            if (centerX - y >= 0 && centerX - y <= _bitmap.Width - 1 && centerY + x >= 0 && centerY + x <= _bitmap.Height - 1) DrawPoint(new Point(centerX - y, centerY + x), 0);
            if (centerX - y >= 0 && centerX - y <= _bitmap.Width - 1 && centerY - x >= 0 && centerY - x <= _bitmap.Height - 1) DrawPoint(new Point(centerX - y, centerY - x), 0);
            while (y > x)
            {
                if (d < 0) //move to E
                {
                    d += dE;
                    dE += 2;
                    dSE += 2;
                }
                else //move to SE
                {
                    d += dSE;
                    dE += 2;
                    dSE += 4;
                    --y;
                }
                ++x;
                if (centerX + x >= 0 && centerX + x <= _bitmap.Width - 1 && centerY + y >= 0 && centerY + y <= _bitmap.Height - 1) DrawPoint(new Point(centerX + x, centerY + y), 0);
                if (centerX + x >= 0 && centerX + x <= _bitmap.Width - 1 && centerY - y >= 0 && centerY - y <= _bitmap.Height - 1) DrawPoint(new Point(centerX + x, centerY - y), 0);
                if (centerX - x >= 0 && centerX - x <= _bitmap.Width - 1 && centerY + y >= 0 && centerY + y <= _bitmap.Height - 1) DrawPoint(new Point(centerX - x, centerY + y), 0);
                if (centerX - x >= 0 && centerX - x <= _bitmap.Width - 1 && centerY - y >= 0 && centerY - y <= _bitmap.Height - 1) DrawPoint(new Point(centerX - x, centerY - y), 0);
                if (centerX + y >= 0 && centerX + y <= _bitmap.Width - 1 && centerY + x >= 0 && centerY + x <= _bitmap.Height - 1) DrawPoint(new Point(centerX + y, centerY + x), 0);
                if (centerX + y >= 0 && centerX + y <= _bitmap.Width - 1 && centerY - x >= 0 && centerY - x <= _bitmap.Height - 1) DrawPoint(new Point(centerX + y, centerY - x), 0);
                if (centerX - y >= 0 && centerX - y <= _bitmap.Width - 1 && centerY + x >= 0 && centerY + x <= _bitmap.Height - 1) DrawPoint(new Point(centerX - y, centerY + x), 0);
                if (centerX - y >= 0 && centerX - y <= _bitmap.Width - 1 && centerY - x >= 0 && centerY - x <= _bitmap.Height - 1) DrawPoint(new Point(centerX - y, centerY - x), 0);
            }

        }

        private void WuLine(Point p1, Point p2)
        {
            if (p1.X > p2.X)
            {
                var tmpX = p1.X;
                var tmpY = p1.Y;
                p1.X = p2.X;
                p1.Y = p2.Y;
                p2.X = tmpX;
                p2.Y = tmpY;
            }

            byte L = 0;
            byte B = CanvasColor; /*Background Color*/
            double dx = p2.X - p1.X;
            double dy = p2.Y - p1.Y;
            double m = dy / dx;
            if (Math.Abs(m) < 1)
            {
                double y = p1.Y;
                for (var x = p1.X; x <= p2.X; ++x)
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
                double x = p1.X;
                for (var y = p1.Y; y <= p2.Y; ++y)
                {
                    var c1 = L * (1 - (x - (int)x)) + B * (x - (int)x);
                    var c2 = L * (x - (int)x) + B * (1 - (x - (int)x));
                    DrawPoint(new Point(Math.Floor(x),y), 0, (byte)c1);
                    DrawPoint(new Point(Math.Floor(x) + 1,y), 0, (byte)c2);
                    x += 1/m;
                }
            }            
        }

        private void WuCircle(Point p1, Point p2)
        {
            var radius = (int)Math.Sqrt(Math.Pow(p1.X - p2.X, 2) + Math.Pow(p1.Y - p2.Y, 2));
            int centerX = (int)p2.X;
            int centerY = (int)p2.Y;

            byte L = 0;
            byte B = CanvasColor; /*Background Color*/
            int x = radius;
            int y = 0;
            if (centerX + x >= 0 && centerX + x <= _bitmap.Width - 1 && centerY + y >= 0 && centerY + y <= _bitmap.Height - 1) DrawPoint(new Point(centerX + x, centerY + y), 0,L);
            if (centerX + x >= 0 && centerX + x <= _bitmap.Width - 1 && centerY - y >= 0 && centerY - y <= _bitmap.Height - 1) DrawPoint(new Point(centerX + x, centerY - y), 0,L);
            if (centerX - x >= 0 && centerX - x <= _bitmap.Width - 1 && centerY + y >= 0 && centerY + y <= _bitmap.Height - 1) DrawPoint(new Point(centerX - x, centerY + y), 0,L);
            if (centerX - x >= 0 && centerX - x <= _bitmap.Width - 1 && centerY - y >= 0 && centerY - y <= _bitmap.Height - 1) DrawPoint(new Point(centerX - x, centerY - y), 0,L);
            if (centerX + y >= 0 && centerX + y <= _bitmap.Width - 1 && centerY + x >= 0 && centerY + x <= _bitmap.Height - 1) DrawPoint(new Point(centerX + y, centerY + x), 0,L);
            if (centerX + y >= 0 && centerX + y <= _bitmap.Width - 1 && centerY - x >= 0 && centerY - x <= _bitmap.Height - 1) DrawPoint(new Point(centerX + y, centerY - x), 0,L);
            if (centerX - y >= 0 && centerX - y <= _bitmap.Width - 1 && centerY + x >= 0 && centerY + x <= _bitmap.Height - 1) DrawPoint(new Point(centerX - y, centerY + x), 0,L);
            if (centerX - y >= 0 && centerX - y <= _bitmap.Width - 1 && centerY - x >= 0 && centerY - x <= _bitmap.Height - 1) DrawPoint(new Point(centerX - y, centerY - x), 0,L);
            while (x > y)
            {
                ++y;
                x = (int)Math.Ceiling(Math.Sqrt(radius * radius - y * y));
                float T =(float)(x- Math.Sqrt(radius * radius - y * y));
                var c2 =(byte)(L * (1 - T) + B * T);
                var c1 = (byte)(L * T + B * (1 - T));

                if (centerX + x >= 0 && centerX + x <= _bitmap.Width - 1 && centerY + y >= 0 && centerY + y <= _bitmap.Height - 1) DrawPoint(new Point(centerX + x, centerY + y), 0, c2);
                if (centerX + x >= 0 && centerX + x <= _bitmap.Width - 1 && centerY - y >= 0 && centerY - y <= _bitmap.Height - 1) DrawPoint(new Point(centerX + x, centerY - y), 0, c2);
                if (centerX - x >= 0 && centerX - x <= _bitmap.Width - 1 && centerY + y >= 0 && centerY + y <= _bitmap.Height - 1) DrawPoint(new Point(centerX - x, centerY + y), 0, c2);
                if (centerX - x >= 0 && centerX - x <= _bitmap.Width - 1 && centerY - y >= 0 && centerY - y <= _bitmap.Height - 1) DrawPoint(new Point(centerX - x, centerY - y), 0, c2);
                if (centerX + y >= 0 && centerX + y <= _bitmap.Width - 1 && centerY + x >= 0 && centerY + x <= _bitmap.Height - 1) DrawPoint(new Point(centerX + y, centerY + x), 0, c2);
                if (centerX + y >= 0 && centerX + y <= _bitmap.Width - 1 && centerY - x >= 0 && centerY - x <= _bitmap.Height - 1) DrawPoint(new Point(centerX + y, centerY - x), 0, c2);
                if (centerX - y >= 0 && centerX - y <= _bitmap.Width - 1 && centerY + x >= 0 && centerY + x <= _bitmap.Height - 1) DrawPoint(new Point(centerX - y, centerY + x), 0, c2);
                if (centerX - y >= 0 && centerX - y <= _bitmap.Width - 1 && centerY - x >= 0 && centerY - x <= _bitmap.Height - 1) DrawPoint(new Point(centerX - y, centerY - x), 0, c2);

                if (centerX + x-1 >= 0 && centerX + x-1 <= _bitmap.Width - 1 && centerY + y >= 0 && centerY + y <= _bitmap.Height - 1) DrawPoint(new Point(centerX + x-1, centerY + y), 0, c1);
                if (centerX + x-1 >= 0 && centerX + x-1 <= _bitmap.Width - 1 && centerY - y >= 0 && centerY - y <= _bitmap.Height - 1) DrawPoint(new Point(centerX + x-1, centerY - y), 0, c1);
                if (centerX - x-1 >= 0 && centerX - x-1 <= _bitmap.Width - 1 && centerY + y >= 0 && centerY + y <= _bitmap.Height - 1) DrawPoint(new Point(centerX - x-1, centerY + y), 0, c1);
                if (centerX - x-1 >= 0 && centerX - x-1 <= _bitmap.Width - 1 && centerY - y >= 0 && centerY - y <= _bitmap.Height - 1) DrawPoint(new Point(centerX - x-1, centerY - y), 0, c1);
                if (centerX + y >= 0 && centerX + y <= _bitmap.Width - 1 && centerY + x-1 >= 0 && centerY + x-1 <= _bitmap.Height - 1) DrawPoint(new Point(centerX + y, centerY + x-1), 0, c1);
                if (centerX + y >= 0 && centerX + y <= _bitmap.Width - 1 && centerY - x-1 >= 0 && centerY - x-1 <= _bitmap.Height - 1) DrawPoint(new Point(centerX + y, centerY - x-1), 0, c1);
                if (centerX - y >= 0 && centerX - y <= _bitmap.Width - 1 && centerY + x-1 >= 0 && centerY + x-1 <= _bitmap.Height - 1) DrawPoint(new Point(centerX - y, centerY + x-1), 0, c1);
                if (centerX - y >= 0 && centerX - y <= _bitmap.Width - 1 && centerY - x-1 >= 0 && centerY - x-1 <= _bitmap.Height - 1) DrawPoint(new Point(centerX - y, centerY - x-1), 0, c1);               
            }


        }

        private void DrawPoint(Point p, int offset = 1, byte intensity = 0)
        {
            if (offset == 0)
            {
                for (int k = 0; k < 3; k++)
                    _pixels[(int)p.Y, (int)p.X, k] = intensity;
                return;
            }

            for (int i = -offset; i < offset; i++)
                for (int j = -offset; j < offset; j++)
                    for (int k = 0; k < 3; k++)
                        _pixels[(int)p.Y + j, (int)p.X + i, k] = intensity;
        }

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