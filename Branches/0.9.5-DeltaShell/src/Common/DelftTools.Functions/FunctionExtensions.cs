using System.Linq;

namespace DelftTools.Functions
{
    public static class FunctionExtensions
    {
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
    }
}