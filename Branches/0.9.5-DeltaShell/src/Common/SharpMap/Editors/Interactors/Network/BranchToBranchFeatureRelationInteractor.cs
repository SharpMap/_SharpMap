using System;
using System.Collections.Generic;
using System.Linq;
using DelftTools.Utils.Collections;
using GeoAPI.Extensions.Feature;
using GeoAPI.Extensions.Networks;
using GeoAPI.Geometries;
using GisSharpBlog.NetTopologySuite.Geometries;
using NetTopologySuite.Extensions.Geometries;
using NetTopologySuite.Extensions.Networks;
using SharpMap.Api;
using SharpMap.Api.Delegates;
using SharpMap.Api.Editors;
using SharpMap.Editors.FallOff;

namespace SharpMap.Editors.Interactors.Network
{
    public class BranchToBranchFeatureRelationInteractor<T> : FeatureRelationInteractor where T : IBranchFeature, new()
    {
        private IBranch clonedBranch;
        private IBranch originalBranch;
        private ILineString originalBranchGeometry;

        private readonly IDictionary<T, double> fractionLookUp = new Dictionary<T, double>();
        private readonly IDictionary<T, T> cloneLookUp = new Dictionary<T, T>();
        private readonly List<IFeatureRelationInteractor> activeRules = new List<IFeatureRelationInteractor>();
        
        private INetwork Network { set; get; }

        private IFallOffPolicy FallOffPolicy { get; set; }

        public override IFeatureRelationInteractor Activate(IFeature feature, IFeature cloneFeature, AddRelatedFeature addRelatedFeature, int level, IFallOffPolicy fallOffPolicy)
        {
            FallOffPolicy = fallOffPolicy ?? new NoFallOffPolicy();

            var branch = feature as IBranch;

            if (branch == null || branch.BranchFeatures.Count <= 0)
            {
                return null;
            }

            var cloneRule = (BranchToBranchFeatureRelationInteractor<T>)CloneRule();
            cloneRule.Start(branch, cloneFeature as IBranch, addRelatedFeature, level);
            
            return cloneRule;
        }

        public override void UpdateRelatedFeatures(IFeature feature, IGeometry newGeometry, IList<int> trackerIndices)
        {
            if (feature != originalBranch)
            {
                throw new ArgumentException("You must call Start first!");
            }

            var newLineString = newGeometry as ILineString;
            var newfractionLookUp = GetNewFractions(trackerIndices, newLineString);

            foreach (var originalFeature in cloneLookUp.Keys)
            {
                var clonedRelatedFeature = cloneLookUp[originalFeature];
                UpdateBranchFeatureGeometry(clonedRelatedFeature.Geometry, newLineString,
                                            newfractionLookUp[originalFeature], clonedBranch, clonedRelatedFeature);

                if (clonedRelatedFeature.Geometry.GetType() == typeof (LineString))
                {
                    NetworkHelper.UpdateLineGeometry(clonedRelatedFeature, newLineString);
                }
            }

            foreach (var activeRule in activeRules)
            { 
                // activeRules are typically only applicable to one source feature. Since activeRules are only 
                // instantiated for if applicable they do not have to match with all originalRelatedFeatures
                // eg. StructureFeatureToStructureTopologyRule
                // CS0 ST0 CS1 CS2 ST1 = 5 originalRelatedFeatures/clonedRelatedFeatures
                // and only an activeRules for ST0 and ST1

                foreach (var orginalFeature in cloneLookUp.Keys)
                {
                    activeRule.UpdateRelatedFeatures(orginalFeature, cloneLookUp[orginalFeature].Geometry, null);
                }
            }
        }

        private Dictionary<T, double> GetNewFractions(IList<int> trackerIndices, ILineString newLineString)
        {
            var newFractions = BranchToBranchFeatureService.UpdateNewFractions(originalBranchGeometry, newLineString, fractionLookUp.Values.ToList(), trackerIndices, FallOffPolicy);
            return fractionLookUp.Keys.Zip(newFractions).ToDictionary(f => f.First, f => f.Second);
        }

        public override void StoreRelatedFeatures(IFeature feature, IGeometry newGeometry, IList<int> trackerIndices)
        {
            var branch = (IBranch)feature;
            var newLineString = newGeometry as ILineString;
            var newfractionLookUp = GetNewFractions(trackerIndices, newLineString);
            
            foreach (var originalFeature in cloneLookUp.Keys)
            {
                UpdateBranchFeatureGeometry(originalFeature.Geometry, newLineString, newfractionLookUp[originalFeature], branch, originalFeature);

                if (originalFeature.Geometry.GetType() == typeof(LineString))
                {
                    NetworkHelper.UpdateLineGeometry(originalFeature, newLineString);
                }
            }

            foreach (var activeRule in activeRules)
            {
                foreach (var orginalFeature in cloneLookUp.Keys)
                {
                    activeRule.StoreRelatedFeatures(orginalFeature, cloneLookUp[orginalFeature].Geometry, null);
                }
            }
        }

        private void UpdateBranchFeatureGeometry(IGeometry target, ILineString newLineString, double newFraction, IBranch branch, IBranchFeature branchFeature)
        {
            var newOffset = !branch.IsLengthCustom
                                ? BranchFeature.SnapChainage(newLineString.Length, newLineString.Length * newFraction)
                                : BranchFeature.SnapChainage(newLineString.Length, branchFeature.Chainage*newLineString.Length/branch.Length);
            
            if (!branch.IsLengthCustom)
            {
                branchFeature.Chainage = newOffset;
            }

            branchFeature.Geometry = GeometryHelper.SetCoordinate(target, 0, GeometryHelper.LineStringCoordinate(newLineString, newOffset));
        }

        private IFeatureRelationInteractor CloneRule()
        {
            return new BranchToBranchFeatureRelationInteractor<T>
                       {
                           FallOffPolicy = FallOffPolicy,
                           Network = Network
                       };
        }

        private void Start(IBranch branch, IBranch cloneBranch, AddRelatedFeature addRelatedFeature, int level)
        {
            originalBranch = branch;
            clonedBranch = cloneBranch;

            originalBranchGeometry = (ILineString)branch.Geometry.Clone();
            
            fractionLookUp.Clear();
            cloneLookUp.Clear();
            
            var branchFeatures = branch.BranchFeatures.OfType<T>().ToList();

            foreach (var branchFeature in branchFeatures)
            {
                var clonedBranchFeature = (T) branchFeature.Clone();

                cloneLookUp[branchFeature] = clonedBranchFeature;
                cloneBranch.BranchFeatures.Add(clonedBranchFeature);

                fractionLookUp[branchFeature] = branchFeature.Chainage / branch.Length;

                if (addRelatedFeature == null) continue;
                addRelatedFeature(activeRules, branchFeature, clonedBranchFeature, level);
            }
        }
    }
}