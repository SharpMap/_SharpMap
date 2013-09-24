using System;
using System.Collections.Generic;
using DelftTools.Functions.DelftTools.Utils.Tuples;

namespace DelftTools.Functions.Filters
{
    /// <summary>
    /// Variable ValueRanges is a VariableIndexRangesFilter with a custom constructor
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class VariableValueRangesFilter<T>:VariableIndexRangesFilter where T : IComparable<T>
    {
        public VariableValueRangesFilter(IVariable variable, IEnumerable<Pair<T, T>> ranges)
            : base(variable, ConvertRanges(variable.Values,ranges))
        {
        }

        private static IList<Pair<int, int>> ConvertRanges(IMultiDimensionalArray values, IEnumerable<Pair<T, T>> ranges)
        {
            IList<Pair<int, int>> indexRanges = new List<Pair<int, int>>();
           


            
            foreach (Pair<T,T> p in ranges)
            {
                if(p.First.CompareTo((T) values[values.Count-1])<0||p.Second.CompareTo((T) values[0])>0)
                {
                    // throw new ArgumentException("Filtered range contains no data.");
                }


                int firstIndex = FindNearest( values, p.First);
                int secondIndex = FindNearest( values, p.Second);



                indexRanges.Add(new Pair<int, int>(firstIndex, secondIndex));
            }
            return indexRanges;
        }

        private static int FindNearest(IMultiDimensionalArray values, T first)
        {
            var x0 = (IComparable) first;

            if (x0.CompareTo(values[0]) <= 0 || values.Count == 1)
            {
                return 0;
            }
            for (int i = 1; i < values.Count; i++)
            {

                if (x0.CompareTo(values[i]) <= 0)
                {
                    return i;
                }
            }
            return values.Count - 1;
        }
    }
}