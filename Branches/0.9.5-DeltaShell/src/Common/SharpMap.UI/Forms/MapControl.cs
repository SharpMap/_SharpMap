using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Windows.Forms;
using DelftTools.Utils.Aop;
using DelftTools.Utils.Collections;
using DelftTools.Utils.Collections.Generic;
using DelftTools.Utils.Threading;
using GeoAPI.Extensions.Feature;
using GeoAPI.Geometries;
using SharpMap.Api;
using SharpMap.Api.Editors;
using SharpMap.Editors.FallOff;
using log4net;
using SharpMap.Layers;
using SharpMap.Rendering.Thematics;
using SharpMap.Styles;
using SharpMap.UI.Tools;
using SharpMap.UI.Tools.Zooming;

namespace SharpMap.UI.Forms
{
    /// <summary>
    /// MapControl Class - MapControl control for Windows forms
    /// </summary>
    [DesignTimeVisible(true)]///, NotifyPropertyChange]
    [Serializable]
    public class MapControl : Control, IMapControl
    {
        #region Delegates

        /// <summary>
        /// MouseEventtype fired from the MapImage control
        /// </summary>
        /// <param name="worldPos"></param>
        /// <param name="imagePos"></param>
        public delegate void MouseEventHandler(ICoordinate worldPos, MouseEventArgs imagePos);

        #endregion

        private static readonly ILog Log = LogManager.GetLogger(typeof(MapControl));

        private static readonly Color[] MDefaultColors = new[]
                                                              {
                                                                  Color.DarkRed, Color.DarkGreen, Color.DarkBlue,
                                                                  Color.Orange, Color.Cyan, Color.Black, Color.Purple,
                                                                  Color.Yellow, Color.LightBlue, Color.Fuchsia
                                                              };

        private static int mDefaultColorIndex;

        // other commonly-used specific tools
        private readonly CurvePointTool curvePointTool;
        private readonly FixedZoomInTool fixedZoomInTool;
        private readonly FixedZoomOutTool fixedZoomOutTool;
        private readonly LegendTool legendTool;
        private readonly MoveTool linearMoveTool;
        private readonly SolidBrush mRectangleBrush = new SolidBrush(Color.FromArgb(210, 244, 244, 244));
        private readonly Pen mRectanglePen = new Pen(Color.FromArgb(244, 244, 244), 1);
        private readonly MeasureTool measureTool;
        private readonly MoveTool moveTool;
        private readonly PanZoomTool panZoomTool;
        private readonly CoverageProfileTool profileTool;
        private readonly QueryTool queryTool;
        private readonly ZoomUsingRectangleTool rectangleZoomTool;
        private readonly SelectTool selectTool;
        private readonly DeleteTool deleteTool;

        private readonly SnapTool snapTool;
        private readonly EventedList<IMapTool> tools;
        private readonly PanZoomUsingMouseWheelTool wheelPanZoomTool;
        private readonly ZoomHistoryTool zoomHistoryTool;


        // TODO: fieds below should be moved to some more specific tools?
        private int mQueryLayerIndex;
        private Map map;
        private DelayedEventHandler<NotifyCollectionChangingEventArgs> mapCollectionChangedEventHandler;
        private DelayedEventHandler<PropertyChangedEventArgs> mapPropertyChangedEventHandler;
        private IList<IFeature> selectedFeatures = new List<IFeature>();
        private Timer refreshTimer = new Timer() { Interval = 300 };

        public void WaitUntilAllEventsAreProcessed()
        {
            while (mapCollectionChangedEventHandler != null && mapPropertyChangedEventHandler != null
                && (mapCollectionChangedEventHandler.HasEventsToProcess || mapCollectionChangedEventHandler.IsRunning
                || mapPropertyChangedEventHandler.HasEventsToProcess || mapPropertyChangedEventHandler.IsRunning))
            {
                Application.DoEvents();
            }
        }

