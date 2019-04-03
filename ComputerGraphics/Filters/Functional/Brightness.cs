using System.Drawing;
using ImageEditor.Helpers;

namespace ImageEditor.Filters.Functional
{
    public class Brightness:FunctionalFilterBase
    {
        private double _contrast=30;
        public override string Name => "Brightness correction";

        public override Color Transform(byte r,byte g,byte b)
        {
            var newR = (byte) ((r + (int) _contrast).TruncateRgb());
            var newG = (byte)((g + (int)_contrast).TruncateRgb());
            var newB = (byte)((b + (int)_contrast).TruncateRgb());
            return Color.FromArgb(newR,newG,newB);
        }    
    }
}
