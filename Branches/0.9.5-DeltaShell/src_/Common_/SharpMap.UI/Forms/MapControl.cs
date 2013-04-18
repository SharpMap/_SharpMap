using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Windows.Forms;
using DelftTools.Utils;
using DelftTools.Utils.Aop;
using DelftTools.Utils.Aop.NotifyPropertyChanged;
using DelftTools.Utils.Collections;
using DelftTools.Utils.Collections.Generic;
using DelftTools.Utils.Threading;
using GeoAPI.Extensions.Feature;
using GeoAPI.Geometries;
using log4net;
using SharpMap.Converters.Geometries;
using SharpMap.Layers;
using SharpMap.Rendering.Thematics;
using SharpMap.Styles;
using SharpMap.Topology;
using SharpMap.UI.Snapping;
using SharpMap.UI.Tools;
using SharpMap.UI.Tools.Zooming;
using Timer = System.Windows.Forms.Timer;

namespace SharpMap.UI.Forms
{
    /// <summary>
    /// MapControl Class - MapControl control for Windows forms
    /// </summary>
    [DesignTimeVisible(true), NotifyPropertyChanged]
    [Serializable]
    public class MapControl : Control, IMapControl
    {
        #region Delegates

        /// <summary>
        /// MouseEventtype fired from the MapImage control
        /// </summary>
        /// <param name="WorldPos"></param>
        /// <param name="ImagePos"></param>
        public delegate void MouseEventHandler(ICoordinate WorldPos, MouseEventArgs ImagePos);

        #endregion

        private static readonly ILog log = LogManager.GetLogger(typeof (MapControl));

        private static readonly Color[] m_DefaultColors = new[]
                                                              {
                                                                  Color.DarkRed, Color.DarkGreen, Color.DarkBlue,
                                                                  Color.Orange, Color.Cyan, Color.Black, Color.Purple,
                                                                  Color.Yellow, Color.LightBlue, Color.Fuchsia
                                                              };

        private static int m_DefaultColorIndex;

        // other commonly-used specific tools
        private readonly CurvePointTool curvePointTool;
        private readonly DeleteTool deleteTool;
        private readonly FixedZoomInTool fixedZoomInTool;
        private readonly FixedZoomOutTool fixedZoomOutTool;
        private readonly LegendTool legendTool;
        private readonly MoveTool linearMoveTool;
        private readonly SolidBrush m_RectangleBrush = new SolidBrush(Color.FromArgb(210, 244, 244, 244));
        private readonly Pen m_RectanglePen = new Pen(Color.FromArgb(244, 244, 244), 1);
        private readonly MeasureTool measureTool;
        private readonly MoveTool moveTool;
        private readonly PanZoomTool panZoomTool;
        private readonly GridProfileTool profileTool;
        private readonly QueryTool queryTool;
        private readonly ZoomUsingRectangleTool rectangleZoomTool;
        private readonly SelectTool selectTool;

        private readonly List<ISnapRule> snapRules = new List<ISnapRule>();
        private readonly SnapTool snapTool;
        private readonly EventedList<IMapTool> tools;
        private readonly ZoomUsingMouseWheelTool wheelZoomTool;
        private readonly ZoomHistoryTool zoomHistoryTool;


        // TODO: fieds below should be moved to some more specific tools?
        private int m_QueryLayerIndex;
        private Map map;
        private DelayedEventHandler<NotifyCollectionChangedEventArgs> mapCollectionChangedEventHandler;
        private DelayedEventHandler<PropertyChangedEventArgs> mapPropertyChangedEventHandler;
        private IList<IFeature> selectedFeatures = new List<IFeature>();

