using System.Collections.Generic;

namespace DelftTools.Units
{
    public interface IProductDimension: IDimension
    {
        IList<IDimension> ProductDimensions { get; }
    }
}
