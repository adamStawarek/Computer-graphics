using System.Collections.Generic;
using ImageEditor.Annotations;
using ImageEditor.Filters;
using ImageEditor.ViewModel.Helpers;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using GalaSoft.MvvmLight.CommandWpf;

namespace ImageEditor.ViewModel
{
    public class FilterDesignerViewModel:INotifyPropertyChanged
    {

        private int _startY;
        public int StartY
        {
            get => _startY;
            set
            {
                if (value == _startY) return;
                _startY = value;
                OnPropertyChanged();
                SetTailPoints(_startY, true);
            }
        }
        private int _endY;        
        public int EndY
        {
            get => _endY;
            set
            {
                if (value == _endY) return;
                _endY = value;
                OnPropertyChanged();
                SetTailPoints(_endY, false);
            }
        }
        private FunctionalFilterBase _selectedFilter;
        public FunctionalFilterBase SelectedFilter
        {
            get => _selectedFilter;
            set
            {
                if (Equals(value, _selectedFilter)) return;
                _selectedFilter = value;
                OnPropertyChanged();
                SetFilter();
            }
        }

        public PlotModel MyModel { get; }
        public LineSeries S1 { get; }
        public FiltersListViewItem CustomFilterItem { get; set; }
        public List<FunctionalFilterBase> FunctionalFilters { get; set; }=new List<FunctionalFilterBase>
        {
            new Brightness(),
            new ColorInversion(),
            new Contrast(),
            new GammaCorrection()
        };

        public RelayCommand RemoveAllPointsCommand { get;}
        public RelayCommand SetFilterCommand { get; }

        public FilterDesignerViewModel()
        {
            MyModel = new PlotModel { Title = "Functional Filter Designer" };
            RemoveAllPointsCommand=new RelayCommand(RemoveAllPoints);
            SetFilterCommand=new RelayCommand(SetFilter);
            S1 = new LineSeries
            {  
                Points = {new DataPoint(0, 0), new DataPoint(255, 255),},
                MarkerFill = OxyColors.SteelBlue,
                MarkerType = MarkerType.Circle,
                MarkerSize = 5
            };
             
            CustomFilterItem=new FiltersListViewItem(new CustomFilter(S1.Points));

            MyModel.Series.Add(S1);           
            MyModel.Axes.Add(new LinearAxis() { Position = AxisPosition.Bottom, Minimum = 0, Maximum = 255 });
            MyModel.Axes.Add(new LinearAxis() { Position = AxisPosition.Left, Minimum = 0, Maximum = 255 });

            MyModel.MouseDown += (s, e) =>
            {
                
                Series series = MyModel.GetSeriesFromPoint(e.Position,10);
                if (series != null)
                {
                    TrackerHitResult result = series.GetNearestPoint(e.Position, false);
                    if(!((int)result.DataPoint.X==0|| (int)result.DataPoint.X == 255))                       
                        S1.Points.Remove(result.DataPoint);                    
                }
                else
                {
                    var x = S1.InverseTransform(e.Position).X;
                    var y = S1.InverseTransform(e.Position).Y;

                    S1.Points.Add(new DataPoint((int)x, (int)y));
                    S1.Points.Sort((p, p2) => p.X.CompareTo(p2.X));
                }

                MyModel.InvalidatePlot(true);
            };      
        }

        private void RemoveAllPoints()
        {
            int numberOfPointsToRemove = S1.Points.Count - 2;//we leave first and last point
            S1.Points.RemoveRange(1,numberOfPointsToRemove);
            MyModel.InvalidatePlot(true);
        }

        public void SetTailPoints(int val, bool isFirstPoint)
        {
            if (isFirstPoint)
            {
                S1.Points.RemoveAt(0);
                S1.Points.Add(new DataPoint(0, val));
            }
            else
            {
                var lastIndex = S1.Points.Count - 1;
                S1.Points.RemoveAt(lastIndex);
                S1.Points.Add(new DataPoint(255, val));
            }         
            S1.Points.Sort((p, p2) => p.X.CompareTo(p2.X));
            MyModel.InvalidatePlot(true);
        }

        private List<int> GetPointsFromIntervalWithGivenStepSize(int stepSize)//from 0 to 255
        {
            var min = byte.MinValue;
            var max = byte.MaxValue;
            List<int> points = new List<int>();
            for (int i= min; i < max; i += stepSize)
            {
                points.Add(i);
            }

            return points;
        }

        private void SetFilter()
        {
            //remove all points before applying filter            
            S1.Points.Clear();
            S1.Points.AddRange(new []{new DataPoint(0,0),new DataPoint(255,255)});           
            foreach (var point in GetPointsFromIntervalWithGivenStepSize(5))
            {
                var newPoint = SelectedFilter.Transform((byte) point);
                S1.Points.Add(new DataPoint(point, newPoint));
            }

            S1.Points.Sort((p, p2) => p.X.CompareTo(p2.X));
            MyModel.InvalidatePlot(true);
        }

        #region property changed
        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        } 
        #endregion
    }
}