        /// <summary>
        /// Initializes a new map
        /// </summary>
        public MapControl()
        {
            SetStyle(
                ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw |
                ControlStyles.UserPaint, true);

            LostFocus += MapBox_LostFocus;
            AllowDrop = true;

            tools = new EventedList<IMapTool>();

            tools.CollectionChanged += tools_CollectionChanged;

            var northArrowTool = new NorthArrowTool(this);
            northArrowTool.Anchor = AnchorStyles.Right | AnchorStyles.Top;
            northArrowTool.Visible = false; // activate using commands
            Tools.Add(northArrowTool);

            var scaleBarTool = new ScaleBarTool(this);

            scaleBarTool.Size = new Size(230, 50);
            scaleBarTool.Anchor = AnchorStyles.Right | AnchorStyles.Bottom;
            scaleBarTool.Visible = true;
            Tools.Add(scaleBarTool);

            legendTool = new LegendTool(this) {Anchor = AnchorStyles.Left | AnchorStyles.Top, Visible = false};
            Tools.Add(legendTool);

            queryTool = new QueryTool(this);
            Tools.Add(queryTool);

            // add commonly used tools

            zoomHistoryTool = new ZoomHistoryTool(this);
            Tools.Add(zoomHistoryTool);

            panZoomTool = new PanZoomTool(this);
            Tools.Add(panZoomTool);

            wheelZoomTool = new ZoomUsingMouseWheelTool(this);
            wheelZoomTool.WheelZoomMagnitude = 0.8;
            Tools.Add(wheelZoomTool);

            rectangleZoomTool = new ZoomUsingRectangleTool(this);
            Tools.Add(rectangleZoomTool);

            fixedZoomInTool = new FixedZoomInTool(this);
            Tools.Add(fixedZoomInTool);

            fixedZoomOutTool = new FixedZoomOutTool(this);
            Tools.Add(fixedZoomOutTool);

            selectTool = new SelectTool {IsActive = true};
            Tools.Add(selectTool);

            moveTool = new MoveTool();
            moveTool.Name = "Move selected vertices";
            moveTool.FallOffPolicy = FallOffPolicyRule.None;
            Tools.Add(moveTool);

            linearMoveTool = new MoveTool();
            linearMoveTool.Name = "Move selected vertices (linear)";
            linearMoveTool.FallOffPolicy = FallOffPolicyRule.Linear;
            Tools.Add(linearMoveTool);

            deleteTool = new DeleteTool();
            Tools.Add(deleteTool);

            measureTool = new MeasureTool(this);
            tools.Add(measureTool);

            profileTool = new GridProfileTool(this);
            profileTool.Name = "Make grid profile";
            tools.Add(profileTool);

            curvePointTool = new CurvePointTool();
            Tools.Add(curvePointTool);

            snapTool = new SnapTool();
            Tools.Add(snapTool);

            var toolTipTool = new ToolTipTool();
            Tools.Add(toolTipTool);

            MapTool fileHandlerTool = new FileDragHandlerTool();
            Tools.Add(fileHandlerTool);

            Tools.Add(new ExportMapToImageMapTool());

            Width = 100;
            Height = 100;

            mapPropertyChangedEventHandler =
                new SynchronizedDelayedEventHandler<PropertyChangedEventArgs>(map_PropertyChanged_Delayed)
                    {
                        FireLastEventOnly = true,
                        Delay2 = 300,
                        Filter = (sender, e) => sender is ILayer ||
                                                sender is VectorStyle ||
                                                sender is ITheme,
                        SynchronizeInvoke = this,
                        Enabled = false
                };
            mapCollectionChangedEventHandler =
                new SynchronizedDelayedEventHandler<NotifyCollectionChangedEventArgs>(map_CollectionChanged_Delayed)
                {
                    FireLastEventOnly = true,
                    Delay2 = 300,
                    Filter = (sender, e) => sender is Map ||
                                            sender is ILayer,
                    SynchronizeInvoke = this,
                    Enabled = false
                };

            Map = new Map(ClientSize) { Zoom = 100 };
        }

        [Description("The color of selecting rectangle.")]
        [Category("Appearance")]
        public Color SelectionBackColor
        {
            get { return m_RectangleBrush.Color; }
            set
            {
                if (value != m_RectangleBrush.Color)
                    m_RectangleBrush.Color = value;
            }
        }

