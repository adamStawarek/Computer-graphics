using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.CommandWpf;
using ImageEditor.Filters;
using ImageEditor.Filters.Interfaces;
using ImageEditor.ViewModel.Helpers;
using Microsoft.Win32;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using MedianFilter = ImageEditor.Filters.MedianFilter;

namespace ImageEditor.ViewModel
{
    public class MainViewModel : ViewModelBase
    {
        private List<FiltersListViewItem> AllFilters = new List<FiltersListViewItem>
        {
            new FiltersListViewItem(new Brightness()),
            new FiltersListViewItem(new ColorInversion()),
            new FiltersListViewItem(new Contrast()),
            new FiltersListViewItem(new DiagonalEdgeDetection()),
            new FiltersListViewItem(new GaussianSmoothing()),
            new FiltersListViewItem(new Sharpen()),
            new FiltersListViewItem(new Blur()),
            new FiltersListViewItem(new Emboss()),
            new FiltersListViewItem(new GammaCorrection()),
            new FiltersListViewItem(new MedianFilter())

        };

        public static MainViewModel Instance { get; set; } //used to update Bitamp, in other View Models
        private Bitmap _orginBitmap;

        public ICollectionView FunctionalFiltersView { get; set; }
        public ICollectionView ConvolutionFiltersView { get; set; }

        private Visibility _spinnerVisibility = Visibility.Hidden;
        public Visibility SpinnerVisibility
        {
            get => _spinnerVisibility;
            set
            {
                _spinnerVisibility = value;
                RaisePropertyChanged("SpinnerVisibility");
            }
        }

        private Bitmap _bitmap;
        public Bitmap Bitmap
        {
            get => _bitmap;
            set
            {
                _bitmap = value;
                RaisePropertyChanged("Bitmap");
            }

        }

        private string _rgbVal;
        public string RgbVal
        {
            get => _rgbVal;
            set
            {
                _rgbVal = value;
                RaisePropertyChanged("RgbVal");
            }
        }

        #region relay commands

        public RelayCommand OpenFileCommand { get; private set; }
        public RelayCommand SaveFileCommand { get; private set; }

        public RelayCommand ClearFiltersCommand { get; private set; }
        public RelayCommand<object> ApplyFilterCommand { get; private set; }
        public RelayCommand<object> DropFilesCommand { get; private set; }
        public RelayCommand<object> SetProjectionCommand { get; set; }
        public RelayCommand<object> OpenPopupCommand { get; private set; }

        #endregion

        public MainViewModel()
        {
            Instance = this;

            FunctionalFiltersView = CollectionViewSource.GetDefaultView(AllFilters.Where(FunctionalFilter));
            ConvolutionFiltersView = CollectionViewSource.GetDefaultView(AllFilters.Where(ConvolutionFilter));

            OpenFileCommand = new RelayCommand(OpenFile);
            SaveFileCommand = new RelayCommand(SaveFile);
            ApplyFilterCommand = new RelayCommand<object>(ApplyFilter);
            DropFilesCommand = new RelayCommand<object>(DropFiles);
            OpenPopupCommand = new RelayCommand<object>(SetCurrentPixelValuesToRgbBox);
            ClearFiltersCommand = new RelayCommand(ClearFilters);

            foreach (var modification in AllFilters)
            {
                if (modification.Filter is IError e)
                {
                    e.ErrorOccured += delegate { modification.ErrorMessage = e.ErrorMessage; };
                    e.NoErrorOccured += delegate { modification.ErrorMessage = e.ErrorMessage; };
                }
            }
        }

        #region file opening/saving

        private void DropFiles(object p)
        {
            var e = p as DragEventArgs;
            if (e != null && !e.Data.GetDataPresent(DataFormats.FileDrop)) return;
            if (e != null)
            {
                var files = (string[])e.Data.GetData(DataFormats.FileDrop);
                if (files == null) return;
                try
                {
                    _orginBitmap = Bitmap = new Bitmap(files.FirstOrDefault());
                    //clear filters and modifications
                    AllFilters.ForEach(m => m.ApplicationCounter = 0);
                    AllFilters.ForEach(m => m.ErrorMessage = "");
                }
                catch
                {
                    return;
                }
            }
        }

