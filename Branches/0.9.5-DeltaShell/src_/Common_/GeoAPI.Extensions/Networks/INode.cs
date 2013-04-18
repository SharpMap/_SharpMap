using System.Collections.Generic;
using DelftTools.Utils.Collections.Generic;

namespace GeoAPI.Extensions.Networks
{
    public interface INode: INetworkFeature
    {
        IList<IBranch> IncomingBranches { get; set; }
        IList<IBranch> OutgoingBranches { get; set; }

        IEventedList<INodeFeature> NodeFeatures { get; set; }

        bool IsBoundaryNode { get; }
    }
}