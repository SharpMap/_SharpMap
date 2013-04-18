using System.Collections;
using DelftTools.Functions.Filters;
using DelftTools.Functions.Generic;
using DelftTools.Units;

namespace DelftTools.Functions
{
    /// <summary>
    /// Defines a variable which can be used in any function as an argument or component.
    /// Variable itself can be independent or dependent. Every variable by definition is a function with only one component.
    /// Here variable is defined as a discrete variable which has a set of values. 
    /// Depending on type of the variable (dependent, independent) values of the variable are represented by 1D or nD array, 
    /// see also <see cref="IMultiDimensionalArray"/>.
    /// </summary>
    public interface IVariable : IFunction, IMeasurable
    {
        /// <summary>
        /// Provides access to all values of the variable.
        /// </summary>
        IMultiDimensionalArray Values { get; set; }

        /// <summary>
        /// Default step used to add new values to the variable. TODO: remove it, UI-related
        /// </summary>
        object DefaultStep { get; set; }

        /// <summary>
        /// List of values which will be interpreted as no-data values.
        /// TODO: make me value, IComparable?
        /// </summary>
        IList NoDataValues { get; set; }

        /// <summary>
        /// Creates filtered variable as a new function which has current variable as it's parent.
        /// </summary>
        /// <param name="filters"></param>
        /// <returns></returns>
        new IVariable Filter(params IVariableFilter[] filters);

        /// <summary>
        /// For some value stores, e.g. NetCDF, size of the variable must be specified. Otherwise if should be 0.
        /// Which means that variable size may change in the future. Factor out to INetCdfVariable?
        /// </summary>
        int FixedSize { get; set; }

        bool IsFixedSize { get; }

        new IVariable Clone();

        /// <summary>
        /// Adds values to independend varaible
        /// </summary>
        /// <param name="values"></param>
        void AddValues(IEnumerable values);

        //object MaxValue { get;}
        //object MinValue { get; }
        //T GetMinValue<T>() where T : IComparable<T>;

        /// <summary>
        /// Factory method.  Creates a new instance of the proper multi dimensional array which can be used to 
        /// keep values of the variable. *Pure optimization issue*
        /// </summary>
        /// <returns></returns>
        IMultiDimensionalArray CreateStorageArray();

        IMultiDimensionalArray CreateStorageArray(IMultiDimensionalArray values);

        bool AutoSort { get; set; }

        /// <summary>
        /// Will replace the default value with a values based on defaultStep and predecessor
        /// usefull for binding in datagrids and lists
        /// This should not be the default beghaviour because the side effect is that adding a default value
        /// on purpose will fail. 
        /// </summary>
        bool GenerateUniqueValueForDefaultValue { get; set; }

        bool ChecksIfValuesAreUnique { get; set; }

        ApproximationType InterpolationType { get; set; }
        
        ApproximationType ExtrapolationType { get; set; } // TODO: extrapolation can be different at the beginning and at the end

        object MaxValue { get; }
        object MinValue { get; }

        IVariableValueFilter CreateValueFilter(object value);
        
        IVariableValueFilter CreateValuesFilter(IEnumerable values);
    }
}