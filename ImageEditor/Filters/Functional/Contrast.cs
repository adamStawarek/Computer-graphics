using System.Drawing;
using ImageEditor.Helpers;

namespace ImageEditor.Filters.Functional
{
    public class Contrast:FunctionalFilterBase
    {
        private double _contrastLevel=30;
        public override string Name => "Contrast enhancement";
        public override Color Transform(byte r, byte g, byte b)
        {
            var factor = 259 * (_contrastLevel + 255) / (255 * (259 - _contrastLevel));
            int newR= ((int)(factor * (r - 128) + 128)).TruncateRgb();
            int newG = ((int)(factor * (g - 128) + 128)).TruncateRgb();
            int newB = ((int)(factor * (b - 128) + 128)).TruncateRgb();
            return Color.FromArgb(newR,newG,newB);
        }
    }
}
