namespace ImageEditor.Filters
{
    //South emboss
    public class Emboss : ConvolutionFilterBase {
        public override double Divisor => 1;
        public override double Bias => 0;
        public override string Name => "Emboss";
        public override double[,] Matrix => new double[,] { { -1, -1, -1 }, { 0, 1, 0 }, { 1, 1, 1 } };
    }
}