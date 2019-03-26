using ImageEditor.Helpers;
using System;

namespace ImageEditor.Filters
{
    public class GammaCorrection:FunctionalFilterBase
    {
        private double gamma= 0.5;
        public override string Name => "Gamma correction";

        public override byte Transform(byte rgbVal)
        {
            return (byte)(255.0 * Math.Pow(rgbVal / 255.0, 1.0 / gamma) + 0.5).TruncateRgb();
        }
    }
}
