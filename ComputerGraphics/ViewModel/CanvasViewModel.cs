using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using GalaSoft.MvvmLight;
using System.Windows.Media.Imaging;
using GalaSoft.MvvmLight.CommandWpf;
using ImageEditor.ViewModel.Helpers;

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
            ClearCanvasCommand=new RelayCommand(ResetBitmap);
            ResetBitmap();
        }  

        private void Click(object obj)
        {
            var e = obj as MouseButtonEventArgs;
            var p = e.GetPosition(((IInputElement) e.Source));
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
                    DDALine(point, (Point) _lastPoint);
                    break;
                case "Antialiased Line Generation":
                    WuLine(point, (Point)_lastPoint);
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
            double y = (int) p1.X < (int) p2.X ? p1.Y : p2.Y;

            int beginX;
            int endX;
            if ((int) p1.X < (int) p2.X)
            {
                beginX = (int) p1.X;
                endX = (int) p2.X;
            }
            else
            {
                beginX = (int) p2.X;
                endX = (int) p1.X;
            }

            for (int x = beginX; x <= endX; ++x)
            {
                DrawPoint(new Point(x, Round(y)), 1);
                y += m;
            }

        }

        private void WuLine(Point p1,Point p2)
        {
            //var steep = Math.Abs(y1 - y0) > Math.Abs(x1 - x0);
            //if (steep)
            //{
            //    Swap(ref x0, ref y0);
            //    Swap(ref x1, ref y1);
            //}
            //if (x0 > x1)
            //{                
            //    Swap(ref x0, ref x1);
            //    Swap(ref y0, ref y1);
            //}
            if (p1.X > p2.X)
            {
                var tmpX = p1.X;
                var tmpY = p1.Y;
                p1.X = p2.X;
                p1.Y = p2.Y;
                p2.X = tmpX;
                p2.Y = tmpY;
            }

            double dx = p2.X - p1.X;
            double dy = p2.Y - p1.Y;
            double gradient = dy / dx;
            double y = p1.Y + gradient;
            for (var x = p1.X + 1; x <= p2.X - 1; x++)
            {
                DrawPoint(new Point(x, (int)y), 1,(byte)((1 - (y - (int)y))*byte.MaxValue));
                DrawPoint(new Point(x, (int)y + 1), 1 ,(byte)((y - (int)y)*byte.MaxValue));
                y += gradient;
            }
        }

        public void Swap<T>(ref T lhs, ref T rhs)
        {
            T temp = lhs;
            lhs = rhs;
            rhs = temp;
        }

        private double Round(double y)
        {
            return y < 0 ? y - 0.5 : y + 0.5;
        }

        private void DrawPoint(Point p, int offset=1,byte intensity=0)
        {
            for (int i = -offset; i < offset; i++)
            for (int j = -offset; j < offset; j++)
            for (int k = 0; k < 3; k++)
                _pixels[(int) p.Y + j, (int) p.X + i, k] = intensity;
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