        /// <summary>
        /// Initializes a new map
        /// </summary>
        public MapControl()
        {
            SetStyle(
                ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw |
                ControlStyles.UserPaint, true);

            LostFocus += MapBox_LostFocus;
            base.AllowDrop = true;

            tools = new EventedList<IMapTool>();

            tools.CollectionChanged += tools_CollectionChanged;

            var northArrowTool = new NorthArrowTool()
            {
                Anchor = AnchorStyles.Right | AnchorStyles.Top,
                Visible = false
            };
            Tools.Add(northArrowTool);

            var scaleBarTool = new ScaleBarTool()
            {
                Size = new Size(230, 50),
                Anchor = AnchorStyles.Right | AnchorStyles.Bottom,
                Visible = true
            };

            Tools.Add(scaleBarTool);

            legendTool = new LegendTool { Anchor = AnchorStyles.Left | AnchorStyles.Top, Visible = false };
            Tools.Add(legendTool);

            queryTool = new QueryTool();
            Tools.Add(queryTool);

            // add commonly used tools

            zoomHistoryTool = new ZoomHistoryTool();
            Tools.Add(zoomHistoryTool);

            panZoomTool = new PanZoomTool();
            Tools.Add(panZoomTool);

            wheelPanZoomTool = new PanZoomUsingMouseWheelTool() { WheelZoomMagnitude = 0.8 };
            Tools.Add(wheelPanZoomTool);

            rectangleZoomTool = new ZoomUsingRectangleTool();
            Tools.Add(rectangleZoomTool);

            fixedZoomInTool = new FixedZoomInTool();
            Tools.Add(fixedZoomInTool);

            fixedZoomOutTool = new FixedZoomOutTool();
            Tools.Add(fixedZoomOutTool);

            selectTool = new SelectTool { IsActive = true };
            Tools.Add(selectTool);

            deleteTool = new DeleteTool();
            Tools.Add(deleteTool);

            moveTool = new MoveTool { Name = "Move selected vertices", FallOffPolicy = FallOffType.None };
            Tools.Add(moveTool);

            linearMoveTool = new MoveTool
            {
                Name = "Move selected vertices (linear)",
                FallOffPolicy = FallOffType.Linear
            };
            Tools.Add(linearMoveTool);

            measureTool = new MeasureTool();
            tools.Add(measureTool);

            profileTool = new CoverageProfileTool { Name = "Make grid profile" };
            tools.Add(profileTool);

            curvePointTool = new CurvePointTool();
            Tools.Add(curvePointTool);

            snapTool = new SnapTool();
            Tools.Add(snapTool);

            var toolTipTool = new ToolTipTool();
            Tools.Add(toolTipTool);

            Width = 100;
            Height = 100;

            mapPropertyChangedEventHandler =
                new DelayedEventHandler<PropertyChangedEventArgs>(map_PropertyChanged_Delayed)
                {
                    SynchronizingObject = this,
                    FireLastEventOnly = true,
                    Delay = 300,
                    Filter = (sender, e) => sender is ILayer ||
                                            sender is VectorStyle ||
                                            sender is ITheme ||
                                            sender is IList<ILayer>,
                    Enabled = false
                };
            mapCollectionChangedEventHandler =
                new DelayedEventHandler<NotifyCollectionChangingEventArgs>(map_CollectionChanged_Delayed)
                {
                    SynchronizingObject = this,
                    FireLastEventOnly = true,
                    Delay = 300,
                    FullRefreshEventHandler = (sender, e) => OnFullRefresh(sender, e),
                    Filter = (sender, e) => sender is Map ||
                                            sender is ILayer ||
                                            sender is IList<ILayer>,
                    Enabled = false
                };

            Map = new Map(ClientSize) { Zoom = 100 };
        }

        private void OnFullRefresh(object sender, EventArgs e)
        {
            SelectTool.RefreshSelection();
        }

        void RefreshTimerTick(object sender, EventArgs e)
        {
            if (Map == null)
            {
                return;
            }

            if (Visible && !Tools.Any(t => t.IsBusy))
            {
                if (Map.Layers.Any(l => l.Visible && l.RenderRequired) || Map.RenderRequired)
                {
                    RefreshSelectTool();
                    Refresh();
                }
            }
        }