        [Description("The color of selectiong rectangle frame.")]
        [Category("Appearance")]
        public Color SelectionForeColor
        {
            get { return m_RectanglePen.Color; }
            set
            {
                if (value != m_RectanglePen.Color)
                    m_RectanglePen.Color = value;
            }
        }

        /// <summary>
        /// Gets or sets the index of the active query layer 
        /// </summary>
        public int QueryLayerIndex
        {
            get { return m_QueryLayerIndex; }
            set { m_QueryLayerIndex = value; }
        }

        #region IMapControl Members

        [Description("The map image currently visualized.")]
        [Category("Appearance")]
        public Image Image
        {
            get
            {
                if (Map == null)
                {
                    return null;
                }
                var bitmap = new Bitmap(Width, Height);
                DrawToBitmap(bitmap, ClientRectangle);
                return bitmap;
            }
        }

        public override Color BackColor
        {
            get { return base.BackColor; }
            set
            {
                base.BackColor = value;
                if (Map != null)
                {
                    Map.BackColor = value;
                }
            }
        }

        /// <summary>
        /// Map reference
        /// </summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Map Map
        {
            get { return map; }
            set
            {
                if (map != null)
                {
                    //unsubscribe from changes in the map layercollection
                    UnSubscribeMapEvents();
                }

                map = value;

                if (map == null)
                {
                    return;
                }

                map.Size = ClientSize;

                SubScribeMapEvents();
                
                /*if (map.Layers.Count > 0)
                {
                    IEnvelope boundingBox = map.GetExtents();
                    if (boundingBox.IsNull)
                    {
                        // If map is empty zoom to more usefull bounds
                        Map.ZoomToBox(GeometryFactory.CreateEnvelope(0, 1000, 0, 1000));
                    }
                    else
                    {
                        map.ZoomToExtents();
                    }
                }
                else
                {
                    map.Zoom = 100;
                }*/

                SetMapStyles();

                Refresh();
            }
        }

        private void SetMapStyles()
        {
            DoubleBuffered = true;
            SetStyle(ControlStyles.DoubleBuffer, true);
            SetStyle(ControlStyles.OptimizedDoubleBuffer, true);
            SetStyle(ControlStyles.AllPaintingInWmPaint, true);
        }

        private void UnSubscribeMapEvents()
        {
            map.CollectionChanged -= mapCollectionChangedEventHandler;
            ((INotifyPropertyChanged)map).PropertyChanged -= mapPropertyChangedEventHandler;
            map.MapRendered -= map_MapRendered;
            map.MapLayerRendered -= m_Map_MapLayerRendered;
        }

        private void SubScribeMapEvents()
        {
            map.CollectionChanged += mapCollectionChangedEventHandler;
            ((INotifyPropertyChanged) map).PropertyChanged += mapPropertyChangedEventHandler;
            map.MapRendered += map_MapRendered;



            map.MapLayerRendered += m_Map_MapLayerRendered;
        }

        private void m_Map_MapLayerRendered(Graphics g, ILayer layer)
        {
            foreach (IMapTool tool in tools)
            {
                if (tool.IsActive)
                {
                    tool.OnMapLayerRendered(g, layer);
                }
            }
        }

        public IList<IMapTool> Tools
        {
            get { return tools; }
        }

        public IMapTool GetToolByName(string toolName)
        {
            foreach (IMapTool tool in Tools)
            {
                if (tool.Name == toolName)
                {
                    return tool;
                }
            }
            return null;
            // Do not throw ArgumentOutOfRangeException UI handlers (button checked) can ask for not existing tool
        }

        public IMapTool GetToolByType(Type type)
        {
            foreach (IMapTool tool in Tools)
            {
                if (tool.GetType() == type)
                {
                    return tool;
                }
            }

            throw new ArgumentOutOfRangeException(type.ToString());
        }

        public T GetToolByType<T>() where T : class
        {
            foreach (IMapTool tool in Tools)
            {
                if (tool.GetType() == typeof (T))
                {
                    return (T) tool;
                }
            }

            return null;
        }

