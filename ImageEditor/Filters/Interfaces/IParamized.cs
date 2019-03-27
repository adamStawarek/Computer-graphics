using System.Collections.Generic;

namespace ImageEditor.Filters.Interfaces
{
    public interface IParameterized
    {
       List<Parameter> Parameters { get; set; }
    }
}
