using System;
using System.Globalization;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using DelftTools.Utils;
using log4net;

namespace DelftTools.Functions
{
    public class MultiDimensionalArrayHelper
    {
        private static readonly ILog log = LogManager.GetLogger(typeof (MultiDimensionalArrayHelper));

        /// <summary>
        /// Check to see if the indexes fit in the new shape. 
        /// </summary>
        /// <param name="shape"></param>
        /// <param name="index"></param>
        /// <returns></returns>
        public static bool IsIndexWithinShape(int[] index, int[] shape)
        {
            for (int i = 0; i < shape.Length; i++)
            {
                if (shape[i] == 0) return false; //nothing fits in array with 0's in shape
            }
            for (int i = 0; i < index.Length; i++) //speed up
            {
                //number of indexes exceeds the new shape. Only add the dimension 0.
                if (i >= shape.Length)
                    if (index[i] != 0)
                    {
                        return false;
                    }
                    else continue; //index fits so check no more
                //check dimension size against the new shape
                if (index[i] >= shape[i])
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Convert a single dimensional index1d to a MD using current stride
        /// </summary>
        /// <param name="index1d"></param>
        /// <param name="stride"></param>
        /// <returns></returns>
        public static int[] GetIndex(int index1d, int[] stride)
        {
            if (stride.Length == 1)
                return new[] {index1d};

            var index = new int[stride.Length];
            for (int i = 0; i < stride.Length; i++)
            {
                index[i] = index1d / stride[i];
                index1d -= index[i]*stride[i];
            }

            return index;
        }

        /// <summary>
        /// Converts indexes to a single dimensional offset.
        /// </summary>
        /// <param name="index"></param>
        /// <param name="stride"></param>
        /// <returns></returns>
        public static int GetIndex1d(int[] index, int[] stride)
        {
            int offset = 0;
            for (int i = 0; i < stride.Length; i++)
            {
                //ignore dimensions for which no data available. This happens when we go from to N+1 D
                if (i < index.Length)
                {
                    offset += (index[i])*stride[i];
                }
            }
            return offset;
        }

        /// <summary>
        /// Calculate the stride vector for a given shape.
        /// </summary>
        /// <param name="shape"></param>
        /// <returns></returns>
        public static int[] GetStride(int[] shape)
        {
            var result = new int[shape.Length];
            int product = 1;
            for (int i = shape.Length - 1; i >= 0; i--)
            {
                result[i] = product;
                product *= (shape[i] == 0 ? 1 : shape[i]);
            }

            return result;
        }

        /// <summary>
        /// Returns the length of the underlying one dimensional array for a given shape
        /// </summary>
        /// <param name="shape"></param>
        /// <returns></returns>
        public static int GetTotalLength(int[] shape)
        {
            int totalLength = 1;

            for (int i = 0; i < shape.Length; i++)
            {
                totalLength *= shape[i];
            }

            return totalLength;
        }

        public static int[] GetShape(Array array)
        {
            var shape = new int[array.Rank];
            for (int i = 0; i < array.Rank; i++)
            {
                shape[i] = array.GetLength(i);
            }

            return shape;
        }

        public static bool IncrementIndex(int[] index, int[] shape, int dimension)
        {
            //log.DebugFormat("Incrementing index: index[0] - {0}, shape[0] - {1}", index[0], shape[0]);

            if (dimension < 0)
            {
                return false;
            }

            index[dimension]++;

            if (index[dimension] < shape[dimension])
            {
                return true;
            }

            if (dimension >= 0)
            {
                for (int i = dimension; i < shape.Length; i++)
                {
                    index[i] = 0;
                }

                return IncrementIndex(index, shape, dimension - 1);
            }

            return false;
        }

        public static bool ShapesAreEqualExceptFirstDimension(int[] shape1, int[] shape2)
        {
            if (shape1.Length != shape2.Length)
            {
                return false;
            }


            for (int i = 1; i < shape1.Length; i++)
            {
                if (shape1[i] != shape2[i])
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Gets the index at which to insert a value and remain sorted.
        /// </summary>
        public static int GetInsertionIndex<T>(T theObject, IList<T> list) where T : IComparable
        {
            if (list.Count == 0 || !Comparer.IsBigger(theObject, list[0])) //if the object is smaller then the 1st we add it to the start.
            {
                return 0;
            }

            if (Comparer.IsBigger(theObject, list[list.Count - 1])) //if the object is larger then the last, we extend the list.
            {
                return list.Count;
            }
                
            // TODO: optimize it, use smth. like quicksort (our values are ordered!)
            for (var i = 0; i < list.Count-1; i++)
            {
                if (Comparer.IsEqual(list[i], theObject)) return i;

                if (Comparer.IsBigger(theObject, list[i]) && Comparer.IsBigger(list[i+1], theObject))
                {
                    return i + 1;
                }
            }

            return list.Count;
        }

        private static void DumpToStringBuilder(IMultiDimensionalArray array, StringBuilder sb, int[] indexes, int dimension)
        {
            if (dimension >= array.Rank)
            {
                return;
            }

            for (int i = 0; i < array.Shape[dimension]; i++, indexes[dimension]++)
            {
                if (dimension == array.Rank - 1)
                {
                    sb.Append(array[indexes] ?? "<null>");
                    if (i != array.Shape[dimension] - 1)
                    {
                        sb.Append(", ");
                    }
                }
                else
                {
                    sb.Append("{");
                    DumpToStringBuilder(array, sb, indexes, dimension + 1);
                    sb.Append("}");
                    if (i < array.Shape[dimension] - 1)
                    {
                        sb.Append(", ");
                    }
                }
            }
            indexes[dimension] = 0; // reset
        }

        public static string ToString(IMultiDimensionalArray array)
        {
            var indexes = new int[array.Rank];

            var culture = Thread.CurrentThread.CurrentCulture;
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;

            var sb = new StringBuilder();
            sb.Append("{");
            DumpToStringBuilder(array, sb, indexes, 0);
            sb.Append("}");

            var str = sb.ToString();

            Thread.CurrentThread.CurrentCulture = culture;
            
            return str;
        }

        public static int[] Detect2DShapeFromString(string str)
        {
            var rowCount = str.Count(c => c == '}') - 1;
            var firstRow = str.Split('}')[0].TrimStart('{', ' ');
            var columnCount = firstRow.Split(',').Length;

            return new[] { rowCount, columnCount };
        }

        public static int[] DetectShapeFromString(string text)
        {
            var rank = text.TakeWhile(c => c == ' ' || c == '{').Count(c => c == '{');

            if (rank > 2)
            {
                throw new NotImplementedException();
            }

            // TODO: extend it for multiple dimensions
            if (rank == 2)
            {
                return Detect2DShapeFromString(text);
            }
            
            // 1d
            return new[] { text.Split(',').Count() };
        }

        private static IEnumerable<int> DetectFirstDimensionSize(string text, int rank)
        {
/*
            // reduce dimensionality by removing one '{', '}'
            var text2 = text.Trim(' ');
            var text3 = text2.Substring(1, text2.Length - 1);

*/
            throw new NotImplementedException();
        }
    }
}