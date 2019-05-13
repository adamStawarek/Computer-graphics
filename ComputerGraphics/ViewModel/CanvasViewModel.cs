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
        public int SelectedLineSize { get; set; } = 2;
        public List<int> LineSizeValues { get; set; } = new List<int> { 2,4};
        public int SelectedCircleSize { get; set; } = 2;
        public List<int> CircleSizeValues { get; set; } = new List<int> { 2,4};
        public int SelectedThickness { get; set; } = 3;
        public List<int> ThicknessValues { get; set; } = new List<int> { 1, 3, 5, 7 };
        public int SelectedMidpointCircleThickness { get; set; } = 1;
        public List<int> MidpointCircleThicknessValues { get; set; } = new List<int> { 1, 3, 5, 7 };
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
            new RasterGraphicViewModel("Antialiased Circle Generation", false),
            new RasterGraphicViewModel("Super Sampling Line",false),
            new RasterGraphicViewModel("Super Sampling Circle",false)
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
                case "Pixel copying":
                    PixelCopy(point, (Point)_lastPoint);
                    break;
                case "Super Sampling Line":
                    SuperSamplingLine(point, (Point) _lastPoint);
                    break;
                case "Super Sampling Circle":
                    SuperSamplingCircle(point, (Point)_lastPoint);
                    break;
                default:
                    break;
            }
        }

        private void SuperSamplingCircle(Point p1, Point p2)
        {
            var newBitmapWidth = (int)_bitmap.Width * SelectedCircleSize;
            var newBitmapHeight = (int)_bitmap.Height * SelectedCircleSize;

            #region set pixels
            byte[,,] _tmpPixels = new byte[newBitmapHeight, newBitmapWidth, 4];
            for (int row = 0; row < newBitmapHeight; row++)
            {
                for (int col = 0; col < newBitmapWidth; col++)
                {
                    for (int i = 0; i < 3; i++)
                    {

                        _tmpPixels[row, col, i] = CanvasColor;
                    }

                    _tmpPixels[row, col, 3] = byte.MaxValue;
                }
            }
            #endregion
            #region set bitmap
            var tmp = new WriteableBitmap(
                newBitmapWidth, newBitmapHeight, 96, 96, PixelFormats.Bgra32, null);
            // Copy the data into a one-dimensional array.
            byte[] pixels1d = new byte[newBitmapHeight * newBitmapWidth * 4];
            int index = 0;
            for (int row = 0; row < newBitmapHeight; row++)
            {
                for (int col = 0; col < newBitmapWidth; col++)
                {
                    for (int i = 0; i < 4; i++)
                        pixels1d[index++] = _tmpPixels[row, col, i];
                }
            }

            // Update writeable bitmap with the colorArray to the image.
            Int32Rect rect = new Int32Rect(0, 0, BitmapWidth, BitmapHeight);
            _stride = 4 * ((BitmapWidth * tmp.Format.BitsPerPixel + 31) / 32);
            tmp.WritePixels(rect, pixels1d, _stride, 0);
            #endregion

            var newP1 = new Point(p1.X * SelectedCircleSize, p1.Y * SelectedCircleSize);
            var newP2 = new Point(p2.X * SelectedCircleSize, p2.Y * SelectedCircleSize);

            #region draw circle
            var radius = (int)Math.Sqrt(Math.Pow(newP1.X - newP2.X, 2) + Math.Pow(newP1.Y - newP2.Y, 2));
            int centerX = (int)newP2.X;
            int centerY = (int)newP2.Y;

            int dE = 3;
            int dSE = 5 - 2 * radius;
            int d = 1 - radius;
            int x = 0;
            int y = radius;
            if (centerX + x >= 0 && centerX + x <= tmp.Width - 1 && centerY + y >= 0 && centerY + y <= tmp.Height - 1) DrawPoint(new Point(centerX + x, centerY + y), _tmpPixels, SelectedCircleSize/2);
            if (centerX + x >= 0 && centerX + x <= tmp.Width - 1 && centerY - y >= 0 && centerY - y <= tmp.Height - 1) DrawPoint(new Point(centerX + x, centerY - y), _tmpPixels, SelectedCircleSize / 2);
            if (centerX - x >= 0 && centerX - x <= tmp.Width - 1 && centerY + y >= 0 && centerY + y <= tmp.Height - 1) DrawPoint(new Point(centerX - x, centerY + y), _tmpPixels, SelectedCircleSize / 2);
            if (centerX - x >= 0 && centerX - x <= tmp.Width - 1 && centerY - y >= 0 && centerY - y <= tmp.Height - 1) DrawPoint(new Point(centerX - x, centerY - y), _tmpPixels, SelectedCircleSize / 2);
            if (centerX + y >= 0 && centerX + y <= tmp.Width - 1 && centerY + x >= 0 && centerY + x <= tmp.Height - 1) DrawPoint(new Point(centerX + y, centerY + x), _tmpPixels, SelectedCircleSize / 2);
            if (centerX + y >= 0 && centerX + y <= tmp.Width - 1 && centerY - x >= 0 && centerY - x <= tmp.Height - 1) DrawPoint(new Point(centerX + y, centerY - x), _tmpPixels, SelectedCircleSize / 2);
            if (centerX - y >= 0 && centerX - y <= tmp.Width - 1 && centerY + x >= 0 && centerY + x <= tmp.Height - 1) DrawPoint(new Point(centerX - y, centerY + x), _tmpPixels, SelectedCircleSize / 2);
            if (centerX - y >= 0 && centerX - y <= tmp.Width - 1 && centerY - x >= 0 && centerY - x <= tmp.Height - 1) DrawPoint(new Point(centerX - y, centerY - x), _tmpPixels, SelectedCircleSize / 2);
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
                if (centerX + x >= 0 && centerX + x <= tmp.Width - 1 && centerY + y >= 0 && centerY + y <= tmp.Height - 1) DrawPoint(new Point(centerX + x, centerY + y), _tmpPixels, SelectedCircleSize / 2);
                if (centerX + x >= 0 && centerX + x <= tmp.Width - 1 && centerY - y >= 0 && centerY - y <= tmp.Height - 1) DrawPoint(new Point(centerX + x, centerY - y), _tmpPixels, SelectedCircleSize/2);
                if (centerX - x >= 0 && centerX - x <= tmp.Width - 1 && centerY + y >= 0 && centerY + y <= tmp.Height - 1) DrawPoint(new Point(centerX - x, centerY + y), _tmpPixels, SelectedCircleSize / 2);
                if (centerX - x >= 0 && centerX - x <= tmp.Width - 1 && centerY - y >= 0 && centerY - y <= tmp.Height - 1) DrawPoint(new Point(centerX - x, centerY - y), _tmpPixels, SelectedCircleSize / 2);
                if (centerX + y >= 0 && centerX + y <= tmp.Width - 1 && centerY + x >= 0 && centerY + x <= tmp.Height - 1) DrawPoint(new Point(centerX + y, centerY + x), _tmpPixels, SelectedCircleSize / 2);
                if (centerX + y >= 0 && centerX + y <= tmp.Width - 1 && centerY - x >= 0 && centerY - x <= tmp.Height - 1) DrawPoint(new Point(centerX + y, centerY - x), _tmpPixels, SelectedCircleSize / 2);
                if (centerX - y >= 0 && centerX - y <= tmp.Width - 1 && centerY + x >= 0 && centerY + x <= tmp.Height - 1) DrawPoint(new Point(centerX - y, centerY + x), _tmpPixels, SelectedCircleSize / 2);
                if (centerX - y >= 0 && centerX - y <= tmp.Width - 1 && centerY - x >= 0 && centerY - x <= tmp.Height - 1) DrawPoint(new Point(centerX - y, centerY - x), _tmpPixels, SelectedCircleSize / 2);
            }

            #endregion         
            #region map to orginal pixels
            for (int row = 0, orgRow = 0; row < newBitmapHeight - SelectedCircleSize; row += SelectedCircleSize, orgRow++)
            {
                for (int col = 0, orgCol = 0; col < newBitmapWidth - SelectedCircleSize; col += SelectedCircleSize, orgCol++)
                {
                    int avg = 0;
                    for (int i = 0; i < SelectedCircleSize; i++)
                    {
                        for (int j = 0; j < SelectedCircleSize; j++)
                        {
                            avg += _tmpPixels[row + i, col + j, 0];
                        }
                    }

                    avg = avg / (SelectedCircleSize * SelectedCircleSize);

                    for (int i = 0; i < 3; i++)
                    {
                        _pixels[orgRow, orgCol, i] = (byte)avg;
                    }

                    _pixels[orgRow, orgCol, 3] = byte.MaxValue;
                }
            }


            #endregion
        }

        private void SuperSamplingLine(Point p1, Point p2)
        {
            var newBitmapWidth = (int)_bitmap.Width * SelectedLineSize;
            var newBitmapHeight = (int)_bitmap.Height * SelectedLineSize;

            #region set pixels
            byte[,,] _tmpPixels = new byte[newBitmapHeight, newBitmapWidth, 4];
            for (int row = 0; row < newBitmapHeight; row++)
            {
                for (int col = 0; col < newBitmapWidth; col++)
                {
                    for (int i = 0; i < 3; i++)
                    {

                        _tmpPixels[row, col, i] = CanvasColor;
                    }

                    _tmpPixels[row, col, 3] = byte.MaxValue;
                }
            }
            #endregion
            #region set bitmap
            var tmp = new WriteableBitmap(
                newBitmapWidth, newBitmapHeight, 96, 96, PixelFormats.Bgra32, null);
            // Copy the data into a one-dimensional array.
            byte[] pixels1d = new byte[newBitmapHeight * newBitmapWidth * 4];
            int index = 0;
            for (int row = 0; row < newBitmapHeight; row++)
            {
                for (int col = 0; col < newBitmapWidth; col++)
                {
                    for (int i = 0; i < 4; i++)
                        pixels1d[index++] = _tmpPixels[row, col, i];
                }
            }

            // Update writeable bitmap with the colorArray to the image.
            Int32Rect rect = new Int32Rect(0, 0, BitmapWidth, BitmapHeight);
            _stride = 4 * ((BitmapWidth * tmp.Format.BitsPerPixel + 31) / 32);
            tmp.WritePixels(rect, pixels1d, _stride, 0);
            #endregion

            var newP1 = new Point(p1.X * SelectedLineSize, p1.Y * SelectedLineSize);
            var newP2 = new Point(p2.X * SelectedLineSize, p2.Y * SelectedLineSize);

            #region draw line
            double dy = newP2.Y - newP1.Y;
            double dx = newP2.X - newP1.X;
            double m = dy / dx;

            if (Math.Abs(m) < 1)//x is increasing more than y
            {
                double y = (int)newP1.X < (int)newP2.X ? newP1.Y : newP2.Y;
                int beginX;
                int endX;
                if ((int)newP1.X < (int)newP2.X)
                {
                    beginX = (int)newP1.X;
                    endX = (int)newP2.X;
                }
                else
                {
                    beginX = (int)newP2.X;
                    endX = (int)newP1.X;
                }

                for (int x = beginX; x <= endX; ++x)
                {
                    DrawPoint(new Point(x, y),_tmpPixels, SelectedLineSize/2);
                    y += m;
                }
            }
            else
            {
                double x = (int)newP1.Y < (int)newP2.Y ? newP1.X : newP2.X;
                int beginY;
                int endY;
                if ((int)newP1.Y < (int)newP2.Y)
                {
                    beginY = (int)newP1.Y;
                    endY = (int)newP2.Y;
                }
                else
                {
                    beginY = (int)newP2.Y;
                    endY = (int)newP1.Y;
                }

                for (int y = beginY; y <= endY; ++y)
                {
                    DrawPoint(new Point(x, y), _tmpPixels, SelectedLineSize / 2);
                    x += 1 / m;
                }
            }


            #endregion
            #region map to orginal pixels
            for (int row = 0,orgRow=0; row < newBitmapHeight-SelectedLineSize; row+=SelectedLineSize,orgRow++)
            {
                for (int col = 0,orgCol=0; col < newBitmapWidth - SelectedLineSize; col+=SelectedLineSize,orgCol++)
                {
                    int avg = 0;
                    for (int i = 0; i < SelectedLineSize; i++)
                    {
                        for (int j = 0; j < SelectedLineSize; j++)
                        {
                            avg+=_tmpPixels[row+i, col+j, 0];
                        }
                    }

                    avg = avg / (SelectedLineSize*SelectedLineSize);

                    for (int i = 0; i < 3; i++)
                    {
                        _pixels[orgRow, orgCol, i] = (byte)avg;
                    }

                    _pixels[orgRow, orgCol, 3] = byte.MaxValue;
                }
            }


            #endregion
        }

        private void DrawPoint(Point p,byte[,,] pixels, int offset = 1, byte intensity = 0)
        {
            if (offset == 0)
            {
                for (int k = 0; k < 3; k++)
                    pixels[(int)p.Y, (int)p.X, k] = intensity;
                return;
            }

            for (int i = -offset; i < offset; i++)
            for (int j = -offset; j < offset; j++)
            for (int k = 0; k < 3; k++)
                if((int)p.Y+j<pixels.GetLength(0)&&(int)p.Y+j>0&& (int)p.X + i < pixels.GetLength(1) && (int)p.X + i > 0)
                pixels[(int)p.Y + j, (int)p.X + i, k] = intensity;
        }

        private void PixelCopy(Point p1, Point p2)
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
                    for (int i = (SelectedThickness - 1) / -2; i <= (SelectedThickness - 1) / 2; i++)
                    {
                        DrawPoint(new Point(x, y+i), 0);
                    }

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
                    for (int i = (SelectedThickness - 1) / -2; i <= (SelectedThickness - 1) / 2; i++)
                    {
                        DrawPoint(new Point(x+i, y), 0);
                    }

                    x += 1 / m;
                }
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
            var dict=new Dictionary<int,int>(){{1,0},{3,1},{5,2},{7,3}};
            if (centerX + x >= 0 && centerX + x <= _bitmap.Width - 1 && centerY + y >= 0 && centerY + y <= _bitmap.Height - 1) DrawPoint(new Point(centerX + x, centerY + y), dict[SelectedMidpointCircleThickness]);
            if (centerX + x >= 0 && centerX + x <= _bitmap.Width - 1 && centerY - y >= 0 && centerY - y <= _bitmap.Height - 1) DrawPoint(new Point(centerX + x, centerY - y), dict[SelectedMidpointCircleThickness]);
            if (centerX - x >= 0 && centerX - x <= _bitmap.Width - 1 && centerY + y >= 0 && centerY + y <= _bitmap.Height - 1) DrawPoint(new Point(centerX - x, centerY + y), dict[SelectedMidpointCircleThickness]);
            if (centerX - x >= 0 && centerX - x <= _bitmap.Width - 1 && centerY - y >= 0 && centerY - y <= _bitmap.Height - 1) DrawPoint(new Point(centerX - x, centerY - y), dict[SelectedMidpointCircleThickness]);
            if (centerX + y >= 0 && centerX + y <= _bitmap.Width - 1 && centerY + x >= 0 && centerY + x <= _bitmap.Height - 1) DrawPoint(new Point(centerX + y, centerY + x), dict[SelectedMidpointCircleThickness]);
            if (centerX + y >= 0 && centerX + y <= _bitmap.Width - 1 && centerY - x >= 0 && centerY - x <= _bitmap.Height - 1) DrawPoint(new Point(centerX + y, centerY - x), dict[SelectedMidpointCircleThickness]);
            if (centerX - y >= 0 && centerX - y <= _bitmap.Width - 1 && centerY + x >= 0 && centerY + x <= _bitmap.Height - 1) DrawPoint(new Point(centerX - y, centerY + x), dict[SelectedMidpointCircleThickness]);
            if (centerX - y >= 0 && centerX - y <= _bitmap.Width - 1 && centerY - x >= 0 && centerY - x <= _bitmap.Height - 1) DrawPoint(new Point(centerX - y, centerY - x), dict[SelectedMidpointCircleThickness]);
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
                if (centerX + x >= 0 && centerX + x <= _bitmap.Width - 1 && centerY + y >= 0 && centerY + y <= _bitmap.Height - 1) DrawPoint(new Point(centerX + x, centerY + y), dict[SelectedMidpointCircleThickness]);
                if (centerX + x >= 0 && centerX + x <= _bitmap.Width - 1 && centerY - y >= 0 && centerY - y <= _bitmap.Height - 1) DrawPoint(new Point(centerX + x, centerY - y), dict[SelectedMidpointCircleThickness]);
                if (centerX - x >= 0 && centerX - x <= _bitmap.Width - 1 && centerY + y >= 0 && centerY + y <= _bitmap.Height - 1) DrawPoint(new Point(centerX - x, centerY + y), dict[SelectedMidpointCircleThickness]);
                if (centerX - x >= 0 && centerX - x <= _bitmap.Width - 1 && centerY - y >= 0 && centerY - y <= _bitmap.Height - 1) DrawPoint(new Point(centerX - x, centerY - y), dict[SelectedMidpointCircleThickness]);
                if (centerX + y >= 0 && centerX + y <= _bitmap.Width - 1 && centerY + x >= 0 && centerY + x <= _bitmap.Height - 1) DrawPoint(new Point(centerX + y, centerY + x), dict[SelectedMidpointCircleThickness]);
                if (centerX + y >= 0 && centerX + y <= _bitmap.Width - 1 && centerY - x >= 0 && centerY - x <= _bitmap.Height - 1) DrawPoint(new Point(centerX + y, centerY - x), dict[SelectedMidpointCircleThickness]);
                if (centerX - y >= 0 && centerX - y <= _bitmap.Width - 1 && centerY + x >= 0 && centerY + x <= _bitmap.Height - 1) DrawPoint(new Point(centerX - y, centerY + x), dict[SelectedMidpointCircleThickness]);
                if (centerX - y >= 0 && centerX - y <= _bitmap.Width - 1 && centerY - x >= 0 && centerY - x <= _bitmap.Height - 1) DrawPoint(new Point(centerX - y, centerY - x), dict[SelectedMidpointCircleThickness]);
            }

        }

        private void WuLine(Point p1, Point p2)
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

        private void WuCircle(Point p1, Point p2)
        {
            var radius = (int)Math.Sqrt(Math.Pow(p1.X - p2.X, 2) + Math.Pow(p1.Y - p2.Y, 2));
            int centerX = (int)p2.X;
            int centerY = (int)p2.Y;

            byte L = 0;
            byte B = CanvasColor; /*Background Color*/
            int x = radius;
            int y = 0;
            if (centerX + x >= 0 && centerX + x <= _bitmap.Width - 1 && centerY + y >= 0 && centerY + y <= _bitmap.Height - 1) DrawPoint(new Point(centerX + x, centerY + y), 0, L);
            if (centerX + x >= 0 && centerX + x <= _bitmap.Width - 1 && centerY - y >= 0 && centerY - y <= _bitmap.Height - 1) DrawPoint(new Point(centerX + x, centerY - y), 0, L);
            if (centerX - x >= 0 && centerX - x <= _bitmap.Width - 1 && centerY + y >= 0 && centerY + y <= _bitmap.Height - 1) DrawPoint(new Point(centerX - x, centerY + y), 0, L);
            if (centerX - x >= 0 && centerX - x <= _bitmap.Width - 1 && centerY - y >= 0 && centerY - y <= _bitmap.Height - 1) DrawPoint(new Point(centerX - x, centerY - y), 0, L);
            if (centerX + y >= 0 && centerX + y <= _bitmap.Width - 1 && centerY + x >= 0 && centerY + x <= _bitmap.Height - 1) DrawPoint(new Point(centerX + y, centerY + x), 0, L);
            if (centerX + y >= 0 && centerX + y <= _bitmap.Width - 1 && centerY - x >= 0 && centerY - x <= _bitmap.Height - 1) DrawPoint(new Point(centerX + y, centerY - x), 0, L);
            if (centerX - y >= 0 && centerX - y <= _bitmap.Width - 1 && centerY + x >= 0 && centerY + x <= _bitmap.Height - 1) DrawPoint(new Point(centerX - y, centerY + x), 0, L);
            if (centerX - y >= 0 && centerX - y <= _bitmap.Width - 1 && centerY - x >= 0 && centerY - x <= _bitmap.Height - 1) DrawPoint(new Point(centerX - y, centerY - x), 0, L);
            while (x > y)
            {
                ++y;
                x = (int)Math.Ceiling(Math.Sqrt(radius * radius - y * y));
                float T = (float)(x - Math.Sqrt(radius * radius - y * y));
                var c2 = (byte)(L * (1 - T) + B * T);
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