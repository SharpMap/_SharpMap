using System.Drawing;
using GeoAPI.Geometries;

namespace SharpMap.UI.Editors
{
    public interface ICoordinateConverter
    {
        /// <summary>
        /// Converts a range in device (normally pixels) coordinates to world coordinates.
        /// </summary>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <returns></returns>
        ICoordinate ImageToWorld(double width, double height);

        /// <summary>
        /// Converts a point in device (normally pixels) coordinates to world coordinates.
        /// </summary>
        /// <param name="p"></param>
        /// <returns></returns>
        ICoordinate ImageToWorld(PointF p);

        /// <summary>
        /// Converts a distance in device (normally pixels) coordinates to world coordinates.
        /// </summary>
        /// <param name="imageSize"></param>
        /// <returns></returns>
        double ImageToWorld(float imageSize);
    }
}