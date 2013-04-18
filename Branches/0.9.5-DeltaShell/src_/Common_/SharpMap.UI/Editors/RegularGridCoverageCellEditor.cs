using GeoAPI.Extensions.Feature;
using SharpMap.Layers;
using SharpMap.Styles;

namespace SharpMap.UI.Editors
{
    class RegularGridCoverageCellEditor : LineStringEditor
    {
        public RegularGridCoverageCellEditor(ICoordinateConverter coordinateConverter, ILayer layer, IFeature feature, VectorStyle vectorStyle) : base(coordinateConverter, layer, feature, vectorStyle)
        {
        }

        public override bool AllowDeletion()
        {
            return false;
        }

        public override bool AllowMove()
        {
            return false;
        }

        public override bool AllowSingleClickAndMove()
        {
            return false;
        }
    }
}
