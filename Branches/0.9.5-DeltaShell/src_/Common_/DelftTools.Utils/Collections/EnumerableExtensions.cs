using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace DelftTools.Utils.Collections
{
    public static class EnumerableExtensions
    {
        public static void ForEach<T>(this IEnumerable<T> source, Action<T> action)
        {
            foreach (var item in source)
            {
                action(item);
            }
        }

        public static void ForEach<T>(this IEnumerable<T> source, Action<T, int> action)
        {
            var i = 0;
            foreach (var item in source)
            {
                action(item, i);
                i++;
            }
        }

        public static bool HasExactlyOneValue(this IEnumerable values)
        {
            int i = 0;
            foreach (object o in values)
            {
                i++;
                if (i > 1)
                    break;
            }
            return (i == 1);
        }

        /// <summary>
        /// Returns values1 except values2.
        /// </summary>
        /// <param name="values1"></param>
        /// <param name="values2"></param>
        /// <returns></returns>
        public static IEnumerable Except(this IEnumerable values1, IList values2)
        {
            foreach (object o in values1)
            {
                if (!values2.Contains(o))
                {
                    yield return o;
                }
            }
        }

        public static string ConvertToString(this IEnumerable<int> values)
        {
            return String.Join(",", values.Select(v => v.ToString()).ToArray());
        }

        public static IEnumerable<T> QuickSort<T>(this IEnumerable<T> list)
            where T : IComparable
        {
            if (!list.Any())
            {
                return Enumerable.Empty<T>();
            }
            T pivot = (T) list.First();
            IEnumerable<T> smaller = list.Where(item => item.CompareTo(pivot) <= 0).QuickSort();
            IEnumerable<T> larger = list.Where(item => item.CompareTo(pivot) > 0).QuickSort();
            
            return smaller.Concat(new[] { pivot }).Concat(larger);
        }

        public static IEnumerable<int> Substract(this IEnumerable<int> index1, IEnumerable<int> index2)
        {
            var i1 = index1.GetEnumerator();
            var i2 = index2.GetEnumerator();

            while (i1.MoveNext() && i2.MoveNext())
            {
                yield return i1.Current - i2.Current;
            }
        }

        public static IEnumerable<int> Add(this IEnumerable<int> index1, IEnumerable<int> index2)
        {
            var i1 = index1.GetEnumerator();
            var i2 = index2.GetEnumerator();

            while (i1.MoveNext() && i2.MoveNext())
            {
                yield return i1.Current + i2.Current;
            }
        }

        public static IEnumerable<int> Add(this IEnumerable<int> index1, int value)
        {
            var i1 = index1.GetEnumerator();

            while (i1.MoveNext())
            {
                yield return i1.Current + value;
            }
        }

        public static IEnumerable<int> Substract(this IEnumerable<int> index1, int value)
        {
            var i1 = index1.GetEnumerator();

            while (i1.MoveNext())
            {
                yield return i1.Current - value;
            }
        }
    }
}