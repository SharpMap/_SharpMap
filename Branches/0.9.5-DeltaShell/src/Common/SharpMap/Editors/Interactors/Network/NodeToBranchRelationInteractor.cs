using System;
using System.Collections.Generic;
using GeoAPI.Extensions.Feature;
using GeoAPI.Extensions.Networks;
using GeoAPI.Geometries;
using SharpMap.Api;
using SharpMap.Api.Delegates;
using SharpMap.Api.Editors;
using SharpMap.Editors.FallOff;

namespace SharpMap.Editors.Interactors.Network
{
    public class NodeToBranchRelationInteractor : FeatureRelationInteractor
    {
        IFeature lastFeature;
        ICoordinate lastCoordinate;
        IList<IFeature> lastRelatedFeatures;
        IList<IFeature> lastRelatedNewFeatures;
        readonly IList<IGeometry> lastRelatedFeatureGeometries = new List<IGeometry>();

        private IFeatureRelationInteractor CloneRule()
        {
            var nodeToBranchTopologyRule = new NodeToBranchRelationInteractor {FallOffPolicy = FallOffPolicy};
            return nodeToBranchTopologyRule;
        }

        List<List<IFeatureRelationInteractor>> activeInRules = new List<List<IFeatureRelationInteractor>>();
        List<List<IFeatureRelationInteractor>> activeOutRules = new List<List<IFeatureRelationInteractor>>();

        public override IFeatureRelationInteractor Activate(IFeature feature, IFeature cloneFeature, AddRelatedFeature addRelatedFeature, int level, IFallOffPolicy fallOffPolicy)
        {
            FallOffPolicy = fallOffPolicy ?? new NoFallOffPolicy();
            INode node;
            if (null != (node = (feature as INode)))
            {
                var cloneRule = (NodeToBranchRelationInteractor)CloneRule();
                cloneRule.Start(node, addRelatedFeature, level);
                return cloneRule;
            }
            return null;
        }

        public void Start(INode node, AddRelatedFeature addRelatedFeature, int level)
        {
            lastFeature = node;
            lastRelatedFeatureGeometries.Clear();
            lastRelatedFeatures = new List<IFeature>();
            lastRelatedNewFeatures = new List<IFeature>();

            lastCoordinate = (ICoordinate)node.Geometry.Coordinates[0].Clone();
            foreach (IBranch branch in node.IncomingBranches)
            {
                lastRelatedFeatures.Add(branch);
                var clone = (IBranch)branch.Clone();
                lastRelatedNewFeatures.Add(clone);
                lastRelatedFeatureGeometries.Add((IGeometry)clone.Geometry.Clone());

                if (null != addRelatedFeature)
                {
                    activeInRules.Add(new List<IFeatureRelationInteractor>());
                    addRelatedFeature(activeInRules[activeInRules.Count - 1], branch, clone, level);
                }
            }
            foreach (var branch in node.OutgoingBranches)
            {
                lastRelatedFeatures.Add(branch);
                var clone = (IBranch) branch.Clone();
                lastRelatedNewFeatures.Add(clone);
                lastRelatedFeatureGeometries.Add((IGeometry)clone.Geometry.Clone());
                if (null != addRelatedFeature)
                {
                    activeOutRules.Add(new List<IFeatureRelationInteractor>());
                    addRelatedFeature(activeOutRules[activeOutRules.Count - 1], branch, clone, level);
                }
            }
        }

        public override void UpdateRelatedFeatures(IFeature feature, IGeometry newGeometry, IList<int> trackerIndices)
        {
            updeetGeometries(false, lastRelatedNewFeatures, feature, newGeometry, trackerIndices);
        }

        public override void StoreRelatedFeatures(IFeature feature, IGeometry newGeometry, IList<int> trackerIndices)
        {
            updeetGeometries(true, lastRelatedFeatures, feature, newGeometry, trackerIndices);
            lastFeature = null;
            return;
        }

        private void updeetGeometries(bool final, IList<IFeature> relatedNewFeatures, IFeature feature, IGeometry newGeometry, IList<int> trackerIndices)
        {
            int index = 0;
            INode node;
            if (null != (node = (feature as INode)))
            {
                if (feature != lastFeature)
                {
                    throw new ArgumentException("You must call FillRelatedFeature first!");
                }
                double deltaX = newGeometry.Coordinates[0].X - lastCoordinate.X;
                double deltaY = newGeometry.Coordinates[0].Y - lastCoordinate.Y;
                for (int b = 0; b < node.IncomingBranches.Count; b++)
                {
                    IBranch branch = node.IncomingBranches[b];
                    IGeometry geometry = lastRelatedFeatureGeometries[index];
                    FallOffPolicy.Reset();
                    // use the move method of FallOfPolicy that uses a source and target geometry
                    if (final)
                    {
                        FallOffPolicy.Move(relatedNewFeatures[index], geometry, geometry.Coordinates.Length - 1, deltaX, deltaY);
                    }
                    else
                    {
                        FallOffPolicy.Move(relatedNewFeatures[index].Geometry, geometry, geometry.Coordinates.Length - 1, deltaX, deltaY);
                    }
                    List<int> branchTrackerIndices = new List<int> { branch.Geometry.Coordinates.Length - 1 };
                    for (int i = 0; i < activeInRules[b].Count; i++)
                    {
                        if (final)
                            activeInRules[b][i].StoreRelatedFeatures(branch, relatedNewFeatures[index].Geometry, branchTrackerIndices);
                        else
                            activeInRules[b][i].UpdateRelatedFeatures(branch, relatedNewFeatures[index].Geometry, branchTrackerIndices);
                    }
                    index++;
                }
                for (int b = 0; b < node.OutgoingBranches.Count; b++)
                {
                    IBranch branch = node.OutgoingBranches[b];
                    IGeometry geometry = lastRelatedFeatureGeometries[index];
                    FallOffPolicy.Reset();
                    if (final)
                    {
                        FallOffPolicy.Move(relatedNewFeatures[index], geometry, 0, deltaX, deltaY);
                    }
                    else
                    {
                        FallOffPolicy.Move(relatedNewFeatures[index].Geometry, geometry, 0, deltaX, deltaY);
                    }
                    List<int> branchTrackerIndices = new List<int> {0};
                    for (int i = 0; i < activeOutRules[b].Count; i++)
                    {
                        if (final)
                            activeOutRules[b][i].StoreRelatedFeatures(branch, relatedNewFeatures[index].Geometry, branchTrackerIndices);
                        else
                            activeOutRules[b][i].UpdateRelatedFeatures(branch, relatedNewFeatures[index].Geometry, branchTrackerIndices);
                    }
                    index++;
                }
            }
        }

        public IFallOffPolicy FallOffPolicy { get; set; }
    }
}