using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using GalaSoft.MvvmLight;
using System.Windows.Media.Imaging;
using GalaSoft.MvvmLight.CommandWpf;

namespace ImageEditor.ViewModel
{
    public class CanvasViewModel:ViewModelBase
    {
        private WriteableBitmap _bitmap;
        private const int BitmapWidth = 780;
        private const int BitmapHeight = 800;
        private readonly byte[,,] _pixels = new byte[BitmapHeight, BitmapWidth, 4];
        private int _stride;
        public RelayCommand<object> ClickCommand { get; private set; }

        public WriteableBitmap Bitmap
        {
            get => _bitmap;
            set
            {
                _bitmap = value;
                RaisePropertyChanged("Bitmap");
            }
        }

        public CanvasViewModel()
        {
            ClickCommand = new RelayCommand<object>(Click);
            InitializeBitmap();
        }

        private void Click(object obj)
        {
            var e = obj as MouseButtonEventArgs;
            System.Windows.Point p = e.GetPosition(((IInputElement)e.Source));
             var XY = $"X: {(int) p.X} Y:{(int) p.Y}";
            MessageBox.Show(XY);
            for(int i=-10;i<10;i++)
                for (int j = -10; j < 10; j++)
                    for (int k = 0; k < 3; k++)
                        _pixels[(int)p.Y+j, (int)p.X+i, k] = 0;
            SetBitmap();
           
        }

        private void InitializeBitmap()
        {
            // Clear to black.
            for (int row = 0; row < BitmapHeight; row++)
            {
                for (int col = 0; col < BitmapWidth; col++)
                {
                    for (int i = 0; i < 3; i++)
                        _pixels[row, col, i] = Byte.MaxValue-10;
                    _pixels[row, col, 3] = Byte.MaxValue;
                }
            }
            SetBitmap();
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
    }
}
