using System;

namespace DelftTools.Functions.Generic
{
    public static class ArrayExtensions
    {
        public static IMultiDimensionalArray<T> ToMultiDimensionalArray<T>(this T[,] values) where T : IComparable
        {
            return (MultiDimensionalArray<T>)values;
        }
    }
}