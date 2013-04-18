using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DelftTools.Functions.Filters;
using DelftTools.Functions.Generic;
using DelftTools.Utils.Reflection;

namespace DelftTools.Functions.Conversion
{
    /// <summary>
    /// Converts a variable in a function to another type :
    /// For example a function x(DateTime) can be converted to x(Double) 
    /// by specifying a converter between datetime and double
    /// </summary>
    /// <typeparam name="TTarget"></typeparam>
    /// <typeparam name="TSource"></typeparam>
    public class ConvertedFunction<TTarget,TSource> : Function where TSource : IComparable where TTarget : IComparable
    {
        //private EventedList<IVariable> arguments;
        protected IVariable convertedVariable;
        protected IVariable<TSource> variableToConvert;

        protected Func<TTarget, TSource> toSource;
        protected Func<TSource, TTarget> toTarget;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="parent">Function to convert</param>
        /// <param name="variableToConvert">Variable inside the function to convert</param>
        /// <param name="toSource">Method to convert from TTarget to TSource</param>
        /// <param name="toTarget">Method to convert from TTarget to TSource</param>
        public ConvertedFunction(IFunction parent, IVariable<TSource> variableToConvert, Func<TTarget, TSource> toSource, Func<TSource, TTarget> toTarget)
        {
            Parent = parent;
            this.variableToConvert = variableToConvert;
            this.toSource = toSource;
            this.toTarget = toTarget;
            foreach (IVariable arg in parent.Arguments)
            {

                Arguments.Add(CreateConvertedVariable(arg));
            }

            foreach (IVariable com in parent.Components)
            {
                if (com == parent)
                {
                    continue;//skip variable which are there own components.
                }
                //TODO: get rid of this stuff. Need it now because we get the arguments twice otherwise :(
                Components.CollectionChanged -= Components_CollectionChanged;
                Components.Add(CreateConvertedVariable(com));
                Components.CollectionChanged += Components_CollectionChanged;
            }
            
        }

        private IVariable CreateConvertedVariable(IVariable variable)
        {
            IVariable returnVariable;

            //have to do some reflection to get the types right.
            if (variableToConvert == variable)
            {
                returnVariable = new ConvertedVariable<TTarget, TTarget, TSource>(variable, variableToConvert, toSource, toTarget);
            }
            else
            {
                Type[] types = new[] { variable.ValueType, typeof(TTarget), typeof(TSource) };
                returnVariable = (IVariable)TypeUtils.CreateGeneric(typeof(ConvertedVariable<,,>), types, variable, variableToConvert, toSource,
                                                                         toTarget);
            }
            returnVariable.Name = variable.Name;
            return returnVariable;
            
        }

        public override IMultiDimensionalArray<T> GetValues<T>(params IVariableFilter[] filters)
        {
            return (IMultiDimensionalArray<T>)new ConvertedArray<TTarget, TSource>(Parent.GetValues<TSource>(ConvertFilters(filters)), toSource, toTarget);
        }
        
        public override IMultiDimensionalArray GetValues(params IVariableFilter[] filters)
        {
            if (Parent == variableToConvert)
            {
                return
                    new ConvertedArray<TTarget, TSource>(Parent.GetValues<TSource>(ConvertFilters(filters)), toSource,
                                                         toTarget);
            }
            return Parent.GetValues(ConvertFilters(filters));
        }

        //TODO: get this more clear
        protected override void SetValues<T>(IEnumerable<T> values, params IVariableFilter[] filters)
        {
            Parent.SetValues(values, ConvertFilters(filters));
        }
        public override void SetValues(IEnumerable values, params IVariableFilter[] filters)
        {
            base.SetValues(values, ConvertFilters(filters));
        }

        private IVariableFilter[] ConvertFilters(IVariableFilter[] filters)
        {
            //rewrite variable value filter to the domain of the source.
            IList<IVariableFilter> filterList = new List<IVariableFilter>();
            foreach (var filter in filters)
            {
                //TODO: rewrite to IEnumerable etc
                if (filter is IVariableValueFilter && filter.Variable.Parent == variableToConvert)
                {
                    var variableValueFilter = filter as IVariableValueFilter;
                    IList values = new List<TSource>();
                    foreach (TTarget obj in variableValueFilter.Values)
                    {
                        values.Add(toSource(obj));
                    }
                    filterList.Add(variableToConvert.CreateValuesFilter(values));
                }
                else
                {
                    filterList.Add(filter);
                }
            }
            return filterList.ToArray();
        }
    }
}