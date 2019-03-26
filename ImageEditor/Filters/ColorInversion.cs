namespace ImageEditor.Filters
{
    public class ColorInversion :FunctionalFilterBase
    {
        public override string Name => "Color inversion";

        public override byte Transform(byte rgbVal)
        {
            byte rgbMax = byte.MaxValue;
            return (byte)(rgbMax - rgbVal);
        } 
    }
}
