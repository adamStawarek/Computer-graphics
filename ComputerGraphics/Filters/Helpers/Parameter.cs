using System.Collections.Generic;

namespace ImageEditor.Filters.Helpers
{
    public class Parameter
    {
        public Parameter(string name)
        {
            Name = name;
            PossibleValues=new List<string>();
        }

        public string Name { get; set; }
        public string Value { get; set; }
        public List<string> PossibleValues { get; set; }       
    }
}
