using System;
using DelftTools.Utils;
using GeoAPI.Extensions.Networks;

namespace NetTopologySuite.Extensions.Actions
{
    public class BranchSplitAction:IEditAction
    {
        /// <summary>
        /// The branch that was split
        /// </summary>
        public IBranch SplittedBranch { get; set; }

    
        /// <summary>
        /// The new branch the was created because of the split
        /// </summary>
        public IBranch NewBranch { get; set; }


        public string Name
        {
            get { return "Split branch"; }
        }
    }
}