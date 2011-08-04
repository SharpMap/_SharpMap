using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DelftTools.Utils
{
    public class MathUtils
    {

        /// <summary>
        /// Minimum value for a list of IComparable elements
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="values"></param>
        /// <returns></returns>
        public static T Min<T>(IEnumerable<T> values)
        {
            return Min<T>(values, Comparer<T>.Default);
        }

        private static T Min<T>(IEnumerable<T> values, IComparer<T> comparer)
        {
            bool first = true;
            T result = default(T);
            foreach (T value in values)
            {
                if (first)
                {
                    result = value;
                    first = false;
                }
                else
                {
                    if (comparer.Compare(result, value) > 0)
                    {
                        result = value;
                    }
                }
            }
            return result;
        }

        /// <summary>
        /// Maximum value for a list of IComparable elements
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="values"></param>
        /// <returns></returns>
        public static T Max<T>(IEnumerable<T> values)
        {
            return Max<T>(values, Comparer<T>.Default);
        }

        private static T Max<T>(IEnumerable<T> values, IComparer<T> comparer)
        {
            bool first = true;
            T result = default(T);
            foreach (T value in values)
            {
                if (first)
                {
                    result = value;
                    first = false;
                }
                else
                {
                    if (comparer.Compare(result, value) < 0)
                    {
                        result = value;
                    }
                }
            }
            return result;
        }
    }
}