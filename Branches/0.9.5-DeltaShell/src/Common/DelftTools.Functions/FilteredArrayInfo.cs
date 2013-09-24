using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DelftTools.Functions.Filters;

namespace DelftTools.Functions
{
    /// <summary>
    /// Provides logic to determince Origin,Shape,Size and Stride for a variable given a list of VariableAggregationFilters.
    /// TODO: name is confusing, this class has something to do with Functions and variable filters
    /// TODO: Maybe combine with other filters???
    /// TODO: check NetCDF Section, Range, maybe use something similar
    /// </summary>
    public class FilteredArrayInfo
    {
        private readonly IList<IVariableFilter> filters;
        private readonly IFunction variable;
        private readonly int rank;
        public FilteredArrayInfo(IFunction variable, IList<IVariableFilter> filters)
        {
            this.filters = filters;
            this.variable = variable;
            this.rank = variable.Arguments.Count;
        }

        /// <summary>
        /// Size in the actual array..more like a span
        /// </summary>
        public int[] Size
        {
            get
            {
                int[] size = variable.Arguments.Select(a => a.Values.Count).ToArray();

                // override shape for any value filter
                foreach (var filter in filters.OfType<VariableAggregationFilter>())
                {
                    var variableIndex = variable.Arguments.IndexOf(filter.Variable);
                    if (variableIndex != -1)
                    {
                        size[variableIndex] = filter.MaxIndex - filter.MinIndex + 1;
                    }
                }

                foreach (var filter in filters.OfType<IVariableValueFilter>())
                {
                    if (filter.Values.Count > 1)
                    {
                        throw new NotSupportedException(
                            "Only single value-based selection or range-based selection are supported, VariableIndexRangeFilter [minIndex, maxIndex]");
                    }
                    var variableIndex = variable.Arguments.IndexOf(filter.Variable);
                    size[variableIndex] = 1;
                }
                return size;
            }
        }

        public int[] Origin
        {
            get
            {
                int[] origin = Enumerable.Repeat(0, variable.Arguments.Count).ToArray();
                // override shape for any value filter
                foreach (var filter in filters.OfType<VariableAggregationFilter>())
                {
                    var variableIndex = variable.Arguments.IndexOf(filter.Variable);
                    if (variableIndex != -1)
                    {
                        origin[variableIndex] = filter.MinIndex;
                    }
                }

                foreach (var filter in filters.OfType<IVariableValueFilter>())
                {
                    if (filter.Values.Count > 1)
                    {
                        throw new NotSupportedException(
                            "Only single value-based selection or range-based selection are supported, VariableIndexRangeFilter [minIndex, maxIndex]");
                    }
                    var variableIndex = variable.Arguments.IndexOf(filter.Variable);
                    int valueIndex = filter.Variable.Values.IndexOf(filter.Values[0]);

                    if (filter.Variable.ValueType == typeof(DateTime))
                    {
                        valueIndex = IndexOfDateTime(filter.Variable.Values, (DateTime)filter.Values[0]);
                    }
                    if (valueIndex != -1)
                    {
                        origin[variableIndex] = valueIndex;
                    }


                }


                return origin;
            }
        }

        private static int IndexOfDateTime(IList values, DateTime time)
        {
            DateTime startTime = new DateTime(1970, 1, 1);
            int i = 0;
            foreach (DateTime time2 in values)
            {
                if ((long)((time2 - startTime).TotalMilliseconds) == (long)((time - startTime).TotalMilliseconds))
                {
                    return i;
                }

                i++;
            }

            return -1;
        }

        public int[] Shape
        {
            get
            {
                int[] shape = variable.Arguments.Select(a => a.Values.Count).ToArray();


                // override shape for any value filter
                foreach (var filter in filters.OfType<VariableAggregationFilter>())
                {
                    var variableIndex = variable.Arguments.IndexOf(filter.Variable);
                    if (variableIndex != -1)
                    {
                        shape[variableIndex] = filter.Count;
                    }
                }
                foreach (var filter in filters.OfType<IVariableValueFilter>())
                {
                    if (filter.Values.Count > 1)
                    {
                        throw new NotSupportedException(
                            "Only single value-based selection or range-based selection are supported, VariableIndexRangeFilter [minIndex, maxIndex]");
                    }
                    var variableIndex = variable.Arguments.IndexOf(filter.Variable);
                    shape[variableIndex] = 1;
                }
                return shape;
            }
        }

        

        public int[] Stride
        {
            get
            {
                int[] stride = Enumerable.Repeat(1, variable.Arguments.Count).ToArray();

                // override shape for any value filter
                foreach (var filter in filters.OfType<VariableAggregationFilter>())
                {
                    var variableIndex = variable.Arguments.IndexOf(filter.Variable);
                    if (variableIndex != -1)
                    {
                        stride[variableIndex] = filter.StepSize;
                    }
                }
                return stride;
            }
        }
            
    }
}