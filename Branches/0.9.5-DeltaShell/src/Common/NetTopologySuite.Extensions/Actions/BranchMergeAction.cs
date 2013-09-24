using DelftTools.Utils.Editing;

using GeoAPI.Extensions.Networks;

namespace NetTopologySuite.Extensions.Actions
{
    /// <summary>
    /// Action that a node is removed and merging the branches
    /// </summary>
    public class BranchMergeAction:EditActionBase
    {
        public BranchMergeAction() : base("Merge branches") { }

        /// <summary>
        /// The node that was removed.
        /// </summary>
        public INode RemovedNode { get; set; }

        /// <summary>
        /// The branch that was removed.
        /// </summary>
        public IBranch RemovedBranch { get; set; }
        
        /// <summary>
        /// The branch that was extended with the other branch 
        /// </summary>
        public IBranch ExtendedBranch { get; set; }
    }
}