namespace ImageEditor.Filters.Convolution
{
    public class Blur:ConvolutionFilterBase
    {
        public override double Divisor => 9.0;
        public override double Bias => 0.0;
        public override string Name => "Blur";
        public override double[,] Matrix => new double[,] { { 1,1,1 }, { 1, 1, 1 }, { 1, 1, 1 } };
    }
}
