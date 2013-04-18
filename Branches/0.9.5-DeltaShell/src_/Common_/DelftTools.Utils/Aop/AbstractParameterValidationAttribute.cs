using System;
using System.Reflection;

namespace DelftTools.Utils.Aop
{
    /// <summary>
    /// A base class implemented by attributes that can be applied to parameters and will be called at runtime to validate them.
    /// </summary>
    public abstract class AbstractParameterValidationAttribute : Attribute
    {
        /// <summary>
        /// Validates the parameter.
        /// </summary>
        /// <param name="parameter">The parameter.</param>
        /// <param name="value">The value.</param>
        public abstract void ValidateParameter(ParameterInfo parameter, object value);
    }
}