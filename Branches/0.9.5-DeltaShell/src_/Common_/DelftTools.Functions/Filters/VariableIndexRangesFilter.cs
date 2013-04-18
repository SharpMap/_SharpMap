using System;
using System.Collections.Generic;
using DelftTools.Functions.Tuples;

namespace DelftTools.Functions.Filters
{
    public class VariableIndexRangesFilter: IVariableFilter
    {
        private IVariable variable;
        private readonly IList<Pair<int, int>> indexRanges;

        public long Id { get; set; }

        public VariableIndexRangesFilter(IVariable variable)
            : this(variable, new List<Pair<int, int>>())
        {
            
        }
        public VariableIndexRangesFilter(IVariable variable, IList<Pair<int,int>> indexRanges)
        {
            this.variable = variable;
            this.indexRanges = indexRanges;//get a clone?
        }

        public IVariable Variable
        {
            get { return variable; }
            set { variable = value; }
        }

        public IVariableFilter Intersect(IVariableFilter filter)
        {
            if (filter == null)
            {
                return (IVariableFilter) Clone();
            }
            throw new NotImplementedException();
        }


        #region ICloneable Members

        public object Clone()
        {
            //todo: disconnect the indexRanges?
            return new VariableIndexRangesFilter(variable,indexRanges);
        }
        public IList<Pair<int, int>> IndexRanges
        {
            get { return indexRanges;}
        }
        #endregion
    }
}