using System.Collections.Specialized;

namespace ImageEditor.Filters.Interfaces
{
    public interface IResult:INotifyCollectionChanged
    {
        object Result { get; set; }
    }
}