        public void ActivateTool(IMapTool tool)
        {
            if (tool.IsActive)
            {
                // tool already active
                return;
            }

            if (tool.AlwaysActive)
            {
                throw new InvalidOperationException("Tool is AlwaysActive, use IMapTool.Execute() to make it work");
            }

            // deactivate other tools
            foreach (IMapTool t in tools)
            {
                if (t.IsActive && !t.AlwaysActive)
                {
                    t.IsActive = false;
                }
            }

            tool.IsActive = true;
        }

        public QueryTool QueryTool
        {
            get { return queryTool; }
        }

        public ZoomHistoryTool ZoomHistoryTool
        {
            get { return zoomHistoryTool; }
        }

        public PanZoomTool PanZoomTool
        {
            get { return panZoomTool; }
        }

        public ZoomUsingMouseWheelTool WheelZoomTool
        {
            get { return wheelZoomTool; }
        }

        public ZoomUsingRectangleTool RectangleZoomTool
        {
            get { return rectangleZoomTool; }
        }

        public FixedZoomInTool FixedZoomInTool
        {
            get { return fixedZoomInTool; }
        }

        public FixedZoomOutTool FixedZoomOutTool
        {
            get { return fixedZoomOutTool; }
        }

        public MoveTool MoveTool
        {
            get { return moveTool; }
        }

        public MoveTool LinearMoveTool
        {
            get { return linearMoveTool; }
        }

        public SelectTool SelectTool
        {
            get { return selectTool; }
        }

        public LegendTool LegendTool
        {
            get { return legendTool; }
        }

        public SnapTool SnapTool
        {
            get { return snapTool; }
        }


        public IList<IFeature> SelectedFeatures
        {
            get { return selectedFeatures; }
            set
            {
                selectedFeatures = value;
                Refresh();
            }
        }

        public IList<ISnapRule> SnapRules
        {
            get { return snapRules; }
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);

            mapPropertyChangedEventHandler.Enabled = true;
            mapCollectionChangedEventHandler.Enabled = true;
        }

        /// <summary>
        /// Refreshes the map
        /// </summary>
        [InvokeRequired]
        public override void Refresh()
        {
            Cursor c = Cursor;

            Cursor = Cursors.WaitCursor;

            if (map == null)
            {
                return;
            }

            map.Render();

            base.Refresh();

            // log.DebugFormat("Refreshed");

            if (MapRefreshed != null)
            {
                MapRefreshed(this, null);
            }

            Cursor = c;
        }

        public event EditorBeforeContextMenuEventHandler BeforeContextMenu;

        public IList<ISnapRule> GetSnapRules(ILayer layer, IFeature feature, IGeometry geometry, int trackingIndex)
        {
            var strategies = new List<ISnapRule>();
            foreach (ISnapRule snapRule in SnapRules)
            {
                if (snapRule.SourceLayer == layer)
                {
                    strategies.Add(snapRule);
                }
            }
            return strategies;
        }

        public bool IsProcessing
        {
            get
            {
                var processingPropertyChangedEvents = mapPropertyChangedEventHandler != null &&
                                                           (mapPropertyChangedEventHandler.IsRunning || mapPropertyChangedEventHandler.HasEventsToProcess);

                var processingCollectionChangedEvents = mapCollectionChangedEventHandler != null &&
                                               (mapCollectionChangedEventHandler.IsRunning || mapCollectionChangedEventHandler.HasEventsToProcess);

                return processingPropertyChangedEvents || processingCollectionChangedEvents;
            }
        }

        #endregion

