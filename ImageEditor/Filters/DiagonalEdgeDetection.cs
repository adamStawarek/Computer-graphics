namespace ImageEditor.Filters
{
    public class DiagonalEdgeDetection:ConvolutionFilterBase
    {
        public override double[,] Matrix { get; } = new double[,] {{-1, 0, 0}, {0, 1, 0}, {0, 0, 0}};
        public override double Divisor { get; } = 1.0;
        public override double Bias { get; } = 0;
        public override string Name => "Edge detection";
    }
}