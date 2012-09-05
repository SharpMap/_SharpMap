using DelftTools.Utils;
using GeoAPI.Extensions.Feature;
using SharpMap.Layers;
using SharpMap.Styles;

namespace SharpMap.UI.Editors
{
    class RegularGridCoverageCellEditor : LineStringEditor
    {
        public RegularGridCoverageCellEditor(ICoordinateConverter coordinateConverter, ILayer layer, IFeature feature, VectorStyle vectorStyle, IEditableObject editableObject) : base(coordinateConverter, layer, feature, vectorStyle, editableObject)
        {
        }

        protected override bool AllowDeletionCore()
        {
            return false;
        }

        protected override bool AllowMoveCore()
        {
            return false;
        }

        public override bool AllowSingleClickAndMove()
        {
            return false;
        }
    }
}