        private void tools_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    ((IMapTool) e.Item).MapControl = this;
                    break;
                case NotifyCollectionChangedAction.Remove:
                    ((IMapTool) e.Item).MapControl = null;
                    break;
                default:
                    break;
            }
        }

        private void map_MapLayerRendered(Graphics g, ILayer layer)
        {
            foreach (IMapTool tool in tools)
            {
                if (tool.IsActive)
                {
                    tool.OnMapLayerRendered(g, layer);
                }
            }
        }

        private void map_MapRendered(Graphics g)
        {
            // TODO: review, migrated from GeometryEditor
            if (g == null)
            {
                return;
            }

            //UserLayer.Render(g, this.mapbox.Map);
            // always draw trackers when they exist -> full redraw when trackers are deleted
            SelectTool.Render(g, Map);
            zoomHistoryTool.MapRendered(Map);
        }

        private void map_PropertyChanged_Delayed(object sender, PropertyChangedEventArgs e)
        {
            if (IsDisposed)
            {
                return;
            }

            foreach (IMapTool tool in tools.ToArray())
            {
                tool.OnMapPropertyChanged(sender, e); // might be a problem, events are skipped
            }

            Refresh();
        }

        private void map_CollectionChanged_Delayed(object sender, NotifyCollectionChangedEventArgs e)
        {
            if(IsDisposed)
            {
                return;
            }

            // hack: some tools add extra tools and can remove them in response to a layer
            // change. For example NetworkEditorMapTool adds NewLineTool for NetworkMapLayer
            foreach (IMapTool tool in tools.ToArray())
            {
                if (tools.Contains(tool)) // ???
                {
                    tool.OnMapCollectionChanged(sender, e);
                }
            }

            var layer = e.Item as ILayer;

            if (layer == null)
            {
                return;
            }

            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    bool allLayersWereEmpty = Map.Layers.Except(new[] {layer}).All(l => l.Envelope.IsNull);
                    if (allLayersWereEmpty && !layer.Envelope.IsNull)
                    {
                        map.ZoomToExtents();
                    }
                    break;
                case NotifyCollectionChangedAction.Replace:
                    throw new NotImplementedException();
            }

            Refresh();
        }

        public static void RandomizeLayerColors(VectorLayer layer)
        {
            layer.Style.EnableOutline = true;
            layer.Style.Fill =
                new SolidBrush(Color.FromArgb(80, m_DefaultColors[m_DefaultColorIndex%m_DefaultColors.Length]));
            layer.Style.Outline =
                new Pen(
                    Color.FromArgb(100,
                                   m_DefaultColors[
                                       (m_DefaultColorIndex + ((int) (m_DefaultColors.Length*0.5)))%
                                       m_DefaultColors.Length]), 1f);
            m_DefaultColorIndex++;
        }

        // TODO: add smart resize here, probably can cache some area around map
        protected override void OnResize(EventArgs e)
        {
            if (map != null && ClientSize.Width > 0 && ClientSize.Height > 0)
            {
                //log.DebugFormat("Resizing map '{0}' from {1} to {2}: ", map.Name, map.Size, ClientSize);
                map.Size = ClientSize;
                Refresh();
            }

            base.OnResize(e);
        }

        private void MapBox_LostFocus(object sender, EventArgs e)
        {
        }

        // TODO handle arrow keys. MapTool should handle key
        protected override bool ProcessDialogKey(Keys keyData)
        {
            switch (keyData)
            {
                case Keys.Down:
                    break;
                case Keys.Up:
                    break;
                case Keys.Left:
                    break;
                case Keys.Right:
                    break;
                default:
                    break;
            }
            return base.ProcessDialogKey(keyData);
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            bool shouldRefresh = false;

            foreach (IMapTool tool in tools)
            {
                if (e.KeyCode == Keys.Escape)
                {
                    tool.Cancel();
                    shouldRefresh = true;
                    continue;
                }
                tool.OnKeyDown(e);
            }
            if ((e.KeyCode == Keys.Delete) && (!e.Handled))
            {
                deleteTool.DeleteSelection();
                shouldRefresh = true;
            }
            if (shouldRefresh)
            {
                Refresh();
            }
            base.OnKeyDown(e);
        }

        protected override void OnKeyUp(KeyEventArgs e)
        {
            foreach (IMapTool tool in tools)
            {
                if (tool.IsActive)
                {
                    tool.OnKeyUp(e);
                }
            }

            base.OnKeyUp(e);
        }

        protected override void OnMouseHover(EventArgs e)
        {
            foreach (IMapTool tool in tools)
            {
                if (tool.IsActive)
                {
                    tool.OnMouseHover(null, e);
                }
            }

            base.OnMouseHover(e);
        }

        protected override void OnMouseDoubleClick(MouseEventArgs e)
        {
            if (map == null)
            {
                return;
            }

            foreach (IMapTool tool in tools)
            {
                if (tool.IsActive)
                {
                    tool.OnMouseDoubleClick(this, e);
                }
            }
            // todo (TOOLS-1151) move implemention in mapView_MouseDoubleClick to SelectTool::OnMouseDoubleClick?
            if (SelectTool.IsActive)
            {
                base.OnMouseDoubleClick(e);
            }
        }

        protected override void OnMouseWheel(MouseEventArgs e)
        {
            if (map == null)
            {
                return;
            }

            ICoordinate mousePosition = map.ImageToWorld(new Point(e.X, e.Y));

            foreach (IMapTool tool in tools)
            {
                if (tool.IsActive)
                {
                    tool.OnMouseWheel(mousePosition, e);
                }
            }

            base.OnMouseWheel(e);
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);

            if (map == null)
            {
                return;
            }
            ICoordinate worldPosition = map.ImageToWorld(new Point(e.X, e.Y));

            foreach (IMapTool tool in Tools)
            {
                if (tool.IsActive)
                {
                    tool.OnMouseMove(worldPosition, e);
                }
            }
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);

            if (!Focused)
            {
                Focus();
            }

            if (map == null)
            {
                return;
            }

            ICoordinate worldPosition = map.ImageToWorld(new Point(e.X, e.Y));

            foreach (IMapTool tool in tools)
            {
                if (tool.IsActive)
                {
                    tool.OnMouseDown(worldPosition, e);
                }
            }
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            if (map == null)
            {
                return;
            }

            ICoordinate worldPosition = map.ImageToWorld(new Point(e.X, e.Y));


            var contextMenu = new ContextMenuStrip();

            foreach (IMapTool tool in tools)
            {
                if (tool.IsActive)
                {
                    tool.OnMouseUp(worldPosition, e);

                    if (e.Button == MouseButtons.Right)
                    {
                        tool.OnBeforeContextMenu(contextMenu, worldPosition);
                    }
                }
            }

            contextMenu.Show(PointToScreen(e.Location));

            base.OnMouseUp(e);
        }

        protected override void OnDragEnter(DragEventArgs drgevent)
        {
            foreach (IMapTool tool in tools)
            {
                if (tool.IsActive)
                {
                    tool.OnDragEnter(drgevent);
                }
            }

            base.OnDragEnter(drgevent);
        }

        /// <summary>
        /// Drop object on map. This can result in new tools in the tools collection
        /// </summary>
        /// <param name="e"></param>
        protected override void OnDragDrop(DragEventArgs e)
        {
            IList<IMapTool> mapTools = new List<IMapTool>();
            foreach (IMapTool tool in tools)
            {
                if (tool.IsActive)
                {
                    mapTools.Add(tool);
                }
            }
            foreach (IMapTool tool in mapTools)
            {
                tool.OnDragDrop(e);
            }

            base.OnDragDrop(e);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            if (Map == null || Map.Image == null)
            {
                return;
            }

            // TODO: fix this
            if (Map.Image.PixelFormat == PixelFormat.Undefined)
            {
                log.Error("Map image is broken - bug!");
                return;
            }

            e.Graphics.DrawImageUnscaled(Map.Image, 0, 0);

            //Trace.WriteLine("OnPaint " + onPaint++);
            foreach (IMapTool tool in tools)
            {
                if (tool.IsActive)
                {
                    tool.OnPaint(e);
                }
            }
            SelectTool.OnPaint(e);

            base.OnPaint(e);
        }

        /// <summary>
        /// Fired when the map has been refreshed
        /// </summary>
        public event EventHandler MapRefreshed;
    }
}