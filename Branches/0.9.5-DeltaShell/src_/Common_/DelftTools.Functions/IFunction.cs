using System;
using System.Collections;
using System.Collections.Generic;
using DelftTools.Functions.Filters;
using DelftTools.Functions.Generic;
using DelftTools.Utils;
using DelftTools.Utils.Collections;
using DelftTools.Utils.Collections.Generic;
using DelftTools.Utils.Data;

namespace DelftTools.Functions
{
    /// <summary>
    /// Defines a <b>functional relation</b> between <b>component and argument variables</b>.
    /// Allows to access values of the components using GetValues, SetValues or this[<c>params</c> argumentIndexes[]].
    /// <example>
    /// Example of the function:<br/><br/>
    /// F = (Vx, Vy, Vz)(x, y, z, t)<br/><br/>
    ///  
    /// S = { f(x), g(y), h(z) }
    /// Here F is a function, Vx, Vy, Vz are <b>component variables</b> and x, y, z, t are <b>argument variables</b><br/><br/>
    /// 	</example>
    /// </summary>
    public interface IFunction : INotifyCollectionChanged, INameable, ICloneable, IUnique<long>
    {
        /// <summary>
        /// Store associated with the current function. Used to get/set values and all properties of the function.
        /// </summary>
        IFunctionStore Store { get; set; } // TODO: make setter of the store prive

        /// <summary>
        /// Function components, e.g. for F = (Vx, Vy, Vz)(t, x, y, z) components are Vx, Vy, Vz
        /// </summary>
        IEventedList<IVariable> Components { get; set; }

        /// <summary>
        /// Function components, e.g. for F = (Vx, Vy, Vz)(t, x, y, z) arguments are: t, x, y, z
        /// </summary>
        IEventedList<IVariable> Arguments { get; set; }

        /// <summary>
        /// Arbitrary attributes.
        /// </summary>
        IDictionary<string, string> Attributes { get; set; } // TODO: make me IDictionary<string, Parameter>, attribute can be of any type

        /// <summary>
        /// Gets or sets a single value using argument values. 
        /// A single value of a function is a tuple of all it's components.
        /// If function has only one component - a single value will be returned here.
        /// </summary>
        /// <param name="argumentValues"></param>
        /// <returns></returns>
        object this[params object[] argumentValues] { get; set; }

        /// <summary>
        /// Creates filtered function by specifying filters on its arguments / components.
        /// </summary>
        /// <param name="filters"></param>
        /// <returns></returns>
        IFunction Filter(params IVariableFilter[] filters);

        /// <summary>
        /// Gets or sets filters currently used in function to filter it's values. 
        /// They can filter only specific values, ranges of indexes/values or entire components.
        /// </summary>
        IList<IVariableFilter> Filters { get; set; }
 
        /// <summary>
        /// Parent function of the filtered function.
        /// </summary>
        IFunction Parent { get; set; }

        /// <summary>
        /// Returns list of all values of the function components.
        /// When filters are provided - only subset of component values will be returned.
        /// If function has more than one component and no component filter is provided - values will be returned in the following order:
        /// 
        /// <example>
        /// For function defined as: F = (f1, f2)(x)<br/>
        /// <see cref="IList"/> values = F.GetValues(); <br/><br/>
        /// Will return: { f1[0], f2[0], f1[1], f2[1] ... f1[x.Count - 1], f2[x.Count - 1] }
        /// </example>
        /// 
        /// </summary>
        /// <param name="filters"></param>
        /// <returns></returns>
        IMultiDimensionalArray GetValues(params IVariableFilter[] filters);

        IMultiDimensionalArray<T> GetValues<T>(params IVariableFilter[] filters) where T : IComparable;

        //object GetApproximatedValue(params IVariableFilter[] filters);

        /// <summary>
        /// Returns interpolated value of the function components based on the argument values of the provided using filters.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="filters"></param>
        /// <returns></returns>
        //T GetApproximatedValue<T>(params IVariableFilter[] filters) where T : IComparable;

        /// <summary>
        /// Return function value. Uses approximation if needed
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="filters"></param>
        /// <returns></returns>
        T Evaluate<T>(params IVariableFilter[] filters) where T : IComparable;

        /// <summary>
        /// Sets all values for a given array of argument values.
        /// </summary>
        /// <param name="values">Components values</param>
        /// <param name="filters">Filtering</param>
        void SetValues(IEnumerable values, params IVariableFilter[] filters);

        //void SetValues<T>(IEnumerable<T> values, params IVariableFilter[] filters);


        /// <summary>
        /// Removes all values for all argument values specified in the filters.
        /// </summary>
        /// <param name="filters"></param>
        void RemoveValues(params IVariableValueFilter[] filters);

        /// <summary>
        /// Clears all values of the function, but not structure!
        /// </summary>
        void Clear();

        /// <summary>
        /// True if variable is independent (has no no arguments).
        /// TODO: this is not property of the function but of variable, push down?
        /// </summary>
        bool IsIndependent { get; }

        event EventHandler<FunctionValuesChangedEventArgs> ValuesChanging;

        event EventHandler<FunctionValuesChangedEventArgs> ValuesChanged;

        /// <summary>
        /// Detaches the function from it arguments and components collection. Used to speed resizes.
        /// HACK: remove it, or hide in the implementation
        /// </summary>
        void Detach();

        /// <summary>
        /// Attaches the function to it arguments and components collection. 
        /// HACK: remove it, or hide in the implementation, user shouldn't be interested in this
        /// </summary>
        void Attach();

        /// HACK: remove it, or hide in the implementation
        bool Attached { get; }

        /// <summary>
        /// Reads an Xml representation of the function.
        /// </summary>
        /// <returns></returns>
        string ToXml();

        // TODO: move it down (to components), function may have 2 components then it is hard to say what value should be returned here
        
        /// <summary>
        /// Determines whether values can be set by user.
        /// </summary>
        bool IsEditable { get; set; }

        IFunction Aggregate<T, TAccumulate>(IVariable v, TAccumulate startValue, Func<TAccumulate, T, TAccumulate> func);

        /// <summary>
        /// Creates a new function as a clone of the current function using a given <see cref="targetStore"/>.
        /// Cloned function is automatically added to the <see cref="targetStore"/>.
        /// Copies values from the source function to the target function.
        /// </summary>
        /// <param name="targetStore">Store to be used as a target for a new function</param>
        /// <returns>New function with the same schema and values as a present function.</returns>
        object Clone(IFunctionStore targetStore);

        object Clone(bool copyValues);

        object Clone(bool copyValues, bool skipArguments, bool skipComponents);
    }
}