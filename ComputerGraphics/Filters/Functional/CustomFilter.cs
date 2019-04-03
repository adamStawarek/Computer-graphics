using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using ImageEditor.Helpers;
using OxyPlot;

namespace ImageEditor.Filters.Functional
{
    public class CustomFilter:FunctionalFilterBase
    {
        public List<DataPoint> Points { get; set; }
        public CustomFilter(List<DataPoint> points)
        {
            Points = points;
        }
        public override string Name => "Custom filter";
        public override Color Transform(byte r, byte g, byte b)
        {
            var p1R = Points.Where(p => p.X <= r).OrderByDescending(p=>p.X).Take(1).First();
            var p2R = Points.Where(p => p.X >= r).OrderBy(p => p.X).Take(1).First();
            var newR = FunctionHelper
                .GetThirdPointYValue(p1R, p2R, r)
                .TruncateRgb();

            var p1G = Points.Where(p => p.X <= g).OrderByDescending(p => p.X).Take(1).First();
            var p2G = Points.Where(p => p.X >= g).OrderBy(p => p.X).Take(1).First();
            var newG = FunctionHelper
                .GetThirdPointYValue(p1G, p2G, g)
                .TruncateRgb();

            var p1B = Points.Where(p => p.X <= b).OrderByDescending(p => p.X).Take(1).First();
            var p2B = Points.Where(p => p.X >= b).OrderBy(p => p.X).Take(1).First();
            var newB = FunctionHelper
                .GetThirdPointYValue(p1B, p2B, b)
                .TruncateRgb();
            return Color.FromArgb(newR,newG,newB);
        }
    }
}
