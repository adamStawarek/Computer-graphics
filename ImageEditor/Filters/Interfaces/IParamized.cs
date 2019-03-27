using System.Collections.Generic;

namespace ImageEditor.Filters.Interfaces
{
    public interface IParameterized
    {
       Dictionary<string,string> Parameters { get; set; }
    }
}
