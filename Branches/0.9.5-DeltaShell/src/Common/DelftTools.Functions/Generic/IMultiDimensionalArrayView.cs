using System;

namespace DelftTools.Functions.Generic
{
    public interface IMultiDimensionalArrayView<T> : IMultiDimensionalArray<T>, IMultiDimensionalArrayView
    {
    }
}