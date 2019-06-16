namespace ImageEditor.ViewModel.Helpers
{

    public class Vertex
    {
        private double x;
        private double y;
        private double[][] p = new double[4][];
        private double[][] n = new double[4][];
        private double[][] t = new double[2][];
        public Vertex(double x, double y, double z, double r)
        {
            this.x = x;
            this.y = y;

            this.p[0] = new double[1];
            this.p[1] = new double[1];
            this.p[2] = new double[1];
            this.p[3] = new double[1];

            this.n[0] = new double[1];
            this.n[1] = new double[1];
            this.n[2] = new double[1];
            this.n[3] = new double[1];

            this.p[0][0] = x;
            this.p[1][0] = y;
            this.p[2][0] = z;
            this.p[3][0] = 1;

            this.n[0][0] = x / r;
            this.n[1][0] = y / r;
            this.n[2][0] = z / r;
            this.n[3][0] = 0;
        }
        public double[][] getP()
        {
            return p;
        }
        public double getX()
        {
            return x;
        }
        public double getY()
        {
            return y;
        }
        public void setXandY(double x, double y)
        {
            this.x = x;
            this.y = y;
        }
        public void setT(double[][] t)
        {
            this.t = t;
        }
    }

}