using DelftTools.Utils.Editing;
using GeoAPI.Extensions.Feature;
using GeoAPI.Geometries;
using SharpMap.Api;
using SharpMap.Editors.Interactors;
using SharpMap.Layers;
using SharpMap.Styles;

namespace SharpMap.UI.Tools
{
    internal class CoverageProfileInteractor : LineStringInteractor
    {
        public CoverageProfileInteractor(ILayer layer, IFeature feature, VectorStyle vectorStyle, IEditableObject editableObject)
            : base(layer, feature, vectorStyle, editableObject)
        {
        }

        public override void Start()
        {
            var coverageProfile = (CoverageProfile) SourceFeature;
            TargetFeature = new CoverageProfile { Geometry = ((IGeometry) coverageProfile.Geometry.Clone()) };
        }

        protected override bool AllowMoveCore()
        {
            return true;
        }

        protected override bool AllowDeletionCore()
        {
            return true;
        }


    }
}