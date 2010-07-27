using System;
using System.Collections.Generic;
using DelftTools.Functions.Filters;

namespace DelftTools.Functions.Generic
{
    public interface IVariable<T> : IVariable where T : IComparable
    {
        /// <summary>
        /// List of values of the variable.
        /// </summary>
        new IMultiDimensionalArray<T> Values { get; set; }  

        /// <summary>
        /// Default value of the variable. Used when number of values in dependent variable changes and default values need to be added.
        /// </summary>
        new T DefaultValue { get; set; }

        /// <summary>
        /// List of values which will be interpreted as no-data values.
        /// </summary>
        new IList<T> NoDataValues { get; set; }

        /// <summary>
        /// Gets values with filters
        /// </summary>
        /// <param name="filters"></param>
        /// <returns></returns>
        new IMultiDimensionalArray<T> GetValues(params IVariableFilter[] filters);

        new IVariable<T> Clone();

        /// <summary>
        /// 
        /// </summary>
        /// <param name="values"></param>
        void AddValues<T>(IEnumerable<T> values);

        bool AutoSort { get; set; }

        //new T MinValue { get; }
        //new T MaxValue { get; }
        NextValueGenerator<T> NextValueGenerator{ get; set; }

        /// <summary>
        /// Minimum value of the variable
        /// </summary>
        new T MinValue{ get;}
        
        /// <summary>
        /// Maximum value of the variable
        /// </summary>
        new T MaxValue { get; }

        /// <summary>
        /// Returns all values of the variable. Includes values that have been filtered out.
        /// </summary>
        IMultiDimensionalArray<T> AllValues { get; }
    }
}