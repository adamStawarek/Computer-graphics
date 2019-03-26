using ImageEditor.Helpers;

namespace ImageEditor.Filters
{
    public class Brightness:FunctionalFilterBase
    {
        private double _contrast=30;
        public override string Name => "Brightness correction";

        public override byte Transform(byte rgbVal)
        {
            return (byte)((rgbVal + (int)_contrast).TruncateRgb());
        }    
    }
}
