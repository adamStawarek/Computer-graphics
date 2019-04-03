using System.Drawing;

namespace ImageEditor.Filters.Functional
{
    public class ColorInversion :FunctionalFilterBase
    {
        public override string Name => "Color inversion";

        public override Color Transform(byte r, byte g, byte b)
        {
            byte rgbMax = byte.MaxValue;
            var newR= (byte)(rgbMax - r);
            var newG = (byte)(rgbMax - g);
            var newB = (byte)(rgbMax - b);
            return Color.FromArgb(newR, newG, newB);
        } 
    }
}
