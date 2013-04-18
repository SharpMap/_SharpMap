using System.Collections.Generic;

namespace DelftTools.Functions.Generic
{
    public class MultiDimensionalArrayEnumerator<T> : MultiDimensionalArrayEnumerator, IEnumerator<T>
    {
        public MultiDimensionalArrayEnumerator(MultiDimensionalArray array) : base(array)
        {
        }

        public void Dispose()
        {
        }

        public new T Current
        {
            get { return (T)base.Current; }
        }
    }
}