using System.ComponentModel;
using System.Drawing;
using GeoAPI.Geometries;
using GisSharpBlog.NetTopologySuite.Geometries;
using SharpMap.UI.Forms;
using System.Windows.Forms;
using Point = System.Drawing.Point;

namespace SharpMap.UI.Tools.Zooming
{
    /// <summary>
    /// Zooms in / out on mouse wheel.
    /// Pans on mouse wheel
    /// </summary>
    public class PanZoomUsingMouseWheelTool : ZoomTool
    {
        private Bitmap DragImage { get; set; }
        private Bitmap StaticToolsImage { get; set; }
        private bool Dragging { get; set; }
        private Point DragStartPoint { get; set; }
        private Point DragEndPoint { get; set; }

        public PanZoomUsingMouseWheelTool(MapControl mapControl)
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
            double usedWheelZoomMagnutide = wheelZoomMagnitude;
            // fine zoom if alt is pressed
            if (IsAltPressed)
            {
                usedWheelZoomMagnutide = wheelZoomMagnitude/8;
            }

            double scaleBase = 1 + usedWheelZoomMagnutide;
            
            double zoomFactor = scale > 0 ? scaleBase : 1/scaleBase;

            Map.Zoom *= zoomFactor;

            Rectangle zoomRectangle;
            if (!IsShiftPressed)
            {
                //determine center coordinate in world units
                double newCenterX = mouseWorldPosition.X - Map.PixelWidth*(e.X - MapControl.Width/2.0);
                double newCenterY = mouseWorldPosition.Y - Map.PixelHeight*(MapControl.Height/2.0 - e.Y);

                // use current map center if shift is pressed
                Map.Center = new Coordinate(newCenterX, newCenterY);

                // draw zoom rectangle (in screen coordinates)
                zoomRectangle = new Rectangle(
                    (int)(e.X * (1 - zoomFactor)),
                    (int)(e.Y * (1 - zoomFactor)),
                    (int)(MapControl.Size.Width * zoomFactor),
                    (int)(MapControl.Size.Height * zoomFactor));
            }
            else
            {
                // draw zoom rectangle (in screen coordinates)
                zoomRectangle = new Rectangle(
                    (int)(MapControl.Width/2.0 * (1 - zoomFactor)),
                    (int)(MapControl.Height/2.0 * (1 - zoomFactor)),
                    (int)(MapControl.Size.Width * zoomFactor),
                    (int)(MapControl.Size.Height * zoomFactor));
            }
            

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

        /// <summary>
        /// pan using mousewheel down
        /// </summary>
        /// <param name="worldPosition"></param>
        /// <param name="e"></param>
        public override void OnMouseDown(ICoordinate worldPosition, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Middle)
            {
                return;
            }
            MapControl.Cursor = Cursors.Hand;

            Dragging = true;
            DragImage = (Bitmap)Map.Image.Clone();
            StaticToolsImage = new Bitmap(Map.Image.Width, Map.Image.Height);
            Graphics g = Graphics.FromImage(StaticToolsImage);

            foreach (IMapTool tool in MapControl.Tools)
            {
                if (tool.IsActive && tool.RendersInScreenCoordinates)
                {
                    tool.OnPaint(new PaintEventArgs(g, MapControl.ClientRectangle));
                }
            }
            g.Dispose();

            DragStartPoint = e.Location;
            DragEndPoint = e.Location;
        }

        
        public override void OnMouseUp(ICoordinate worldPosition, MouseEventArgs e)
        {
            if (!((e.Button == MouseButtons.Middle) && Dragging))
            {
                return;
            }

            DragImage.Dispose();
            StaticToolsImage.Dispose();
            Dragging = false;
            Point point = new Point((MapControl.ClientSize.Width / 2 + (DragStartPoint.X - DragEndPoint.X)),
                                    (MapControl.ClientSize.Height / 2 + (DragStartPoint.Y - DragEndPoint.Y)));
            Map.Center = Map.ImageToWorld(point);

            MapControl.Cursor = Cursors.Default;
            MapControl.Refresh();
        }

        public override void OnMouseMove(ICoordinate worldPosition, MouseEventArgs e)
        {
            if (!Dragging)
            {
                return;
            }
            MapControl.Cursor = Cursors.Hand;
            DragEndPoint = e.Location;

            var point = new Point((DragEndPoint.X - DragStartPoint.X),
                                    (DragEndPoint.Y - DragStartPoint.Y));

            var previewImage = new Bitmap(DragImage.Width, DragImage.Height);
            Graphics g = Graphics.FromImage(previewImage);
            g.Clear(MapControl.BackColor);
            g.DrawImageUnscaled(DragImage, point);
            g.DrawImageUnscaled(StaticToolsImage, 0, 0);
            g.Dispose();

            g = MapControl.CreateGraphics();
            g.DrawImage(previewImage, 0, 0);
            g.Dispose();

            previewImage.Dispose();
        }
    }
}