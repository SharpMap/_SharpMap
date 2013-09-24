using DelftTools.Utils.Editing;
using GeoAPI.Extensions.Networks;

namespace NetTopologySuite.Extensions.Actions
{
    public class BranchReverseAction : EditActionBase
    {
        /// <summary>
        /// The branch that has been reversed
        /// </summary>
        public IBranch ReversedBranch { get; set; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="branch">The reversed branch, assumed to be not null</param>
        public BranchReverseAction(IBranch branch) : base("Reverse branch " + branch.GetType().Name.ToLower())
        {
            ReversedBranch = branch;
        }
    }
}