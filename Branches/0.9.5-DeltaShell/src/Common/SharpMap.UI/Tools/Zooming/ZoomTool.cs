using GeoAPI.Geometries;
using SharpMap.UI.Forms;

namespace SharpMap.UI.Tools.Zooming
{
    /// <summary>
    /// Zoom In / Out using mouse wheel, or rectangle.
    /// </summary>
    public abstract class ZoomTool : MapTool
    {
        protected ZoomTool(MapControl mapControl) : base(mapControl)
        {
        }
    }
}