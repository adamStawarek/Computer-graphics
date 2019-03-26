using ImageEditor.Helpers;

namespace ImageEditor.Filters
{
    public class Contrast:FunctionalFilterBase
    {
        private double _contrastLevel=30;
        public override string Name => "Contrast enhancement";
        public override byte Transform(byte rgbVal)
        {
            var factor = 259 * (_contrastLevel + 255) / (255 * (259 - _contrastLevel));
            int val= ((int)(factor * (rgbVal - 128) + 128)).TruncateRgb();
            return (byte)val;
        }
    }
}