        private void SaveFile()
        {
            if (Bitmap == null) return;
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.Filter = "Images|*.png;*.bmp;*.jpg";
            saveFileDialog.Title = "Save an Image File";
            saveFileDialog.ShowDialog();

            if (saveFileDialog.FileName != "")
            {
                using (System.IO.FileStream fs =
                    (System.IO.FileStream)saveFileDialog.OpenFile())
                {
                    string ext = System.IO.Path.GetExtension(saveFileDialog.FileName);
                    switch (ext)
                    {
                        case ".jpg":
                            _bitmap.Save(fs,
                                System.Drawing.Imaging.ImageFormat.Jpeg);
                            break;

                        case ".bmp":
                            _bitmap.Save(fs,
                                System.Drawing.Imaging.ImageFormat.Bmp);
                            break;
                        case ".png":
                            _bitmap.Save(fs,
                                System.Drawing.Imaging.ImageFormat.Png);
                            break;
                    }
                }
            }
        }

        private void OpenFile()
        {
            OpenFileDialog openfiledialog = new OpenFileDialog();

            openfiledialog.Title = "Open Image";
            openfiledialog.Filter = "Image File|*.bmp; *.gif; *.jpg; *.jpeg; *.png;";

            if (openfiledialog.ShowDialog() == true)
            {
                _orginBitmap = Bitmap = new Bitmap(openfiledialog.FileName);
                //clear filters and modifications
                AllFilters.ForEach(m => m.ApplicationCounter = 0);
                AllFilters.ForEach(m => m.ErrorMessage = "");
            }
        }

        #endregion

        private bool FunctionalFilter(FiltersListViewItem arg)
        {
            return
                   arg.Filter.GetType().IsEquivalentTo(typeof(Brightness)) ||
                   arg.Filter.GetType().IsEquivalentTo(typeof(ColorInversion)) ||
                   arg.Filter.GetType().IsEquivalentTo(typeof(Contrast)) ||
                   arg.Filter.GetType().IsEquivalentTo(typeof(GammaCorrection));

        }

        private bool ConvolutionFilter(FiltersListViewItem arg)
        {
            return
                   arg.Filter.GetType().IsEquivalentTo(typeof(GaussianSmoothing)) ||
                   arg.Filter.GetType().IsEquivalentTo(typeof(Sharpen)) ||
                   arg.Filter.GetType().IsEquivalentTo(typeof(Blur)) ||
                   arg.Filter.GetType().IsEquivalentTo(typeof(Emboss)) ||
                   arg.Filter.GetType().IsEquivalentTo(typeof(DiagonalEdgeDetection))||
                   arg.Filter.GetType().IsEquivalentTo(typeof(MedianFilter));

        }

        private void SetCurrentPixelValuesToRgbBox(object obj)
        {
            var color = ColorUnderCursor.Get();
            POINT p;
            ColorUnderCursor.GetCursorPos(out p);
            RgbVal = "Red: " + color.B + "   Green: " + color.G + "   Blue: " + color.R;
        }

        private async void ApplyFilter(object obj)
        {
            if (_bitmap == null || SpinnerVisibility == Visibility.Visible) return;
            var filter = obj as FiltersListViewItem;
            SpinnerVisibility = Visibility.Visible;
            _bitmap = await ApplyFilterAsync(new Bitmap(_bitmap), filter);
            RaisePropertyChanged("Bitmap");
            SpinnerVisibility = Visibility.Hidden;
        }
      
        private async Task<Bitmap> ApplyFilterAsync(Bitmap b, FiltersListViewItem filterItem)
        {
            await Task.Run(delegate
            {       
                filterItem.Filter.Filter(b);
                filterItem.ApplicationCounter++;
            });
            return b;
        }

        private void ClearFilters()
        {
            if (_bitmap == null || SpinnerVisibility == Visibility.Visible) return;
            _bitmap = new Bitmap(_orginBitmap);
            RaisePropertyChanged("Bitmap");
            foreach (var filtersListViewItem in AllFilters)
            {
                filtersListViewItem.ApplicationCounter = 0;
            }
        }
    }
}
