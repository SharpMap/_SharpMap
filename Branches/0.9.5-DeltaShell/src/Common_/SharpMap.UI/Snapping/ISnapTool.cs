using GeoAPI.Extensions.Feature;
using GeoAPI.Geometries;

namespace SharpMap.UI.Snapping
{
    /// <summary>
    /// ISnapper basic function for snapping to objects. Polygon or Line basesd object should make there
    /// own implementation
    /// </summary>
    public interface ISnapTool
    {
        //void AddSnap(IGeometry geometry, ICoordinate from, ICoordinate by, ICoordinate to);
        void AddSnap(IGeometry geometry, ICoordinate from, ICoordinate to);

        ISnapResult SnapResult { get; set; }

        //IFeature SnappedFeature { get; set; }
        //ICoordinate Location { get; }
        //int PreviousIndex { get; }
        //int NextIndex { get; }
        //bool Failed { get; }
        void Reset();
    }
}