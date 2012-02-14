using DelftTools.Utils;
using GeoAPI.Extensions.Feature;
using GeoAPI.Geometries;
using SharpMap.Layers;
using SharpMap.Styles;
using SharpMap.UI.Tools;

namespace SharpMap.UI.Editors
{
    internal class GridProfileEditor : LineStringEditor
    {
        public GridProfileEditor(ICoordinateConverter coordinateConverter, ILayer layer, IFeature feature,
                                 VectorStyle vectorStyle, IEditableObject editableObject)
            : base(coordinateConverter, layer, feature, vectorStyle, editableObject)
        {
        }

        public override void Start()
        {
            var gridProfile = (CoverageProfile) SourceFeature;
            TargetFeature = new CoverageProfile { Geometry = ((IGeometry) gridProfile.Geometry.Clone()) };
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