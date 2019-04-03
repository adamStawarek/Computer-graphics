using System;
using System.Drawing;
using ImageEditor.Helpers;

namespace ImageEditor.Filters.Functional
{
    public class GammaCorrection:FunctionalFilterBase
    {
        private double gamma= 0.5;
        public override string Name => "Gamma correction";

        public override Color Transform(byte r, byte g, byte b)
        {
            var newR= (byte)(255.0 * Math.Pow(r / 255.0, 1.0 / gamma) + 0.5).TruncateRgb();
            var newG = (byte)(255.0 * Math.Pow(g / 255.0, 1.0 / gamma) + 0.5).TruncateRgb();
            var newB = (byte)(255.0 * Math.Pow(b / 255.0, 1.0 / gamma) + 0.5).TruncateRgb();

            return Color.FromArgb(newR, newG, newB);
        }
    }
}
