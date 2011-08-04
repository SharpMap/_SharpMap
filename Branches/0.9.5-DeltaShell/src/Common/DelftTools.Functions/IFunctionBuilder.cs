using System.Collections.Generic;

namespace DelftTools.Functions
{
    /// <summary>
    /// Used to create custom functions based on simple variables (those which use primitive types as their value type).
    /// </summary>
    public interface IFunctionBuilder
    {
        // TODO: extend to support more functions. A single store may contain many custom functions.

        bool CanBuildFunction(IEnumerable<IVariable> variables);//IDictionary<string, string> attributes);
        IFunction CreateFunction(IEnumerable<IVariable> variables);
    }
}