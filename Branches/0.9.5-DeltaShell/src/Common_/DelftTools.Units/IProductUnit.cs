using System.Collections.Generic;

namespace DelftTools.Units
{
    /// <summary>
    /// Product unit constructed as a product of another units:
    /// 
    /// ProductUnit = ProductUnit1 * ProductUnit2 * ... * ProductUnitN
    /// 
    /// </summary>
    public interface IProductUnit: IUnit
    {
        IList<IUnit> ProductUnits { get; }
    }
}
