namespace DelftTools.Functions.Generic
{
    public static class ArrayExtensions
    {
        public static IMultiDimensionalArray<T> ToMultiDimensionalArray<T>(this T[,] values)
        {
            return (MultiDimensionalArray<T>)values;
        }
    }
}