        private void RefreshSelectTool()
        {
            if (SelectTool != null)
            {
                SelectTool.RefreshFeatureInteractors();
            }
        }

        [Description("The color of selecting rectangle.")]
        [Category("Appearance")]
        public Color SelectionBackColor
        {
            get { return mRectangleBrush.Color; }
            set
            {
                //if (value != mRectangleBrush.Color)
                mRectangleBrush.Color = value;
            }
        }

        [Description("The color of selection rectangle frame.")]
        [Category("Appearance")]
        public Color SelectionForeColor
        {
            get { return mRectanglePen.Color; }
            set
            {
                //if (value != mRectanglePen.Color)
                mRectanglePen.Color = value;
            }
        }

        /// <summary>
        /// Gets or sets the index of the active query layer 
        /// </summary>
        public int QueryLayerIndex
        {
            get { return mQueryLayerIndex; }
            set { mQueryLayerIndex = value; }
        }

        #region IMapControl Members

        [Description("The map image currently visualized.")]
        [Category("Appearance")]
        public Image Image
        {
            get
            {
                if (Map == null || Width <= 0 || Height <= 0)
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
                    map.ClearImage();
                    if (refreshTimer != null)
                    {
                        refreshTimer.Stop();
                        refreshTimer.Tick -= RefreshTimerTick;
                    }
                }

                map = value;

                if (map == null)
                {
                    return;
                }

                map.Size = ClientSize;

                SubScribeMapEvents();

                SetMapStyles();

                Refresh();

                if (Visible)
                {
                    if (refreshTimer != null)
                    {
                        refreshTimer.Tick += RefreshTimerTick;
                        if (Visible)
                        {
                            refreshTimer.Start();
                        }
                    }
                }
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
            map.MapRendered -= OnMapRendered;
            map.MapLayerRendered -= OnMapLayerRendered;
        }

        private void SubScribeMapEvents()
        {
            map.CollectionChanged += mapCollectionChangedEventHandler;
            ((INotifyPropertyChanged)map).PropertyChanged += mapPropertyChangedEventHandler;
            map.MapRendered += OnMapRendered;
            map.MapLayerRendered += OnMapLayerRendered;
        }

        private void OnMapLayerRendered(Graphics g, ILayer layer)
        {
            foreach (var tool in tools.Where(tool => tool.IsActive))
            {
                tool.OnMapLayerRendered(g, layer);
            }
        }

        public IList<IMapTool> Tools
        {
            get { return tools; }
        }

        public IMapTool GetToolByName(string toolName)
        {
            return Tools.FirstOrDefault(tool => tool.Name == toolName);
            // Do not throw ArgumentOutOfRangeException UI handlers (button checked) can ask for not existing tool
        }

        public IMapTool GetToolByType(Type type)
        {
            foreach (var tool in Tools.Where(tool => tool.GetType() == type))
            {
                return tool;
            }

            throw new ArgumentOutOfRangeException(type.ToString());
        }

        public T GetToolByType<T>() where T : class
        {
            //change it to support interfaces..
            return Tools.Where(tool => tool is T).Cast<T>().FirstOrDefault();
        }

