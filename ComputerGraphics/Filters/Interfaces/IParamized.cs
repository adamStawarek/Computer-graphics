using System.Collections.Generic;
using ImageEditor.Filters.Helpers;

namespace ImageEditor.Filters.Interfaces
{
    public interface IParameterized
    {
       List<Parameter> Parameters { get; set; }
    }
}
