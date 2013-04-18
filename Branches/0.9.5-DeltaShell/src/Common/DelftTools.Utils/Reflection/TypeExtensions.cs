using System;

namespace DelftTools.Utils.Reflection
{
    public static class TypeExtensions
    {
        public static Type GetGenericTypeRecursive(this Type type, Type targetType)
        {
            if (type == null)
                throw new ArgumentNullException("type");

            if (targetType == null)
                throw new ArgumentNullException("targetType");

            if (type == targetType)
                return targetType;

            if (!(targetType.IsAssignableFrom(type)))
                throw new ArgumentException("The type to analyze must be an instance of or an sub type of the target type");

            var baseType = type.BaseType;

            return type.IsGenericType ? type.GetGenericArguments()[0] : 
                baseType == null ? type : GetGenericTypeRecursive(type.BaseType, targetType);
        }
    }
}