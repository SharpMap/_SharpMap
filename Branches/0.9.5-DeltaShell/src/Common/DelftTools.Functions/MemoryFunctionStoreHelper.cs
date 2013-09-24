using System.Collections.Generic;
using System.Linq;

namespace DelftTools.Functions
{
    /// <summary>
    /// TODO: get a base class for Stores and get this stuff in abstract or not.
    /// </summary>
    public class MemoryFunctionStoreHelper
    {
        
        /// <summary>
        /// Creates a dictionary describing argument based dependencies for the given list of functions.
        /// </summary>
        /// <param name="functions"></param>
        /// <returns></returns>
        public static IDictionary<IVariable, IEnumerable<IVariable>> GetDependentVariables(IList<IFunction> functions)
        {
            var result = new Dictionary<IVariable, IEnumerable<IVariable>>();

            var variables = functions.OfType<IVariable>();

            foreach (var variable in variables)
            {
                var variable1 = variable;
                result[variable] = functions.OfType<IVariable>()
                    .Where(variable2 => variable2.Arguments.Contains(variable1))
                    .ToList();
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