        public void ActivateTool(IMapTool tool)
        {
            if (tool == null || tool.IsActive)
            {
                return;
            }

            if (tool.AlwaysActive)
            {
                throw new InvalidOperationException("Tool is AlwaysActive, use IMapTool.Execute() to make it work");
            }

            // deactivate other tools
            foreach (var t in tools.Where(t => t.IsActive && !t.AlwaysActive))
            {
                t.IsActive = false;
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

        public PanZoomUsingMouseWheelTool WheelPanZoomTool
        {
            get { return wheelPanZoomTool; }
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

        public DeleteTool DeleteTool
        {
            get { return deleteTool; }
        }

        public CoverageProfileTool CoverageProfileTool
        {
            get { return profileTool; }
        }

        public LegendTool LegendTool
        {
            get { return legendTool; }
        }

        public SnapTool SnapTool
        {
            get { return snapTool; }
        }



        public IEnumerable<IFeature> SelectedFeatures
        {
            get { return selectedFeatures; }
            set
            {
                selectedFeatures = value.ToList();
                FireSelectedFeaturesChanged();
                if (Visible)
                {
                    Refresh();
                }
            }
        }

        private void FireSelectedFeaturesChanged()
        {
            if (SelectedFeaturesChanged != null)
            {
                SelectedFeaturesChanged(this, EventArgs.Empty);
            }
        }

        public event EventHandler SelectedFeaturesChanged;

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);

            mapPropertyChangedEventHandler.Enabled = true;
            mapCollectionChangedEventHandler.Enabled = true;
        }

        protected override void OnHandleDestroyed(EventArgs e)
        {
            mapPropertyChangedEventHandler.Enabled = false;
            mapCollectionChangedEventHandler.Enabled = false;

            if (refreshTimer != null)
            {
                refreshTimer.Tick -= RefreshTimerTick;
                refreshTimer.Stop();
                refreshTimer.Dispose();
                refreshTimer = null;
            }

            base.OnHandleDestroyed(e);
        }

        protected override void OnVisibleChanged(EventArgs e)
        {
            base.OnVisibleChanged(e);

            if (disposingActive)
                return;

            if (refreshTimer == null)
            {
                refreshTimer = new Timer() { Interval = 300 };
            }

            if (Visible)
            {
                refreshTimer.Stop();
                refreshTimer.Tick -= RefreshTimerTick;
                refreshTimer.Tick += RefreshTimerTick;
                refreshTimer.Start();
            }
            else
            {
                refreshTimer.Stop();
                refreshTimer.Tick -= RefreshTimerTick;
            }
        }


        /// <summary>
        /// Refreshes the map
        /// </summary>
        [InvokeRequired]
        public override void Refresh()
        {
            if (!DelayedEventHandlerController.FireEvents || disposed)
            {
                return; //no refreshing..bleh
            }

            if (refreshTimer != null)
            {
                refreshTimer.Stop();
            }

            try
            {
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
            }
            finally
            {
                if (Visible)
                {
                    if (refreshTimer != null)
                    {
                        refreshTimer.Start();
                    }
                }
            }
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

        private void tools_CollectionChanged(object sender, NotifyCollectionChangingEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangeAction.Add:
                    ((IMapTool)e.Item).MapControl = this;
                    break;
                case NotifyCollectionChangeAction.Remove:
                    ((IMapTool)e.Item).MapControl = null;
                    break;
                default:
                    break;
            }
        }

        /*
                private void MapMapLayerRendered(Graphics g, ILayer layer)
                {
                    foreach (var tool in tools.Where(tool => tool.IsActive))
                    {
                        tool.OnMapLayerRendered(g, layer);
                    }
                }
        */

        private void OnMapRendered(Graphics g)
        {
            // TODO: review, migrated from GeometryEditor
            if (g == null)
            {
                return;
            }

            //UserLayer.Render(g, this.mapbox.Map);
            // always draw Trackers when they exist -> full redraw when Trackers are deleted
            SelectTool.Render(g, Map);
            zoomHistoryTool.MapRendered(Map);
        }

        private void map_PropertyChanged_Delayed(object sender, PropertyChangedEventArgs e)
        {
            if (IsDisposed || !IsHandleCreated) // must be called before InvokeRequired
            {
                return;
            }

            //Log.DebugFormat("IsDisposed: {0}, IsHandleCreated: {1}, Disposing: {2}", IsDisposed, IsHandleCreated, Disposing);

            map_PropertyChanged(sender, e);
        }

        [InvokeRequired]
        private void map_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (IsDisposed)
            {
                return;
            }

            if (sender is ILayer && e.PropertyName == "Map")
            {
                return; // performance optimization, avoid double rendering
            }

            foreach (var tool in tools.ToArray())
            {
                tool.OnMapPropertyChanged(sender, e); // might be a problem, events are skipped
            }
            if (Visible)
            {
                Refresh();
            }
            else
            {
                map.Layers.ForEach(l => { if (!l.RenderRequired) l.RenderRequired = true; });
            }
        }

