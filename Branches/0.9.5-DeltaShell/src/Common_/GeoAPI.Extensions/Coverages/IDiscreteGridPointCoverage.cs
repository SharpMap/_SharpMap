using System.Collections.Generic;
using DelftTools.Functions;
using DelftTools.Functions.Generic;
using GeoAPI.Geometries;

namespace GeoAPI.Extensions.Coverages
{
    /// <summary>
    /// i
    /// j
    /// x(i, j)
    /// y(i, j))
    /// f(i, j, ...)
    /// 
    /// TODO: check if it the same as defined in ISO 19123
    /// </summary>
    public interface IDiscreteGridPointCoverage : ICoverage
    {
        IVariable Index1 { get; }

        IVariable Index2 { get; }

        IVariable<double> X { get; }
        
        IVariable<double> Y { get; }
        
        int Size1 { get; }

        int Size2 { get; }

        void Resize(int size1, int size2, IEnumerable<double> xCoordinates, IEnumerable<double> yCoordinates);
    }
}