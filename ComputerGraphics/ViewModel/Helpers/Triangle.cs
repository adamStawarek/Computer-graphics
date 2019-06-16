namespace ImageEditor.ViewModel.Helpers
{
    public class Triangle
    {
        private readonly Vertex _v1;
        private readonly Vertex _v2;
        private readonly Vertex _v3;
        public Triangle(Vertex v1, Vertex v2, Vertex v3)
        {
            this._v1 = v1;
            this._v2 = v2;
            this._v3 = v3;
        }
        public Vertex GetV1()
        {
            return _v1;
        }
        public Vertex GetV2()
        {
            return _v2;
        }
        public Vertex GetV3()
        {
            return _v3;
        }      
    }
}