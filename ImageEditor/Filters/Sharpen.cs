namespace ImageEditor.Filters
{
    public class Sharpen : ConvolutionFilterBase
    {
        public override double Divisor => 1.0;
        public override double Bias => 0.0;
        public override string Name => "Sharpen";
        public override double[,] Matrix => new double[,] { { -1, -1, -1 }, { -1, 9, -1 }, { -1, -1, -1 } };
    }
}