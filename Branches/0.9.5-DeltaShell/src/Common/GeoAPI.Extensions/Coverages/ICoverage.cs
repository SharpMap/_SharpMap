using System;
using System.Collections.Generic;
using DelftTools.Functions;
using DelftTools.Functions.Generic;
using GeoAPI.Extensions.Feature;
using GeoAPI.Geometries;

namespace GeoAPI.Extensions.Coverages
{
    /// <summary>
    /// A coverage is a feature that associates positions within a bounded space (its domain) 
    /// to feature attribute values (its range). In other words, it is both a feature and a function. 
    /// Examples include a raster image, a polygon overlay or a digital elevation matrix.
    /// 
    /// http://www.unidata.ucar.edu/projects/THREDDS/GALEON/Reports/CDM-ISO/CDM-ISO-DataModels.htm
    /// 
    /// Coverage has IGeometryCollection as it's geometry.
    /// 
    /// The essential property of coverage is to be able to generate a value for any point 
    /// within its domain. How coverage is represented internally is not a concern. For 
    /// example consider the following different internal representations of coverage:
    /// 1. A coverage may be represented by a set of polygons which exhaustively tile 
    ///  a plane (that is each point on the plane falls in precisely one polygon). The 
    ///  value returned by the coverage for a point is the value of an attribute of the 
    ///  polygon that contains the point.
    /// 2. A coverage may be represented by a grid of values. The value returned by the 
    ///  coverage for a point is that of the grid value whose location is nearest the point.
    /// 3. Coverage may be represented by a mathematical function. The value returned 
    ///  by the coverage for a point is just the return value of the function when supplied 
    ///  the coordinates of the point as arguments.
    /// 4. Coverage may be represented by combination of these. For example, coverage may 
    ///  be represented by a combination of mathematical functions valid over a set of 
    ///  polynomials.
    /// </summary>
    public interface ICoverage: IFeature, IFunction
    {
        /// <summary>
        /// Evaluates value on the coverage at a given coordinate.
        /// </summary>
        /// <param name="coordinate"></param>
        /// <returns></returns>
        object Evaluate(ICoordinate coordinate);

        /// <summary>
        /// Evaluates value of coverage at coordinate.
        /// </summary>
        /// <param name="coordinate"></param>
        /// <returns></returns>
        T Evaluate<T>(ICoordinate coordinate);

        /// <summary>
        /// Evaluates a value in the coverage using given (x,y) coordinates.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        T Evaluate<T>(double x, double y);

        /// <summary>
        /// Evaluates coverage value at coordinate and time.
        /// </summary>
        /// <param name="coordinate"></param>
        /// <param name="time"></param>
        /// <returns></returns>
        object Evaluate(ICoordinate coordinate, DateTime? time);

        /// <summary>
        /// Time variable, or null if coverage is not time-dimensional.
        /// </summary>
        IVariable<DateTime> Time { get; set; }

        bool IsTimeDependent { get; set; }

        IFunction GetTimeSeries(ICoordinate coordinate);

        /// <summary>
        /// Returns a time filtered version of this coverage
        /// </summary>
        /// <param name="time"></param>
        ICoverage FilterTime(DateTime time);
    }

}