using GeoAPI.Geometries;
using NetTopologySuite.Extensions.Coverages;
using SharpMap.UI.Forms;
using SharpMap.Layers;
using GeoAPI.Extensions.Feature;
using SharpMap.Styles;
using SharpMap.UI.Tools;

namespace SharpMap.UI.Editors
{
    public class FeatureEditorFactory
    {
        public static IFeatureEditor Create(ICoordinateConverter coordinateConverter, ILayer layer, IFeature feature, VectorStyle vectorStyle)
        {
            if (null == feature)
                return null;
            // most specific type should be first
            if (feature is GridProfile)
                return new GridProfileEditor(coordinateConverter, layer, feature, vectorStyle);
            if (feature is RegularGridCoverageCell)
                return new RegularGridCoverageCellEditor(coordinateConverter, layer, feature, vectorStyle);
            if (feature.Geometry is ILineString)
                return new LineStringEditor(coordinateConverter, layer, feature, vectorStyle);
            if (feature.Geometry is IPoint)
                return new PointEditor(coordinateConverter, layer, feature, vectorStyle);
            // todo implement custom mutator for Polygon and MultiPolygon
            // LineStringMutator will work as long as moving is not supported.
            if (feature.Geometry is IPolygon)
                return new LineStringEditor(coordinateConverter, layer, feature, vectorStyle);
            if (feature.Geometry is IMultiPolygon)
                return new LineStringEditor(coordinateConverter, layer, feature, vectorStyle);
            return null;
            //throw new ArgumentException("Unsupported type " + feature.Geometry);
        }
    }
}