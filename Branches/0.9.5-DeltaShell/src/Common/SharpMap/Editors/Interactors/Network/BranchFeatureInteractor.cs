using DelftTools.Utils.Editing;
using GeoAPI.Extensions.Networks;
using GeoAPI.Geometries;
using GisSharpBlog.NetTopologySuite.Geometries;
using GisSharpBlog.NetTopologySuite.LinearReferencing;
using NetTopologySuite.Extensions.Geometries;
using NetTopologySuite.Extensions.Networks;
using SharpMap.Api;
using SharpMap.Api.Editors;
using SharpMap.Editors.Snapping;
using SharpMap.Layers;
using SharpMap.Rendering;
using GeoAPI.Extensions.Feature;
using SharpMap.Styles;

namespace SharpMap.Editors.Interactors.Network
{
    public class BranchFeatureInteractor<T> : PointInteractor, IBranchMaintainableInteractor, INetworkFeatureInteractor where T : IBranchFeature, new()
    {
        private bool moving;

        public BranchFeatureInteractor(ILayer layer, IFeature feature, VectorStyle vectorStyle, IEditableObject editableObject)
            : base(layer, feature, vectorStyle, editableObject)
        {
        }

        protected override bool AllowMoveCore()
        {
            return true;
        }

        protected override bool AllowDeletionCore()
        {
            return true;
        }

        public override bool MoveTracker(TrackerFeature trackerFeature, double deltaX, double deltaY, SnapResult snapResult)
        {
            moving = true;
            return base.MoveTracker(trackerFeature, deltaX, deltaY, snapResult);
        }

        public INetwork Network { get; set; }

        public override void Stop()
        {
            Stop(null);
        }

        private void Stop(SnapResult snapResult)
        {
            Stop(snapResult,false);
        }

        public void Stop(SnapResult snapResult, bool stayOnSameBranch)
        {
            if (!(SourceFeature is IBranchFeature)) return;
            if (null != snapResult && null == snapResult.SnappedFeature as IBranch) return;

            var branchFeature = (IBranchFeature)SourceFeature;

            if (moving)
            {
                branchFeature.SetBeingMoved(true);
            }

            var tolerance = Layer == null ? Tolerance : MapHelper.ImageToWorld(Layer.Map, 1);

            var branch = branchFeature.Branch;
            INetwork network = Network ?? branch.Network; // network must be known

            if (!stayOnSameBranch && branch != null)
            {
                branch.BranchFeatures.Remove(branchFeature);
                branchFeature.Branch = null;
            }

            base.Stop(); // set new geometry

            if (!stayOnSameBranch)
            {
                NetworkHelper.AddBranchFeatureToNearestBranch(network.Branches, branchFeature, tolerance);
            }

            if (moving)
            {
                branchFeature.SetBeingMoved(false);
                moving = false;
            }

            // todo move update distance to AddBranchFeatureToNearestBranch?
            double distance = GeometryHelper.Distance((ILineString) branchFeature.Branch.Geometry, branchFeature.Geometry.Coordinates[0]);
            if (branchFeature.Branch.IsLengthCustom)
            {
                distance *= branchFeature.Branch.Length/branchFeature.Branch.Geometry.Length;
            }
            branchFeature.Chainage = BranchFeature.SnapChainage(branchFeature.Branch.Length, distance);
            if (branchFeature.Geometry.GetType() == typeof(LineString))
            {
                UpdateLineGeometry(branchFeature);
            }
        }

        private static void UpdateLineGeometry(IBranchFeature branchFeature)
        {
            var lengthIndexedLine = new LengthIndexedLine(branchFeature.Branch.Geometry);
            branchFeature.Geometry = lengthIndexedLine.ExtractLine(branchFeature.Chainage, branchFeature.Chainage + branchFeature.Length);
        }

        public override void Delete()
        {
            base.Delete();
            IBranch branch = ((IBranchFeature)SourceFeature).Branch;
            if (null != branch)
            {
                branch.BranchFeatures.Remove((IBranchFeature) SourceFeature);
            }
        }
    }
}