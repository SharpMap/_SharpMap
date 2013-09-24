using GeoAPI.Extensions.Networks;
using NetTopologySuite.Extensions.Coverages;

namespace NetTopologySuite.Extensions.Networks
{
    
    public static class BranchFeatureExtensions
    {
        public static NetworkLocation ToNetworkLocation(this IBranchFeature branchFeature)
        {
            return new NetworkLocation(branchFeature.Branch, branchFeature.Chainage);
        }
    }
}