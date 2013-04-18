using System;
using System.Collections.Generic;
using System.Linq;
using DelftTools.Functions.Generic;
using DelftTools.Utils.Reflection;

namespace DelftTools.Functions
{
    /// <summary>
    /// TODO: get a base class for Stores and get this stuff in abstract or not.
    /// </summary>
    public class MemoryFunctionStoreHelper
    {
        public static IMultiDimensionalArray CreateEmptyValuesArray(IVariable variable)
        {
            Type valueType = variable.GetType().GetGenericArguments()[0];

            return (MultiDimensionalArray)TypeUtils.CreateGeneric(typeof(MultiDimensionalArray<>), valueType);
        }

        /// <summary>
        /// Creates a dictionary describing argument based dependencies for the given list of functions.
        /// </summary>
        /// <param name="functions"></param>
        /// <returns></returns>
        public static IDictionary<IVariable, IEnumerable<IVariable>> GetDependentVariables(IList<IFunction> functions)
        {
            var result = new Dictionary<IVariable, IEnumerable<IVariable>>();
            
            for (var i = 0; i < functions.Count; i++)
            {
                var variable = functions[i] as IVariable;

                if (variable == null)
                {
                    continue;
                }

                var dependentVariables = new List<IVariable>();

                for (var j = 0; j < functions.Count; j++)
                {
                    var variable2 = functions[j] as IVariable;
                    if (variable2 == null)
                    {
                        continue;
                    }

                    if (variable2.Arguments.Contains(variable))
                    {
                        dependentVariables.Add(variable2);
                    }
                }

                if (dependentVariables.Count > 0)
                {
                    result[variable] = dependentVariables;
                }
            }

            return result;
        }

        /// <summary>
        /// Creates a dictionary describing component based dependencies for the given list of functions.
        /// </summary>
        /// <param name="functions"></param>
        /// <returns></returns>
        public static IDictionary<IVariable, IList<IVariable>> GetComponentsDependencyTable(IList<IFunction> functions)
        {
            IDictionary<IVariable, IList<IVariable>> result = new Dictionary<IVariable, IList<IVariable>>();
            foreach (IVariable variable in functions.Where(f => f is IVariable))
            {

                IVariable variable1 = variable;
                IEnumerable<IVariable> variableDependencies = new List<IVariable>();
                IEnumerable<IVariable> dependendComponents =  
                    from f in functions
                    where f is IVariable && f != variable1 && functions.Any(sub => sub.IsIndependent && sub.Components.Contains(variable1) && sub.Components.Contains((IVariable)f))
                    select (IVariable)f;

                variableDependencies = variableDependencies.Concat(dependendComponents);
                if (variableDependencies.Count() != 0)
                    result.Add(variable, variableDependencies.ToList());
            }
            return result;
        }
    }
}