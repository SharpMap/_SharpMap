using System;
using System.Collections.Generic;
using DelftTools.Functions.Filters;
using DelftTools.Functions.Generic;
using DelftTools.Utils.Collections;
using DelftTools.Utils.Collections.Generic;
using DelftTools.Utils.Data;

namespace DelftTools.Functions
{
    /// <summary>
    /// Defines interface of the function values data store.
    /// 
    /// TODO: remove the params overloads. The inenumerable is nicer and params is just convenience.it makes it easier to write more stores
    /// </summary>
    public interface IFunctionStore : IUnique<long>, INotifyCollectionChange, ICloneable
    {
        /// <summary>
        /// Functions contained in the store. Use it to add or remove functions.
        /// During add value store must check
        /// </summary>
        IEventedList<IFunction> Functions { get; set; }

        /// <summary>
        /// Sets new values.
        /// </summary>
        /// <param name="function"></param>
        /// <param name="values">List containing combination of component values.</param>
        /// <param name="filters"></param>
        void SetVariableValues<T>(IVariable function, IEnumerable<T> values, params IVariableFilter[] filters);

        /// <summary>
        /// Removes all values that match any of the filters from the store
        /// </summary>
        /// <param name="function"></param>
        /// <param name="filters"></param>
        void RemoveFunctionValues(IFunction function, params IVariableValueFilter[] filters);

        /// <summary>
        /// Some stores are read-only, or support only add, or clear, but not remove of specific values.
        /// </summary>
        bool SupportsPartialRemove { get; }

        /// <summary>
        /// Adds new values to independend variables. If value exist it is ignored
        /// </summary>
        /// <param name="function"></param>
        /// <param name="values">List containing combination of component values.</param>
        /// <param name="filters"></param>
        void AddIndependendVariableValues<T>(IVariable variable, IEnumerable<T> values); // TODO: this method duplicates SetVariableValues, remove it.
        
        /// <summary>
        /// Returns values of the selected function as multidimensional array.
        /// </summary>
        /// <param name="function"></param>
        /// <param name="filters"></param>
        /// <returns></returns>
        IMultiDimensionalArray GetVariableValues(IVariable function, params IVariableFilter[] filters);

        /// <summary>
        ///  Returns values of the function as strongly typed multidimensional array.
        /// </summary>
        /// <param name="function"></param>
        /// <param name="filters"></param>
        /// <returns></returns>
        IMultiDimensionalArray<T> GetVariableValues<T>(IVariable function, params IVariableFilter[] filters);
        
        event EventHandler<FunctionValuesChangingEventArgs> FunctionValuesChanged;
        event EventHandler<FunctionValuesChangingEventArgs> FunctionValuesChanging;

        /// <summary>
        /// List of custom stored userTypes. 
        /// </summary>
        IList<ITypeConverter> TypeConverters { get; }

        /// <summary>
        /// Determines wether values changes should result in events
        /// </summary>
        bool FireEvents { get; set; }

        /// <summary>
        /// Used when a variables needs to resize explicitly. Resizes the variable to match it's argument
        /// TODO: this should not be part of the interface!
        /// </summary>
        /// <param name="variable"></param>
        void UpdateVariableSize(IVariable variable);

        T GetMaxValue<T>(IVariable variable);
        T GetMinValue<T>(IVariable variable);

        void CacheVariable(IVariable variable);
        
        bool DisableCaching { get; set; }
    }
}