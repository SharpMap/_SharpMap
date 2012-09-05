using System.ComponentModel;
using System.Drawing;
using GeoAPI.Geometries;
using GisSharpBlog.NetTopologySuite.Geometries;
using SharpMap.UI.Forms;
using System.Windows.Forms;

namespace SharpMap.UI.Tools.Zooming
{
    /// <summary>
    /// Zooms in / out on mouse wheel.
    /// </summary>
    public class ZoomUsingMouseWheelTool : ZoomTool
    {
        public ZoomUsingMouseWheelTool(MapControl mapControl)
            : base(mapControl)
        {
            Name = "ZoomInOutUsingWheel";
        }


        private double wheelZoomMagnitude = 2;

        [Description("The amount which a single movement of the mouse wheel zooms by.")]
        [DefaultValue(2)]
        [Category("Behavior")]
        public double WheelZoomMagnitude
        {
            get { return wheelZoomMagnitude; }
            set { wheelZoomMagnitude = value; }
        }


        public override bool AlwaysActive
        {
            get { return true; }
        }


        public override void OnMouseWheel(ICoordinate mouseWorldPosition, MouseEventArgs e)
        {
            // zoom map 
            double scale = (-e.Delta/250.0);
            double scaleBase = 1 + wheelZoomMagnitude;

            // fine zoom if alt is pressed
            if (IsAltPressed)
            {
                scale = (-e.Delta/500.0);
            }

            double zoomFactor = scale > 0 ? scaleBase : 1/scaleBase;

            Map.Zoom *= zoomFactor;

            //determine center coordinate in world units
            double newCenterX = mouseWorldPosition.X - Map.PixelWidth*(e.X - MapControl.Width/2.0);
            double newCenterY = mouseWorldPosition.Y - Map.PixelHeight*(MapControl.Height/2.0 - e.Y);

            // use current map center if shift is pressed
            Map.Center = new Coordinate(newCenterX, newCenterY);

            // draw zoom rectangle (in screen coordinates)
            Rectangle zoomRectangle = new Rectangle(
                (int) (e.X*(1 - zoomFactor)),
                (int) (e.Y*(1 - zoomFactor)),
                (int) (MapControl.Size.Width*zoomFactor),
                (int) (MapControl.Size.Height*zoomFactor));

            // draw image and clear background in a separate image first to prevent flickering
            Bitmap previewImage = (Bitmap) Map.Image.Clone();
            Graphics g = Graphics.FromImage(previewImage);
            g.Clear(MapControl.BackColor);
            g.DrawImage(Map.Image, MapControl.ClientRectangle, zoomRectangle, GraphicsUnit.Pixel);

            // make tools to draw themself while map is being rendered
            foreach (IMapTool tool in MapControl.Tools)
            {
                if (tool.IsActive)
                {
                    tool.OnPaint(new PaintEventArgs(g, MapControl.ClientRectangle));
                }
            }

            g.Dispose();

            // now draw preview image on control
            g = MapControl.CreateGraphics();
            g.DrawImage(previewImage, 0, 0);
            g.Dispose();

            previewImage.Dispose();

            // call full map rendering (long operation)
            MapControl.Refresh();
        }
    }
}