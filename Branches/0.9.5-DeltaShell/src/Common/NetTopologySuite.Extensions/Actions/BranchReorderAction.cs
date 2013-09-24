using DelftTools.Utils.Collections.Extensions;
using DelftTools.Utils.Editing;
using GeoAPI.Extensions.Networks;

namespace NetTopologySuite.Extensions.Actions
{
    public class BranchReorderAction : EditActionBase
    {
        public BranchReorderAction(INetwork network, int oldIndex, int newIndex) : base("Reorder branch position")
        {
            Network = network;
            OldIndex = oldIndex;
            NewIndex = newIndex;

            Branch = Network.Branches[oldIndex];
        }

        public IBranch Branch { get; private set; }

        public int OldIndex { get; set; }

        public int NewIndex { get; set; }

        public INetwork Network { get; set; }

        public void Execute()
        {
            Network.BeginEdit(this);
            Network.Branches.Move(OldIndex, NewIndex);
            Network.EndEdit();
        }
    }
}