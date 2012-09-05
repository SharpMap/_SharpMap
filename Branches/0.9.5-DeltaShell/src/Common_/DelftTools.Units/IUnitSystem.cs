using System.Collections.Generic;

namespace DelftTools.Units
{
    public interface IUnitSystem
    {
        string Name { get; set; }

        IList<IDimension> BaseDimensions { get; set; }
        IList<IUnit> BaseUnits { get; set; }
        IList<IUnit> DerivedUnits { get; set; }

        IUnit GetUnitBySymbol(string symbol);
        IUnit GetUnitByName(string name);

        IList<IUnit> GetCompatibleUnits(IUnit unit);

        IList<IUnit> GetTransformedUnits(IUnit unit);
        
        /* .... put more utility methods here ... */
    }
}