        private void map_CollectionChanged_Delayed(object sender, NotifyCollectionChangingEventArgs e)
        {
            if (IsDisposed || !IsHandleCreated) // must be called before InvokeRequired
            {
                return;
            }

            map_CollectionChanged(sender, e);
        }

        [InvokeRequired]
        private void map_CollectionChanged(object sender, NotifyCollectionChangingEventArgs e)
        {
            if (IsDisposed)
            {
                return;
            }

            // hack: some tools add extra tools and can remove them in response to a layer
            // change. For example NetworkEditorMapTool adds NewLineTool for NetworkMapLayer
            foreach (var tool in tools.ToArray().Where(tool => tools.Contains(tool)))
            {
                tool.OnMapCollectionChanged(sender, e);
            }

            var layer = e.Item as ILayer;

            if (layer == null)
            {
                return;
            }

            if (Map == null)
            {
                return; // may happen in multi-threaded environment
            }

            switch (e.Action)
            {
                case NotifyCollectionChangeAction.Add:
                    var allLayersWereEmpty = Map.Layers.Except(new[] { layer }).All(l => l.Envelope != null && l.Envelope.IsNull);
                    if (allLayersWereEmpty && layer.Envelope != null && !layer.Envelope.IsNull)
                    {
                        map.ZoomToExtents(); //HACK: OOPS, changing domain model from separate thread!
                    }

                    break;
                case NotifyCollectionChangeAction.Replace:
                    throw new NotImplementedException();
            }

            Refresh();
        }

        public static void RandomizeLayerColors(VectorLayer layer)
        {
            layer.Style.EnableOutline = true;
            layer.Style.Fill =
                new SolidBrush(Color.FromArgb(80, MDefaultColors[mDefaultColorIndex % MDefaultColors.Length]));
            layer.Style.Outline =
                new Pen(
                    Color.FromArgb(100,
                                   MDefaultColors[
                                       (mDefaultColorIndex + ((int)(MDefaultColors.Length * 0.5))) %
                                       MDefaultColors.Length]), 1f);
            mDefaultColorIndex++;
        }

        // TODO: add smart resize here, probably can cache some area around map
        protected override void OnResize(EventArgs e)
        {
            if (map != null && ClientSize.Width > 0 && ClientSize.Height > 0)
            {
                //log.DebugFormat("Resizing map '{0}' from {1} to {2}: ", map.Name, map.Size, ClientSize);
                map.Size = ClientSize;
                map.Layers.ForEach(l => l.RenderRequired = true);
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

        /// <summary>
        /// Handles the key pressed by the user
        /// </summary>
        /// <param name="e"></param>
        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);

            var shouldRefresh = false;

            // cache list of tools (it can change during execute of OnKeyDown)
            var toolsList = tools.ToList();

            foreach (var tool in toolsList)
            {
                if (e.KeyCode == Keys.Escape)
                {
                    // if the user presses the escape key first cancel an operation in progress
                    if (tool.IsBusy)
                    {
                        tool.Cancel();
                        shouldRefresh = true;
                    }
                    continue;
                }
                tool.OnKeyDown(e);
            }

            if ((!toolsList.Any(t => t.IsBusy)) && (e.KeyCode == Keys.Escape) && (!SelectTool.IsActive))
            {
                // if the user presses the escape key and there was no operation in progress switch to select.
                ActivateTool(SelectTool);
                shouldRefresh = true;
            }

            if (shouldRefresh)
            {
                Refresh();
            }
        }

        protected override void OnKeyUp(KeyEventArgs e)
        {
            WithActiveToolsDo(tool => tool.OnKeyUp(e));
            base.OnKeyUp(e);
        }

        protected override void OnMouseHover(EventArgs e)
        {
            WithActiveToolsDo(tool => tool.OnMouseHover(null, e));
            base.OnMouseHover(e);
        }

        private void WithActiveToolsDo(Action<IMapTool> mapToolAction)
        {
            var activeTools = tools.Where(tool => tool.IsActive).ToList();
            foreach (var tool in activeTools)
            {
                mapToolAction(tool);
            }
        }

