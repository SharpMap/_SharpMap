using System;
using System.Linq;
using System.Reflection;
using PostSharp.Extensibility;
using PostSharp.Laos;

namespace DelftTools.Utils.Aop
{
    /// <summary>
    /// When applied to a class, automatically applies the <see cref="MethodParameterValidationAspect"/> to any methods with an attribute that can be validated.
    /// </summary>
    /// <remarks>
    /// Detects whether any method on any type (via an assembly-level application of the attribute) should be validated, and if so, 
    /// injects a <see cref="MethodParameterValidationAspect"/>.
    /// </remarks>
    [MulticastAttributeUsage(MulticastTargets.Class | MulticastTargets.Struct), Serializable]
    public class ParameterValidationAspect : CompoundAspect
    {
        /// <summary>
        /// Provides the aspects.
        /// </summary>
        /// <param name="element">The element.</param>
        /// <param name="collection">The collection.</param>
        public override void ProvideAspects(object element, LaosReflectionAspectCollection collection)
        {
            var targetType = element as Type;
            if (targetType == null) return;

            var bindings = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.DeclaredOnly;
            var methods = targetType.GetMethods(bindings).Cast<MethodBase>();
            var ctors = targetType.GetConstructors(bindings).Cast<MethodBase>();
            var membersToValidate = methods.Union(ctors).Where(ShouldValidate);
            foreach (var method in membersToValidate)
            {
                collection.AddAspect(method, new MethodParameterValidationAspect());
            }
        }

        /// <summary>
        /// Determines whether a method should be validated.
        /// </summary>
        private static bool ShouldValidate(MethodBase method)
        {
            return method.GetParameters()
                       .Where(param => 
                              param.GetCustomAttributes(false)
                                  .Where(pa => typeof(AbstractParameterValidationAttribute).IsAssignableFrom(pa.GetType())
                                  ).Count() >= 1
                       ).Count() >= 1;
        }
    }
}