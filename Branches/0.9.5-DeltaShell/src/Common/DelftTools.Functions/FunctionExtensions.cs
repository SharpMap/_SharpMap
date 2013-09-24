using System;
using System.Collections.Generic;
using System.Linq;
using DelftTools.Functions.Filters;

namespace DelftTools.Functions
{
    public static class FunctionExtensions
    {
        public static IFunction CopyToStore(this IFunction function, IFunctionStore store)
        {
            if (function.Parent == null)
            {
                var clone = (IFunction)function.Clone();

                FixFixedSize(clone);

                store.Functions.Add(clone);
                return clone;
            }

            var variables = function.Arguments.Concat(function.Components).ToList();
            var variableValues = new List<IMultiDimensionalArray>();

            //copy values of all variables into temp array (write them as soon as first Flush() or first SetValues() occurs).
            foreach (var variable in variables)
            {
                variableValues.Add(variable.Values);
            }

            IFunction topParent = function.Parent;
            while (topParent.Parent != null)
            {
                topParent = topParent.Parent;
            }
            var detachedFunction = (IFunction)topParent.Clone(false); //can only clone unfiltered function

            var detachedVariables = detachedFunction.Arguments.Concat(detachedFunction.Components).ToList();

            for (int i = 0; i < variableValues.Count; i++)
            {
                var values = variableValues[i];
                detachedVariables[i].FixedSize = values.Count;
            }

            store.Functions.Add(detachedFunction);
            
            // copy temp values back to netCdf
            for (int i = 0; i < variableValues.Count; i++)
            {
                var values = variableValues[i];
                if (values.Count > 0)
                {
                    var detachedVariable = detachedVariables[i];
                    detachedVariable.SetValues(values);
                }
            }

            return detachedFunction;
        }

        private static void FixFixedSize(IFunction function)
        {
            for (int i = 1; i < function.Arguments.Count; i++)
            {
                function.Arguments[i].FixedSize = function.Arguments[i].Values.Count;
            }
        }

        /// <summary>
        /// Gets the first argument variable in the function of data type T
        /// </summary>
        /// <typeparam name="T">The type to search for</typeparam>
        /// <param name="function">The function to explore</param>
        /// <returns>If a match is found the variable will be returned. Null otherwise</returns>
        public static IVariable GetFirstArgumentVariableOfType<T>(this IFunction function)
        {
            if (function == null || function.Arguments == null || function.Arguments.Count == 0)
                return null;

            return function.Arguments.FirstOrDefault(v => v.ValueType.Equals(typeof(T)) || v.ValueType.IsAssignableFrom(typeof(T)));
        }
        /// <summary>
        /// Getst the first component variable in the function of data type T
        /// </summary>
        /// <typeparam name="T">The type to search for</typeparam>
        /// <param name="function">The function to explore</param>
        /// <returns>If a match is found the variable will be returned. Null otherwise</returns>
        public static IVariable GetFirstComponentVariableOfType<T>(this IFunction function)
        {
            if (function == null || function.Components == null || function.Components.Count == 0)
                return null;

            return function.Components.FirstOrDefault(v => v.ValueType.Equals(typeof(T)) || v.ValueType.IsAssignableFrom(typeof(T)));
        }

        /// <summary>
        /// TODO: refactor, very specific case only makes API of function in all other cases unnecessary complex
        /// </summary>
        /// <typeparam name="TArg"></typeparam>
        /// <typeparam name="TComp"></typeparam>
        /// <param name="function"></param>
        /// <param name="compValue"></param>
        /// <param name="argValues"></param>
        public static void SetComponentArgumentValues<TArg, TComp>(this IFunction function, IEnumerable<TComp> compValue, IEnumerable<TArg> argValues)
            where TArg : IComparable
            where TComp : IComparable
        {
            ValidateFunctionIs1DAndOfCorrectTypeOrThrow<TArg, TComp>(function);
            function.SetValues(compValue, new VariableValueFilter<TArg>(function.Arguments[0], argValues));
        }

        /// <summary>
        /// TODO: refactor, very specific case only makes API of function in all other cases unnecessary complex
        /// </summary>
        /// <typeparam name="TArg"></typeparam>
        /// <typeparam name="TComp"></typeparam>
        /// <param name="function"></param>
        /// <param name="argvalue"></param>
        /// <returns></returns>
        public static TComp Evaluate1D<TArg,TComp>(this IFunction function,TArg argvalue) where TComp : IComparable where TArg : IComparable
        {
            ValidateFunctionIs1DAndOfCorrectTypeOrThrow<TArg,TComp>(function);
            return function.Evaluate<TComp>(new VariableValueFilter<TArg>(function.Arguments[0], argvalue));
        }

        
        private static void ValidateFunctionIs1DAndOfCorrectTypeOrThrow<TArg, TComp>(IFunction function)
        {
            if ((function.Arguments.Count != 1) || (function.Components.Count != 1))
            {
                throw new InvalidOperationException("Function does not have a single argument and component.");
            }
            if (function.Arguments[0].ValueType != typeof(TArg))
            {
                throw new InvalidOperationException(string.Format("Function argument is not of type {0}.", typeof(TArg)));
            }
            if (function.Components[0].ValueType != typeof(TComp))
            {
                throw new InvalidOperationException(string.Format("Function component is not of type {0}.", typeof(TComp)));
            }
        }

        public static bool IsEqualOrDescendant(this IFunction function, IFunction toFind)
        {
            if (function.Equals(toFind))
                return true;

            while (function.Parent != null)
            {
                function = function.Parent;
                if (function.Equals(toFind))
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Returns an array of all component values, and works for multi-component functions.
        /// This function should be used instead of <see cref="Function.GetValues"/> as long as that method
        /// does not return ALL component values
        /// TODO [Tiemen]: Should be fixed in function
        /// </summary>
        /// <param name="function"></param>
        /// <param name="arguments"></param>
        /// <returns></returns>
        public static object[] GetAllComponentValues(this IFunction function, params object[] arguments)
        {
            return function.Components.Select(c => c[arguments]).ToArray();
        }
    }
}