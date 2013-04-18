using System;
using System.Collections.ObjectModel;
using System.Drawing;
using System.Windows.Forms;
using GeoAPI.Geometries;
using GisSharpBlog.NetTopologySuite.Geometries;
using SharpMap.Data.Providers;
using SharpMap.Layers;
using SharpMap.UI.Forms;
using SharpMap.UI.Mapping;
using GeoPoint = GisSharpBlog.NetTopologySuite.Geometries.Point;

namespace SharpMap.UI.Tools
{
    public class MeasureTool : MapTool
    {
        private Collection<IGeometry> pointGometries;
        private VectorLayer pointLayer;
        private double distanceInMeters;

        public MeasureTool(MapControl mapControl)
            : base(mapControl)
        {
            pointGometries = new Collection<IGeometry>();
            pointLayer = new VectorLayer();
            pointLayer.Name = "measure";
            pointLayer.DataSource = new DataTableFeatureProvider(pointGometries);
            pointLayer.Style.Symbol = TrackerSymbolHelper.GenerateSimple(Pens.DarkMagenta, Brushes.Indigo, 6, 6);
            pointLayer.Enabled = false;
            pointLayer.ShowInLegend = false;
        }

        /// <summary>
        /// Use this property to enable or disable tool. When the measure tool is deactivated, it cleans up old measurements.
        /// </summary>
        public override bool IsActive
        {
            get
            {
                return base.IsActive;
            }
            set
            {
                base.IsActive = value;
                if (!IsActive)
                    Clear();
            }
        }

        public override void ActiveToolChanged(IMapTool newTool)
        {
            // TODO: It seems this is never called, so it is also cleared when the IsActive property is (re)set
            Clear();
            base.ActiveToolChanged(newTool);
        }

        /// <summary>
        /// Clean up set coordinates and distances for a fresh future measurement
        /// </summary>
        private void Clear()
        {
            pointGometries.Clear();
            distanceInMeters = double.MinValue;
        }

        public override void OnMouseDown(GeoAPI.Geometries.ICoordinate worldPosition, System.Windows.Forms.MouseEventArgs e)
        {
            // Starting a new measurement?
            if (pointGometries.Count >= 2)
            {
                Clear();
            }

            // Add the newly selected point
            pointGometries.Add(new GeoPoint(worldPosition));

            CalculateDistance();

            // Refresh the screen
            pointLayer.RenderRequired = true;
            MapControl.Refresh(); // HACK: Why is this needed? (Only RenderRequired = true isn't enough...)
            
            base.OnMouseDown(worldPosition, e);
        }

        /// <summary>
        /// Calculate distance in meters between the two selected points
        /// </summary>
        private void CalculateDistance()
        {
            if (pointGometries.Count >= 2)
            {
                // Convert the world coordinates into actual meters using a geodetic calculation helper 
                // class. The height is not taken into account (since this information is missing).
                // TODO: Use the elipsoid appropriate to the current map layers loaded
                /*GeodeticCalculator calc = new GeodeticCalculator();
                GlobalCoordinates gp0 = new GlobalCoordinates(pointGometries[0].Coordinate.X, pointGometries[0].Coordinate.Y);
                GlobalCoordinates gp1 = new GlobalCoordinates(pointGometries[1].Coordinate.X, pointGometries[1].Coordinate.Y);
                GeodeticCurve gc = calc.CalculateGeodeticCurve(Ellipsoid.WGS84, gp0, gp1);
                distanceInMeters = gc.EllipsoidalDistance;*/
                // HACK: For now use a plain coordinates to meters calculation like Sobek normally uses (RD or Amersfoort)
                distanceInMeters = Math.Sqrt(
                    Math.Pow(pointGometries[1].Coordinate.X - pointGometries[0].Coordinate.X, 2) +
                    Math.Pow(pointGometries[1].Coordinate.Y - pointGometries[0].Coordinate.Y, 2));

                // Show a line indicator
                pointGometries.Add(new LineString(new ICoordinate[] { pointGometries[0].Coordinate, pointGometries[1].Coordinate }));
            }
        
        }

        /// <summary>
        /// Painting of the measure tool (the selected points, a connecting line and the distance in text)
        /// </summary>
        /// <param name="e"></param>
        public override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            Render(e.Graphics, MapControl.Map);
        }

        /// <summary>
        /// Visual rendering of the measurement (two line-connected points and the text)
        /// </summary>
        /// <param name="graphics"></param>
        /// <param name="map"></param>
        public override void Render(Graphics graphics, Map map)
        {
            pointLayer.Map = map;
            pointLayer.Enabled = true;
            pointLayer.RenderRequired = true;
            pointLayer.Render();
            graphics.DrawImageUnscaled(pointLayer.Image, 0, 0);

            // Show the distance in text
            if (pointGometries.Count >= 2)
            {
                Font distanceFont = new Font("Arial", 10);
                Map.WorldToImage(pointGometries[1].Coordinate);
                PointF textPoint = Map.WorldToImage(pointGometries[1].Coordinate);
                if (distanceInMeters > double.MinValue)
                    graphics.DrawString(distanceInMeters.ToString("N") + "m", distanceFont, Brushes.Black, textPoint);
            }
        }
        
    }
}
