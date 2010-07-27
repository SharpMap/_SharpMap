using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using PostSharp.Laos;

namespace DelftTools.Utils.Aop
{
    /// <summary>
    /// Must be applied to a method if the <see>NotNullAttribute</see> attribute is on a parameter
    /// This is temporary until PostSharp gets a OnParameterBoundaryAspect.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Constructor, AllowMultiple = false), Serializable]
    public class MethodParameterValidationAspect : OnMethodBoundaryAspect
    {
        private readonly IList<ParameterValidationRequirements> _parameterRules = new List<ParameterValidationRequirements>();
        
        public override void RuntimeInitialize(MethodBase method)
        {
            base.RuntimeInitialize(method);
            
            var parameters = method.GetParameters();
            foreach (var parameter in parameters)
            {
                var validationRequirements = new ParameterValidationRequirements(parameter);
                validationRequirements.RulesToApply.AddRange( parameter.GetCustomAttributes(false).Select(att => att as AbstractParameterValidationAttribute).Where(att => att != null));
                _parameterRules.Add(validationRequirements);
            }
        }

        [DebuggerNonUserCode]
        public override void OnEntry(MethodExecutionEventArgs eventArgs)
        {
            var arguments = eventArgs.GetReadOnlyArgumentArray();
            for (var i = 0; i < arguments.Length; ++i)
            {
                var argumentValue = arguments[i];
                var parameterRules = _parameterRules[i];
                
                foreach (var rule in parameterRules.RulesToApply)
                {
                    rule.ValidateParameter(parameterRules.Parameter, argumentValue);
                }
            }
        }

        [Serializable]
        private class ParameterValidationRequirements
        {
            public readonly ParameterInfo Parameter;
            public readonly List<AbstractParameterValidationAttribute> RulesToApply;

            public ParameterValidationRequirements(ParameterInfo parameter)
            {
                Parameter = parameter;
                RulesToApply = new List<AbstractParameterValidationAttribute>();
            }
        }
    }
}