using System;

namespace DelftTools.Utils
{
    public static class ComparableExtensions
    {
        public static bool IsBigger(this IComparable object1, IComparable object2)
        {
            return object1.CompareTo(object2) == 1;
        }

        public static bool IsSmaller(this IComparable object1, IComparable object2)
        {
            return object1.CompareTo(object2) == -1;
        }
    }
    public class Comparer
    {
        public static bool AreReferencesOrValuesEqual(object source, object target)
        {
            if (source is ValueType || target is ValueType)
            {
                if(source == null || target == null)
                {
                    return source == target;
                }

                return source.Equals(target);
            }
            
            return source == target;
        }


        public static bool IsEqual(IComparable object1, IComparable object2)
        {
            return object1.CompareTo(object2) == 0;
        }

        /// <summary>
        /// Returns true if object 1 is 'bigger' then object 2. 2 > 1 returns true
        /// </summary>
        /// <param name="object1"></param>
        /// <param name="object2"></param>
        /// <returns></returns>
        public static bool IsBigger(IComparable object1,IComparable object2)
        {
            return object1.CompareTo(object2) == 1;
        }

        /// <summary>
        /// Determines whether item is between lower and upper. Excluding bounds eg. 1 is not between 1 and 3, but 3 is.
        /// </summary>
        /// <returns></returns>
        public static bool IsBetween(IComparable previous,IComparable item,IComparable nextV)
        {
            return IsBigger(item, previous) && IsBigger(nextV, item);
        }
    }
}