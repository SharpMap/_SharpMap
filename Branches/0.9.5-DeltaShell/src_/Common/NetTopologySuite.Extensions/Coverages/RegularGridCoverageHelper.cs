using System;
using System.Collections.Generic;
using DelftTools.Functions;
using DelftTools.Functions.Generic;
using DelftTools.Utils.Reflection;
using GeoAPI.Extensions.Coverages;
using GeoAPI.Geometries;
using GisSharpBlog.NetTopologySuite.LinearReferencing;

namespace NetTopologySuite.Extensions.Coverages
{
    public class RegularGridCoverageHelper
    {
        /// <summary>
        /// Defines the gridProfile function
        /// </summary>
        /// <param name="grid"></param>
        /// <param name="polyline"></param>
        public static Function GetGridValues(IRegularGridCoverage grid, ILineString polyline)
        {
            Function function = new Function {Name = grid.Name};
            if (grid.Time != null)
            {
                grid.Name += " (" + grid.Time + ")";
            }
            function.Arguments.Add(new Variable<double>("offset"));
         function.Components.Add(  (IVariable) TypeUtils.CreateGeneric(typeof (Variable<>), grid.Components[0].ValueType, new object[] {"value"}));
            function.Components[0].NoDataValues = grid.Components[0].NoDataValues;

            if (null == polyline)
            {
                return function;
            }
            UpdateGridValues(function, grid, polyline);
            return function;
        }

        /// <summary>
        /// Fills gridvalues function with profiledata based on profileline over the grid
        /// </summary>
        /// <param name="function"></param>
        /// <param name="grid"></param>
        /// <param name="polyline"></param>
        public static void UpdateGridValues(Function function, IRegularGridCoverage grid, ILineString polyline)
        {
            function.Clear();
            double offset = 0;
            double step = polyline.Length / 100;
            foreach (ICoordinate coordinate in GetGridProfileCoordinates(polyline, step))
            {
                function[offset] = grid.Evaluate(coordinate);
                offset += step;
            }
        }

        /// <summary>
        /// return the coordinates along the gridProfile at stepSize intervals.
        /// </summary>
        /// <param name="gridProfile"></param>
        /// <param name="stepSize"></param>
        /// <returns></returns>
        public static IEnumerable<ICoordinate> GetGridProfileCoordinates(ILineString gridProfile, double stepSize)
        {
            var lengthIndexedLine = new LengthIndexedLine(gridProfile);
            if (0 == stepSize)
                throw new ArgumentException("Stepsize too small", "stepSize");
            int count = (int)((gridProfile.Length / stepSize) + 1);
            for (int i=0; i<count; i++)
            {
                yield return (ICoordinate)lengthIndexedLine.ExtractPoint(i * stepSize).Clone();
            }
        }
    }
}