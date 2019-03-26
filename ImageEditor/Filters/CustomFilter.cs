using ImageEditor.Helpers;
using OxyPlot;
using System.Collections.Generic;
using System.Linq;

namespace ImageEditor.Filters
{
    public class CustomFilter:FunctionalFilterBase
    {
        public List<DataPoint> Points { get; set; }

        public CustomFilter(List<DataPoint> points)
        {
            Points = points;
        }

        public override string Name => "Custom filter";

        public override byte Transform(byte rgbVal)
        {
            var p1 = Points.Where(p => p.X <= rgbVal).OrderByDescending(p=>p.X).Take(1).First();
            var p2 = Points.Where(p => p.X >= rgbVal).OrderBy(p => p.X).Take(1).First();
            var result = FunctionHelper
                .GetThirdPointYValue(p1, p2, rgbVal)
                .TruncateRgb();
            return (byte)result;
        }
    }
}
