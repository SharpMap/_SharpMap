using System;
using System.Drawing;
using System.Windows.Forms;
using GeoAPI.Geometries;
using SharpMap.UI.Forms;
using GeoPoint=GisSharpBlog.NetTopologySuite.Geometries.Point;

namespace SharpMap.UI.Tools
{
    /// <summary>
    /// When this tool is active it displays a scalebar on the mapcontrol.
    /// </summary>
    public class ScaleBarTool : LayoutComponentTool
    {
        private ScaleBar bar;
        private bool initScreenPosition = false;

        /// <summary>
        /// Creates the scale bar layout component.
        /// </summary>
        /// <param name="mapControl">The map control it operates on</param>
        public ScaleBarTool(MapControl mapControl): base(mapControl)
        {
            Name = "ScaleBar";
            bar = new ScaleBar
                      {
                          BarUnit = MapUnits.ws_muMeter,
                          MapUnit = MapUnits.ws_muMeter,
                          AlignMent = StringAlignment.Near
                      };

            //bar.BorderVisible = true;
            //this.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;           
        }


        public override void OnMouseDown(ICoordinate worldPosition, System.Windows.Forms.MouseEventArgs e)
        {
            //do not allow user to select this tool.
         //   base.OnMouseDown(worldPosition, e);
        }

        /// <summary>
        /// Draws a scalebar on the screen.
        /// </summary>
        public override void Render(Graphics graphics, Map mapBox)
        {
            if (Visible)
            {
                if (!initScreenPosition)
                {
                    initScreenPosition = SetInitialScreenLocation();
                }

                // Get the current map scale
                double meters = GetSegmentInMeters();

                //display km scale on bar if map has small scale 
                bar.BarUnit = meters<5000 ? MapUnits.ws_muMeter : MapUnits.ws_muKilometer;

                if (meters > 0) // A valid scale was found
                {
                    bar.SetScale(meters, size.Width);
                    bar.DrawTheControl(graphics, ScreenRectangle);
               }

            }
            base.Render(graphics, mapBox);
        }

        /// <summary>
        /// Calculate the width of one scale bar segment, based on the current map zoom factor.
        /// </summary>
        /// <returns>The segment width in meters</returns>
        // TODO: Only recalculate the segment when the zoom level of the map changes
        private double GetSegmentInMeters()
        {
            // Get the beginnign and ending world coordinates of a virtual line width the lenght of 
            // this scale bar size.Width in pixels.
            ICoordinate measure0 = Map.ImageToWorld(new PointF(screenLocation.X, screenLocation.Y));
            ICoordinate measure1 = Map.ImageToWorld(new PointF(screenLocation.X + size.Width, screenLocation.Y + 1));

            // Convert the world coordinates into actual meters using a geodetic calculation helper 
            // class. The height is not taken into account (since this information is missing).
            // TODO: Use the elipsoid appropriate to the current map layers loaded
            /*GeodeticCalculator calc = new GeodeticCalculator();
            GlobalCoordinates gp0 = new GlobalCoordinates(measure0.X, measure0.Y);
            GlobalCoordinates gp1 = new GlobalCoordinates(measure1.X, measure1.Y);
            GeodeticCurve gc = calc.CalculateGeodeticCurve(Ellipsoid.WGS84, gp0, gp1);
            return gc.EllipsoidalDistance;*/
            // HACK: For now use a plain coordinates to meters calculation like Sobek normally uses (RD or Amersfoort)
            return Math.Sqrt(
                    Math.Pow(measure1.X - measure0.X, 2) +
                    Math.Pow(measure1.Y - measure0.Y, 2));

            // If no usefull coordinate system was found, return 0
            //return 0;
        }

        private Point GetInitScreenLocation()
        {
            int margin = 5;

            var point = new Point(margin, margin);

            if ((Anchor & AnchorStyles.Bottom) == AnchorStyles.Bottom)
            {
                point.Y = Map.Size.Height - size.Height - margin;
            }

            if ((Anchor & AnchorStyles.Right) == AnchorStyles.Right)
            {
                point.X = Map.Size.Width - size.Width - margin;
            }

            return point;
        }
    }
}