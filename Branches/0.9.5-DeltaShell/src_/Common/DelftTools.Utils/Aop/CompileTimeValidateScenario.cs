using System;
using System.Reflection;

namespace DelftTools.Utils.Aop
{
    /// <summary>
    /// Helperclass to restrict the methods to be targeted by an 
    /// attribute implementing OnMethodBoundaryAspect
    /// </summary>
    public class CompileTimeValidateScenario
    {
        /// <summary>
        /// The criteria for public property setter methods
        /// </summary>
        /// <param name="methodBase"></param>
        /// <returns></returns>
        public static bool ValidateForPublicPropertySetter(MethodBase methodBase)
        {
            if (!methodBase.IsPublic)
            {
                return false;
            }
            if (!methodBase.Name.StartsWith("set_"))
            {
                return false;
            }
            return true;
        }
        /// <summary>
        /// The criteria to restrict an attribute to a specific type at compile time
        /// </summary>
        /// <param name="methodBase"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        public static bool RestrictPropertySetterToSpecificType(MethodBase methodBase, Type type)
        {
            if (methodBase.GetParameters() == null)
            {
                return false;
            }
            if (methodBase.GetParameters().Length != 1)
            {
                return false;
            }
            return methodBase.GetParameters()[0].ParameterType == type;
        }


        /// <summary>
        /// The criteria to restrict an attribute to a specific interfaceType at compile time
        /// </summary>
        /// <param name="methodBase"></param>
        /// <param name="interfaceType"></param>
        /// <returns></returns>
        public static bool RestrictPropertySetterToSpecificImplementation(MethodBase methodBase, Type interfaceType)
        {
            if (methodBase.GetParameters() == null)
            {
                return false;
            }
            if (methodBase.GetParameters().Length != 1)
            {
                return false;
            }

            Type type = methodBase.GetParameters()[0].ParameterType;
            string interfaceFullName = interfaceType.FullName;
            return type.GetInterface(interfaceFullName) != null;
        }



    }
}