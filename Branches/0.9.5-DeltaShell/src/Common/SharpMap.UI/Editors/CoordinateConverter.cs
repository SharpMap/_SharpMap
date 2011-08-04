using System;
using System.Drawing;
using GeoAPI.Geometries;
using SharpMap.Converters.Geometries;
using SharpMap.UI.Forms;

namespace SharpMap.UI.Editors
{
    public class CoordinateConverter : ICoordinateConverter
    {
        /// <summary>
        /// The mapcontrol that contains the map that will actually perform the conversion.
        /// Device to map conversion is only useful in the context of a device (screen = control or bitmap)
        /// The Map in a mapcontrol can be changed and to prevent cpmpplex and buggy update mechanism just refer
        /// to the mapcontrol.
        /// </summary>
        IMapControl MapControl { get; set; }
        //Map Map { get; set; }

        public CoordinateConverter(IMapControl mapControl)
        {
            MapControl = mapControl;
        }

        public ICoordinate ImageToWorld(double width, double height)
        {
            ICoordinate c1 = MapControl.Map.ImageToWorld(new PointF(0, 0));
            ICoordinate c2 = MapControl.Map.ImageToWorld(new PointF((float)width, (float)height));
            return GeometryFactory.CreateCoordinate(Math.Abs(c1.X - c2.X), Math.Abs(c1.Y - c2.Y));
        }

        public ICoordinate ImageToWorld(PointF p)
        {
            return MapControl.Map.ImageToWorld(p);
        }

        public double ImageToWorld(float imageSize)
        {
            ICoordinate c1 = MapControl.Map.ImageToWorld(new PointF(0, 0));
            ICoordinate c2 = MapControl.Map.ImageToWorld(new PointF(imageSize, imageSize));
            return Math.Abs(c1.X - c2.X);
        }

    }
}