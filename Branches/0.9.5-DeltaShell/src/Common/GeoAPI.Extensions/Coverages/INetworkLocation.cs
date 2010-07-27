using GeoAPI.Extensions.Networks;

namespace GeoAPI.Extensions.Coverages
{
    // TODO: INetwork location cannot be IBranchFeature because branchfeatures end up in 
    // Branch.Branchfeatures. Dont want to add NetworkLocation to branch
    public interface INetworkLocation: IBranchFeature
    {
    }
}