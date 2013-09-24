using System;
using System.Collections;

namespace DelftTools.Functions
{
    public class MultiDimensionalArrayEnumerator : IEnumerator
    {
        private IMultiDimensionalArray array;
        private int[] index;
        private int[] shape;
        private int rank;

        public MultiDimensionalArrayEnumerator(MultiDimensionalArray array)
        {
            this.array = array;
            Reset();
        }

        public bool MoveNext()
        {
            //instead of using count this should be faster.
            foreach (var i in shape)
            {
                if (i == 0)
                {
                    return false;
                }
            }

            return MultiDimensionalArrayHelper.IncrementIndex(index, shape, rank - 1);
        }
        
        public void Reset()
        {
            shape = array.Shape;
            rank = array.Rank;

            index = new int[rank];
            for (var i = 0; i < rank; i++)
            {
                index[i] = 0;
            }

            index[rank - 1] = -1;
        }

        public object Current
        {
            get
            {
                if (index[rank - 1] < 0)
                {
                    throw new IndexOutOfRangeException("Use MoveNext to select the next element");
                }

                return array[index];
            }
        }
    }
}