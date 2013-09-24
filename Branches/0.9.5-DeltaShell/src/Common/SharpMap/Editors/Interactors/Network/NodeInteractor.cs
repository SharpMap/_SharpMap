using System.Collections.Generic;
using DelftTools.Utils.Editing;
using GeoAPI.Extensions.Networks;
using SharpMap.Api;
using SharpMap.Api.Editors;
using SharpMap.Layers;
using GeoAPI.Extensions.Feature;
using SharpMap.Styles;

namespace SharpMap.Editors.Interactors.Network
{
    /// <summary>
    /// Feature editor for nodes in the network.
    /// Deleting of nodes by the user is not supported; user also does not add nodes
    /// if it is required set AllowDeletion() to true and add new NodeBranchTopology rule
    /// cfr BranchNodeTopology and call to branchNodeTopology.OnBranchDeleted(branch) in 
    /// HydroNetworkEditorMapTool
    /// </summary>
    public class NodeInteractor : PointInteractor
    {
        public NodeInteractor(ILayer layer, IFeature feature, VectorStyle vectorStyle, IEditableObject editableObject)
            : base(layer, feature, vectorStyle, editableObject)
        {
        }
        
        public override void Stop()
        {
            base.Stop();

            var node = SourceFeature as INode;
            var network = node.Network;

            var branchNodeTopology = new BranchNodeTopology();
            branchNodeTopology.Branches = network.Branches;
            branchNodeTopology.Nodes = network.Nodes;
            branchNodeTopology.Layer = Layer;
            branchNodeTopology.OnNodeMoved(node);
        }

        protected override bool AllowMoveCore()
        {
            return true;
        }

        public override bool AllowSingleClickAndMove()
        {
            return true;
        }

        protected override bool AllowDeletionCore()
        {
            return false;
        }

        public override IEnumerable<IFeatureRelationInteractor> GetFeatureRelationInteractors(IFeature feature)
        {
            yield return new NodeToBranchRelationInteractor();
        }

    }
}