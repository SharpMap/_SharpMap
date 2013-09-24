using System.Linq;
using GeoAPI.Extensions.Networks;
using NetTopologySuite.Extensions.Coverages;

namespace NetTopologySuite.Extensions.Networks
{
    public static class NetworkFeatureExtensions
    {
        public static NetworkLocation ToNetworkLocation(this INetworkFeature networkFeature)
        {
            var branchFeature = networkFeature as IBranchFeature;
            if (branchFeature != null)
            {
                return branchFeature.ToNetworkLocation();
            }

            var node = networkFeature as INode;
            if (node != null)
            {
                IBranch outgoingBranch = node.OutgoingBranches.FirstOrDefault();
                if (outgoingBranch != null)
                {
                    return new NetworkLocation(outgoingBranch, 0);
                }
                IBranch incomingBranch = node.IncomingBranches.First();
                return new NetworkLocation(incomingBranch, incomingBranch.Length);
            }
            return null;
        }
    }
}