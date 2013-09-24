using System.Collections.Generic;
using DelftTools.Functions;
using DelftTools.Functions.Generic;
using GeoAPI.Extensions.Feature;

namespace GeoAPI.Extensions.Coverages
{
    /// <summary>
    /// x(i, j)
    /// y(i, j))
    /// f(i, j, ...)
    /// 
    /// F = (f(i, j ...), x(i, j), y(i, j))
    /// 
    /// TODO: check if it the same as defined in ISO 19123
    /// TODO: rename to ICurvilinearGridCoverage
    /// </summary>
    public interface IDiscreteGridPointCoverage : ICoverage
    {
        IVariable Index1 { get; }

        IVariable Index2 { get; }

        IVariable<double> X { get; }

        IVariable<double> Y { get; }

        int Size1 { get; }

        int Size2 { get; }
        
        IMultiDimensionalArray<IGridFace> Faces { get; }

        void Resize(int size1, int size2, IEnumerable<double> xCoordinates, IEnumerable<double> yCoordinates);
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="searchDistance">Search distance in the world coordinates</param>
        /// <returns></returns>
        IEnumerable<IFeature> GetFeatures(double x, double y, double searchDistance);
    }

}