        protected override void OnMouseDoubleClick(MouseEventArgs e)
        {
            if (map == null)
            {
                return;
            }

            WithActiveToolsDo(tool => tool.OnMouseDoubleClick(this, e));

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

            // sometimes map control is focused even when mouse is outside - then skip it
            if (e.X < 0 || e.Y < 0 || e.X > Width || e.Y > Height)
            {
                return;
            }

            var mousePosition = map.ImageToWorld(new Point(e.X, e.Y));

            WithActiveToolsDo(tool => tool.OnMouseWheel(mousePosition, e));
            base.OnMouseWheel(e);
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);

            if (map == null || !Visible || disposingActive)
            {
                return;
            }

            var worldPosition = map.ImageToWorld(new Point(e.X, e.Y));

            WithActiveToolsDo(tool => tool.OnMouseMove(worldPosition, e));
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

            var worldPosition = map.ImageToWorld(new Point(e.X, e.Y));

            WithActiveToolsDo(tool => tool.OnMouseDown(worldPosition, e));
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            if (map == null)
            {
                return;
            }

            var worldPosition = map.ImageToWorld(new Point(e.X, e.Y));

            var contextMenu = new ContextMenuStrip();

            WithActiveToolsDo(tool =>
            {
                tool.OnMouseUp(worldPosition, e);

                if (e.Button == MouseButtons.Right)
                {
                    tool.OnBeforeContextMenu(contextMenu, worldPosition);
                }
            });

            if (!disposed)
            {
                foreach (ToolStripItem item in contextMenu.Items)
                {
                    item.Visible = true;
                }
                contextMenu.Show(PointToScreen(e.Location));
            }

            //make sure the base event is fired first...HydroNetworkEditorMapTool enables the 
            base.OnMouseUp(e);
        }

        protected override void OnDragEnter(DragEventArgs drgevent)
        {
            WithActiveToolsDo(tool => tool.OnDragEnter(drgevent));
            base.OnDragEnter(drgevent);
        }

        /// <summary>
        /// Drop object on map. This can result in new tools in the tools collection
        /// </summary>
        /// <param name="e"></param>
        protected override void OnDragDrop(DragEventArgs e)
        {
            WithActiveToolsDo(tool => tool.OnDragDrop(e));
            base.OnDragDrop(e);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            //HACK: check if this works and then move this code/logic elsewhere
            if (!DelayedEventHandlerController.FireEvents)
            {
                return;//stop painting..
            }
            if (Map == null || Map.Image == null)
            {
                return;
            }

            // TODO: fix this
            if (Map.Image.PixelFormat == PixelFormat.Undefined)
            {
                Log.Error("Map image is broken - bug!");
                return;
            }

            e.Graphics.DrawImageUnscaled(Map.Image, 0, 0);

            foreach (var tool in tools.Where(tool => tool.IsActive))
            {
                tool.OnPaint(e);
            }

            SelectTool.OnPaint(e);

            base.OnPaint(e);
        }

        /// <summary>
        /// Fired when the map has been refreshed
        /// </summary>
        public event EventHandler MapRefreshed;

        private bool disposed;
        private bool disposingActive;
        protected override void Dispose(bool disposing)
        {
            if (disposingActive || disposed)
            {
                return;
            }

            disposingActive = true;

            try
            {
                if (Map != null)
                {
                    Map.ClearImage();
                }

                mapPropertyChangedEventHandler.Enabled = false;
                mapCollectionChangedEventHandler.Enabled = false;

                if (refreshTimer != null)
                {
                    refreshTimer.Tick -= RefreshTimerTick;
                    refreshTimer.Stop();
                    refreshTimer.Dispose();
                    refreshTimer = null;
                }

                mRectangleBrush.Dispose();
                mRectanglePen.Dispose();

                mapCollectionChangedEventHandler.Dispose();
                mapPropertyChangedEventHandler.Dispose();

                base.Dispose(disposing);
            }
            catch (Exception e)
            {
                Log.Error("Exception during dispose", e);
            }
            disposed = true;
            disposingActive = false;
        }
    }
}