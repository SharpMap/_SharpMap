using DelftTools.Utils.Editing;
using GeoAPI.Extensions.Networks;
using GeoAPI.Geometries;
using NetTopologySuite.Extensions.Coverages;
using NetTopologySuite.Extensions.Networks;
using SharpMap.Api;
using SharpMap.Api.Editors;
using SharpMap.Layers;
using GeoAPI.Extensions.Feature;
using SharpMap.Styles;
using System.Collections.Generic;

namespace SharpMap.Editors.Interactors.Network
{
    public class BranchInteractor : LineStringInteractor, INetworkFeatureInteractor
    {
        public BranchInteractor(ILayer layer, IFeature feature, VectorStyle vectorStyle, IEditableObject editableObject)
            : base(layer, feature, vectorStyle, editableObject)
        {
            branchNodeTopology = new BranchNodeTopology();
        }

        private BranchNodeTopology branchNodeTopology;
        public virtual BranchNodeTopology BranchNodeTopology
        {
            get { return branchNodeTopology; }
            set { branchNodeTopology = value; }
        }

        public override void Stop()
        {
            IList<int> indices = new List<int>();
            for (int i = 0; i < SourceFeature.Geometry.Coordinates.Length; i++)
            {
                // TODO add support for Alltracker
                if (Trackers[i].Selected)
                {
                    indices.Add(i);
                }
            }
            foreach (IFeatureRelationInteractor topologyRule in FeatureRelationEditors)
            {
                topologyRule.StoreRelatedFeatures(SourceFeature, TargetFeature.Geometry, indices);
            }
            FeatureRelationEditors.Clear();
            // copy targetfeature geometry to source
            //#$#
            SourceFeature.Geometry = (IGeometry)TargetFeature.Geometry.Clone();
            //for (int i = 0; i < TargetFeature.Geometry.Coordinates.Length; i++)
            //{
            //    SourceFeature.Geometry.Coordinates[i] = (ICoordinate) TargetFeature.Geometry.Coordinates[i].Clone();
            //}
            //SourceFeature.Geometry.GeometryChangedAction();
            // #$#

            //base.Stop();
            branchNodeTopology.Branches = Network.Branches;
            branchNodeTopology.Nodes = Network.Nodes;
            branchNodeTopology.Layer = Layer;
            branchNodeTopology.OnBranchAdded((IBranch)SourceFeature);
        }

        public override IEnumerable<IFeatureRelationInteractor> GetFeatureRelationInteractors(IFeature feature)
        {
            yield return new BranchToBranchFeatureRelationInteractor<NetworkLocation>();
        }

        protected override bool AllowMoveCore()
        {
            return true;
        }
        protected override bool AllowDeletionCore()
        {
            return true;
        }

        public override void Delete()
        {
            IBranch branch = (IBranch)SourceFeature;
            branchNodeTopology.Branches = Network.Branches;
            branchNodeTopology.Nodes = Network.Nodes;
            branchNodeTopology.Layer = Layer;
            branchNodeTopology.OnBranchDeleting(branch);
            //base.Delete();
            Network.Branches.Remove((IBranch)SourceFeature);
            branchNodeTopology.OnBranchDeleted(branch);
        }

        public override void Add(IFeature feature)
        {
            NetworkHelper.AddChannelToHydroNetwork(Network, (IBranch)feature);
            
            // TODO: didn't we just set the nodes and order of this branch, why call OnBranchAdded here?!
            branchNodeTopology.Branches = Network.Branches;
            branchNodeTopology.Nodes = Network.Nodes;
            branchNodeTopology.Layer = Layer;
            branchNodeTopology.OnBranchAdded((IBranch)SourceFeature);
        }

        public INetwork Network { get; set; }
    }
}