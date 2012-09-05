using GeoAPI.Geometries;
using SharpMap.Layers;
using GeoAPI.Extensions.Feature;
using SharpMap.Styles;
using SharpMap.UI.Tools;

namespace SharpMap.UI.Editors
{
    class GridProfileEditor : LineStringEditor
    {
        public GridProfileEditor(ICoordinateConverter coordinateConverter, ILayer layer, IFeature feature, VectorStyle vectorStyle)
            : base(coordinateConverter, layer, feature, vectorStyle)
        {
        }
        public override void Start()
        {
            GridProfile branch = (GridProfile)SourceFeature;
            TargetFeature = new GridProfile
                                {
                                    Geometry = ((IGeometry)branch.Geometry.Clone()),
                                };
        }
        public override bool AllowMove()
        {
            return true;
        }
        public override bool AllowDeletion()
        {
            return true;
        }
    }
}