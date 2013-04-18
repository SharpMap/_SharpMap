using System;
using System.Linq.Expressions;

namespace DelftTools.Utils.Reflection
{
    public static class PropertyInfoExtensions
    {
        public static string GetPropertyName<TClass,TProperty>(this TClass instance, Expression<Func<TClass,TProperty>> expression)
        {
            var member = expression.Body as MemberExpression;

            // If the method gets a lambda expression 
            // that is not a member access,
            // for example, () => x + y, an exception is thrown.
            if (member != null)
            {
                return member.Member.Name;
            }

            throw new ArgumentException(
                "'" + expression +
                "': is not a valid expression for this method");
        }

        public static bool IsPropertyOfType<TClass,TProperty>(this TClass instance, Expression<Func<TClass,TProperty>> expression, Type type)
        {
            var member = expression.Body as MemberExpression;

            // If the method gets a lambda expression 
            // that is not a member access,
            // for example, () => x + y, an exception is thrown.
            if (member != null)
            {
                return member.Member.ReflectedType == type;
            }
            
            throw new ArgumentException(
                "'" + expression +
                "': is not a valid expression for this method");

        }
    }
}