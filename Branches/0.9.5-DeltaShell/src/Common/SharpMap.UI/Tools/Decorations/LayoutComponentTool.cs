using System.Drawing;
using System.Windows.Forms;
using GeoAPI.Geometries;

namespace SharpMap.UI.Tools
{
    /// <summary>
    /// A base class for layout-related components on a map such as a legend, scale bar or north arrow.
    /// It implements drag and drop moving of the component.
    /// </summary>
    public abstract class LayoutComponentTool : MapTool, IMapTool
    {
        protected AnchorStyles anchor;

        private bool componentDragging; // True if the user is dragging the layout component to move it
        private Point dragOffset; // The offset from the top-left of the bitmap to the actual mouse click point
        private Size oldMapSize; // Store the 'old' size of the map to compare to changes in the map size
        protected Point screenLocation;
        protected Size size;
        protected bool visible;

        public LayoutComponentTool()
        {
            screenLocation = new Point(0, 0);
            visible = true;
        }

        public override bool RendersInScreenCoordinates
        {
            get { return true; }
        }

        /// <summary>
        /// Gets or sets the anchoring of the component, sticking to 1 or more map control edges
        /// </summary>
        public AnchorStyles Anchor
        {
            get { return anchor; }
            set { anchor = value; }
        }

        /// <summary>
        /// The location of the component on the screen, relative to the top-left of the map control.
        /// </summary>
        public Point ScreenLocation
        {
            get { return screenLocation; }
            set { screenLocation = value; }
        }

        /// <summary>
        /// The size of the component on the screen, in pixels
        /// </summary>
        public virtual Size Size
        {
            get { return size; }
            set { size = value; }
        }

        /// <summary>
        /// If the layout component should be drawn to the screen
        /// </summary>
        public virtual bool Visible
        {
            get { return visible; }
            set { visible = value;}
        }

        /// <summary>
        /// Returns the rectangle of this component on the screen
        /// </summary>
        public Rectangle ScreenRectangle
        {
            get { return new Rectangle(screenLocation, Size); }
        }

        #region IMapTool Members

        /// <summary>
        /// A layout component is always active (returning true), showing the control on the screen and 
        /// allowing interactions with it.
        /// </summary>
        public override bool AlwaysActive
        {
            get { return true; }
        }

        /// <summary>
        /// When the left mouse button is clicked on the layout component and the move tool was 
        /// selected, start dragging. 
        /// Ignore when another feature is already selected.
        /// </summary>
        /// <param name="worldPosition">The world location clicked</param>
        /// <param name="e">The mouse state</param>
        public override void OnMouseDown(ICoordinate worldPosition, MouseEventArgs e)
        {
            if (Visible && MapControl.MoveTool.IsActive)
            {
                PointF mouseDownLocation = Map.WorldToImage(worldPosition);

                if (e.Button == MouseButtons.Left && screenLocation.X < mouseDownLocation.X &&
                        screenLocation.X + Size.Width >= mouseDownLocation.X && screenLocation.Y < mouseDownLocation.Y &&
                        screenLocation.Y + Size.Height >= mouseDownLocation.Y)
                {
                    // Start dragging
                    componentDragging = true;
                    dragOffset = new Point((int)mouseDownLocation.X - screenLocation.X,
                                           (int)mouseDownLocation.Y - screenLocation.Y);

                    MapControl.MoveTool.IsActive = false;
                }
            }

            base.OnMouseDown(worldPosition, e);
        }

        /// <summary>
        /// While dragging (left mouse button), adjust the component screen location.
        /// </summary>
        /// <param name="worldPosition">The new location</param>
        /// <param name="e">The mouse state</param>
        public override void OnMouseMove(ICoordinate worldPosition, MouseEventArgs e)
        {
            if (Visible && componentDragging)
            {
                // Adjust the location of this layout component
                PointF newLocation = Map.WorldToImage(worldPosition);
                screenLocation.X = (int) newLocation.X - dragOffset.X;
                screenLocation.Y = (int) newLocation.Y - dragOffset.Y;

                CorrectScreenLocation();

                StartDrawing();
                DoDrawing(true);
                StopDrawing();
            }

            base.OnMouseMove(worldPosition, e);
        }

        /// <summary>
        /// When the left mouse button was released, stop the dragging of the layout component.
        /// </summary>
        /// <param name="worldPosition"></param>
        /// <param name="e"></param>
        public override void OnMouseUp(ICoordinate worldPosition, MouseEventArgs e)
        {
            if (componentDragging)
            {
                base.MapControl.MoveTool.IsActive = true;
                componentDragging = false;
            }

            base.OnMouseUp(worldPosition, e);
        }

        /// <summary>
        /// Painting the component includes correcting the component's screen location to any map size changes.
        /// </summary>
        /// <param name="e"></param>
        public override void OnPaint(PaintEventArgs e)
        {
            // TODO: Instead of doing this all the time it should rather do this when the map's size has actually changed
            ReflectMapSizeChanges();

            if (Visible)
            {
                // Rendering of the visual appearance of this component
                Render(e.Graphics, MapControl.Map);
            }
        }

        #endregion

        /// <summary>
        /// Correct the new screen location to the actual visible control area.
        /// </summary>
        private void CorrectScreenLocation()
        {
            // Check boundaries
            if (screenLocation.X < 0)
                screenLocation.X = 0;
            if (screenLocation.Y < 0)
                screenLocation.Y = 0;
            if (screenLocation.X + Size.Width > Map.Size.Width)
                screenLocation.X = Map.Size.Width - Size.Width;
            if (screenLocation.Y + Size.Height > Map.Size.Height)
                screenLocation.Y = Map.Size.Height - Size.Height;
        }

        /// <summary>
        /// When the map's Size property is changed, adjust our screen location according to the anchoring.
        /// </summary>
        private void ReflectMapSizeChanges()
        {
            if (oldMapSize != Map.Size)
            {
                // First time, get the current map size
                if (oldMapSize.Height == 0 && oldMapSize.Width == 0)
                    oldMapSize = Map.Size;

                // Calculate new screen X location
                if ((anchor & AnchorStyles.Left) != AnchorStyles.Left &&
                    (anchor & AnchorStyles.Right) != AnchorStyles.Right)
                    screenLocation.X += (Map.Size.Width - oldMapSize.Width)/2;
                else if ((anchor & AnchorStyles.Right) == AnchorStyles.Right)
                    screenLocation.X += (Map.Size.Width - oldMapSize.Width);
                // Calculate new screen Y location
                if ((anchor & AnchorStyles.Top) != AnchorStyles.Top &&
                    (anchor & AnchorStyles.Bottom) != AnchorStyles.Bottom)
                    screenLocation.Y += (Map.Size.Height - oldMapSize.Height)/2;
                else if ((anchor & AnchorStyles.Bottom) == AnchorStyles.Bottom)
                    screenLocation.Y += (Map.Size.Height - oldMapSize.Height);

                CorrectScreenLocation();

                // Store the new size for future comparision
                oldMapSize = Map.Size;
            }
        }

        /// <summary>
        /// Set the initial screenlocation based on anchorstyles
        /// </summary>
        /// <returns></returns>
        protected bool SetInitialScreenLocation()
        {
            int margin = 5;

            var point = new Point(margin,margin);

            if((Anchor & AnchorStyles.Bottom)==AnchorStyles.Bottom)
            {
                point.Y = Map.Size.Height - size.Height - margin;
            }

            if((Anchor & AnchorStyles.Right)==AnchorStyles.Right)
            {
                point.X = Map.Size.Width - size.Width - margin;
            }

            screenLocation = point;

            return true;
        }
    }
}