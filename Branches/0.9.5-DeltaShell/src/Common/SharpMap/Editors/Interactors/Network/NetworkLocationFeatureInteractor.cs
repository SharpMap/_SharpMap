using DelftTools.Utils.Editing;
using GeoAPI.Extensions.Coverages;
using GeoAPI.Extensions.Feature;
using GeoAPI.Extensions.Networks;
using GeoAPI.Geometries;
using NetTopologySuite.Extensions.Coverages;
using NetTopologySuite.Extensions.Geometries;
using NetTopologySuite.Extensions.Networks;
using SharpMap.Api;
using SharpMap.Api.Editors;
using SharpMap.Editors.Snapping;
using SharpMap.Layers;
using SharpMap.Styles;

namespace SharpMap.Editors.Interactors.Network
{
    public class NetworkLocationFeatureInteractor : BranchFeatureInteractor<NetworkLocation>
    {
        public NetworkLocationFeatureInteractor(ILayer layer, IFeature feature, VectorStyle vectorStyle, IEditableObject editableObject)
            : base(layer, feature, vectorStyle, editableObject)
        {
        }

        public override bool AllowSingleClickAndMove()
        {
            return true;
        }

        public override void Stop(SnapResult snapResult)
        {
            var networkLocation = (INetworkLocation)SourceFeature;
            if (null == networkLocation)
            {
                return;
            }

            var branch = snapResult.SnappedFeature as IBranch;
            if (null == branch)
            {
                return;
            }

            var network = Network ?? branch.Network; // network must e known
            if (network == null)
            {
                return;
            }

            if (networkLocation.Branch != branch)
            {
                networkLocation.Branch = branch;
            }
            // todo move update distance to AddBranchFeatureToNearestBranch?
            var distanceInGeometryLength = GeometryHelper.Distance((ILineString) networkLocation.Branch.Geometry, TargetFeature.Geometry.Coordinates[0]);
            var distanceInCalculationLength = NetworkHelper.CalculationChainage(branch, distanceInGeometryLength);
            networkLocation.Chainage = BranchFeature.SnapChainage(networkLocation.Branch.Length, distanceInCalculationLength);
        }
    }
}