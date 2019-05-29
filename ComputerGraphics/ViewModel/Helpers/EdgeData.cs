using System;
using System.Drawing;

namespace ImageEditor.ViewModel.Helpers
{
    public class EdgeData : IComparable<EdgeData>
    {
        private Point _startPoint;
        private Point _endPoint;
        private double _ratio;
        private int _i;
        public void SetStartPoint(Point startPoint)
        {
            this._startPoint = startPoint;
        }
        public void SetEndPoint(Point endPoint)
        {
            this._endPoint = endPoint;
        }
        public Point GetStartPoint()
        {
            return this._startPoint;
        }
        public Point GetEndPoint()
        {
            return this._endPoint;
        }
        public void CalculateRatio()
        {
            _ratio = ((double)(_endPoint.X - _startPoint.X) / (double)(_endPoint.Y - _startPoint.Y));
        }
        public int CalculateX(int x)
        {
            this._i = (int)Math.Ceiling((_startPoint.X + (_ratio * (x - _startPoint.Y))));
            return (int)Math.Ceiling(((_startPoint.X + (_ratio * (x - _startPoint.Y)))));
        }

        public int CompareTo(EdgeData other)
        {
            return this._i.CompareTo(other._i);
        }
    }
}