using System;
using System.Collections;

namespace DelftTools.Functions
{
    public class MultiDimensionalArrayEnumerator : IEnumerator
    {
        private IMultiDimensionalArray array;
        private int[] index;

        public MultiDimensionalArrayEnumerator(MultiDimensionalArray array)
        {
            this.array = array;
            index = new int[array.Rank];
            Reset();
        }

        public bool MoveNext()
        {
            //instead of using count this should be faster.
            foreach (var i in array.Shape)
            {
                if (i == 0)
                {
                    return false;
                }
            }
            
            return MultiDimensionalArrayHelper.IncrementIndex(index, array.Shape, array.Rank - 1);
        }
        
        public void Reset()
        {
            for (var i = 0; i < array.Rank; i++)
            {
                index[i] = 0;
            }

            index[array.Rank - 1] = -1;
        }

        public object Current
        {
            get
            {
                if (index[array.Rank - 1] < 0)
                {
                    throw new IndexOutOfRangeException("Use MoveNext to select the next element");
                }

                return array[index];
            }
        }
    }
}