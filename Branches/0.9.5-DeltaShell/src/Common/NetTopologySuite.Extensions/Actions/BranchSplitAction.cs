using DelftTools.Utils.Editing;

using GeoAPI.Extensions.Networks;

namespace NetTopologySuite.Extensions.Actions
{
    public class BranchSplitAction : EditActionBase
    {
        public BranchSplitAction() : base("Split branch") { }

        /// <summary>
        /// The branch that was split
        /// </summary>
        public IBranch SplittedBranch { get; set; }

        /// <summary>
        /// The new branch the was created because of the split
        /// </summary>
        public IBranch NewBranch { get; set; }
    }
}