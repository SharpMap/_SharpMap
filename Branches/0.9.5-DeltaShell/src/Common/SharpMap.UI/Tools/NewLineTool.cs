using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Forms;
using GeoAPI.Extensions.Feature;
using GeoAPI.Geometries;
using GisSharpBlog.NetTopologySuite.Geometries;
using NetTopologySuite.Extensions.Geometries;
using SharpMap.Api;
using SharpMap.Api.Editors;
using SharpMap.Editors.Snapping;
using log4net;
using SharpMap.Data.Providers;
using SharpMap.Layers;
using SharpMap.Rendering;
using SharpMap.UI.Helpers;
using System.Drawing;
using System.Diagnostics;
using GeometryFactory = SharpMap.Converters.Geometries.GeometryFactory;
using Point = System.Drawing.Point;

namespace SharpMap.UI.Tools
{
    public class NewLineTool : MapTool
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(NewLineTool));

        // when adding a new geometry object store the index. 
        private bool adding;

        public bool AutoCurve { get; set; }
        private bool ActualAutoCurve { get; set; }
        public double MinDistance { get; set; }
        public double ActualMinDistance { get; set; }

        #region IEditTool Members

        public NewLineTool(Func<ILayer, bool> layerFilter, string name)
        {
            AutoAppendKey = Keys.A;
            IncreaseMinDistanceKey = Keys.R;
            DecreaseMinDistanceKey = Keys.L;
            SnapKey = Keys.S;
            RemoveLastPointKey = Keys.Back;
            ManualAppendKey = Keys.M;
            LayerFilter = layerFilter;
            Name = name;
            MinDistance = 10.0;
            AutoCurve = false;
            ActualSnapping = true;
        }

        private static ILineString RemoveDuplicatePoints(ILineString lineString)
        {
            List<ICoordinate> vertices = new List<ICoordinate> {lineString.Coordinates[0]};

            for (int i = 1; i < lineString.Coordinates.Length; i++)
            {
                // remove duplicate start points, see MouseDown
                if ((lineString.Coordinates[i].X != lineString.Coordinates[i - 1].X) ||
                    (lineString.Coordinates[i].Y != lineString.Coordinates[i - 1].Y))
                    vertices.Add(lineString.Coordinates[i]);
            }
            if (vertices.Count > 1)
                return GeometryFactory.CreateLineString(vertices.ToArray());
            return null;
        }
        private static ILineString RemoveCurvePoint(ILineString lineString, int removeNumber, ICoordinate addCoordinate)
        {
            List<ICoordinate> vertices = new List<ICoordinate>();

            for (int i = 0; (i >= 0) && (i < lineString.Coordinates.Length - removeNumber); i++)
            {
                vertices.Add(lineString.Coordinates[i]);
            }
            if (null != addCoordinate)
                vertices.Add(addCoordinate);
            if (vertices.Count > 1)
                return GeometryFactory.CreateLineString(vertices.ToArray());
            return null;
        }

        private static ILineString AppendCurvePoint(ILineString lineString, ICoordinate worldPos)
        {
            List<ICoordinate> vertices = new List<ICoordinate>();

            for (int i = 0; i < lineString.Coordinates.Length; i++)
            {
                if (1 == i)
                {
                    // remove duplicate start points, see MouseDown
                    if ((lineString.Coordinates[0].X != lineString.Coordinates[1].X) &&
                        (lineString.Coordinates[0].Y != lineString.Coordinates[1].Y))
                    vertices.Add(lineString.Coordinates[i]);
                }
                else
                {
                    vertices.Add(lineString.Coordinates[i]);
                }
            }
            vertices.Add((ICoordinate)worldPos.Clone());
            return GeometryFactory.CreateLineString(vertices.ToArray());
        }

        public VectorLayer VectorLayer { get { return Layers.OfType<VectorLayer>().FirstOrDefault(); } }

        // note the layer in MapTool is the layer that is the target of the newLineTool. newLineLayer is 
        // layer that enables fast updates during creation of a new line.
        private VectorLayer newLineLayer;
        private readonly Collection<IGeometry> newLineGeometry = new Collection<IGeometry>();
        private void AddDrawingLayer()
        {
            newLineLayer = new VectorLayer(VectorLayer)
                               {
                                   RenderRequired = true,
                                   Name = "newLine",
                                   Map = VectorLayer == null? Map : VectorLayer.Map
                               };

            DataTableFeatureProvider trackingProvider = new DataTableFeatureProvider(newLineGeometry);
            newLineLayer.DataSource = trackingProvider;
            MapControlHelper.PimpStyle(newLineLayer.Style, true);
        }
        private void RemoveDrawingLayer()
        {
            newLineLayer = null;
        }

        public override void StartDrawing()
        {
            base.StartDrawing();
            AddDrawingLayer();
        }
        public override void StopDrawing()
        {
            base.StopDrawing();
            RemoveDrawingLayer();
        }

        private void StartNewLine(ICoordinate worldPos)
        {
            List<ICoordinate> verticies = new List<ICoordinate>
                                              {
                                                  GeometryFactory.CreateCoordinate(worldPos.X, worldPos.Y),
                                                  GeometryFactory.CreateCoordinate(worldPos.X, worldPos.Y)
                                              };
            ILineString lineString = GeometryFactory.CreateLineString(verticies.ToArray());

            ((DataTableFeatureProvider)newLineLayer.DataSource).Clear();
            newLineLayer.DataSource.Add(lineString);

            adding = true;
            ActualAutoCurve = AutoCurve;
            ActualMinDistance = MinDistance;
        }

        public override void OnMouseDown(ICoordinate worldPosition, MouseEventArgs e)
        {
            if (VectorLayer == null)
            {
                return;
            }
            
            if(e.Button == MouseButtons.Middle)
            {
                Cancel();
            }
            if (e.Button != MouseButtons.Left)
                return;

            isBusy = true;
            StartDrawing();

            TemporalEnd = false;

            if ((!adding))//|| (AutoCurve))
            {
                StartNewLine(Snap(worldPosition).Location);
            }
            else
            {
                AppendCoordinate((ILineString)newLineGeometry[0], worldPosition);
                StartDrawing();
                DoDrawing(true);
                StopDrawing();
            }
        }


        public override void OnMouseMove(ICoordinate worldPosition, MouseEventArgs e)
        {
            if (VectorLayer == null)
            {
                return;
            }

            if (!((MouseButtons.None == e.Button) || (MouseButtons.Left == e.Button)))
            {
                return;
            }

            // Execute snapping rules and show snap result, even when not in adding mode.
            if ((!adding) && (null != MapControl))
            {
                MapControl.SnapTool.ExecuteLayerSnapRules(VectorLayer, null, null, worldPosition, -1);
            }

            if (adding)
            {
                var lineString = (ILineString)newLineGeometry[0];
                if (ActualAutoCurve)
                {
                    AppendCoordinate(lineString, worldPosition);
                }
                else
                {
                    ShowSnap(lineString, worldPosition);
                }
            }
            StartDrawing();
            DoDrawing(true);
            StopDrawing();
        }

        /// <summary>
        /// Adds the 'sub'line to be added after the next click to the snaplayer when tool is not in autoAppend mode.
        /// </summary>
        /// <param name="lineString"></param>
        /// <param name="worldPosition"></param>
        private void ShowSnap(ILineString lineString, ICoordinate worldPosition)
        {
            ((DataTableFeatureProvider)MapControl.SnapTool.SnapLayer.DataSource).Clear();

			if (VectorLayer != null)
            	MapControl.SnapTool.SnapLayer.CoordinateTransformation = VectorLayer.CoordinateTransformation;

            MapControl.SnapTool.AddSnap(lineString, lineString.Coordinates[lineString.Coordinates.Length - 1], Snap(worldPosition).Location);
        }

        private SnapResult Snap(ICoordinate worldPosition)
        {
            if (ActualSnapping)
            {
                return MapControl.SnapTool.ExecuteLayerSnapRules(VectorLayer, null, adding ? newLineGeometry[0] : null,
                    worldPosition, adding ? newLineGeometry[0].Coordinates.Length - 1 : -1);
            }
            return new SnapResult(worldPosition, null, null, null, -1, -1);
        }

        private void AppendCoordinate(ILineString lineString, ICoordinate worldPosition)
        {
            var newLineString = (ILineString)lineString.Clone();

            double worldDistance = MapHelper.ImageToWorld(Map, (int)ActualMinDistance);

            ICoordinate coordinate;
            if (TemporalEnd)
                coordinate = lineString.Coordinates[lineString.Coordinates.Length - 2];
            else
                coordinate = lineString.Coordinates[lineString.Coordinates.Length - 1];
            if (worldPosition.Distance(coordinate) > worldDistance)
            {
                // if distance is larger than marge add new coordinate at exact location. During drawing line
                // you do not want to snap. For example a line should be able to pass very near a node.

                // HACK: not nice to solve here. If autocurve do not add snapped point when dragging. If not auto
                // curve use the actualSnapping value (=Snap)
                ICoordinate SnapLocation = worldPosition;
                if (!AutoCurve)
                    SnapLocation = Snap(worldPosition).Location;
                if (TemporalEnd)
                    newLineString = (ILineString)GeometryHelper.SetCoordinate(lineString, lineString.Coordinates.Length - 1, SnapLocation);
                else
                    newLineString = AppendCurvePoint(lineString, SnapLocation);
                TemporalEnd = false;
            }
            else
            {
                
                ICoordinate snapLocation = Snap(worldPosition).Location;
                if (TemporalEnd)
                    newLineString = (ILineString)GeometryHelper.SetCoordinate(lineString, lineString.Coordinates.Length - 1, snapLocation);
                else
                    newLineString = AppendCurvePoint(lineString, snapLocation);
                TemporalEnd = true;
            }
            newLineGeometry.Clear();
            newLineGeometry.Add(newLineString);
        }

        public override void OnMouseUp(ICoordinate worldPosition, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left)
                return;
            if (newLineGeometry.Count == 0)
            {
                return;
            }

            SnapResult snapResult = MapControl.SnapTool.ExecuteLayerSnapRules(VectorLayer, null, adding ? newLineGeometry[0] : null,
                                                            worldPosition,
                                                            adding ? newLineGeometry[0].Coordinates.Length - 1 : -1);

            ILineString lineString = (ILineString)newLineGeometry[0];
            if (null == lineString)
            {
                isBusy = false;
                return;
            }
            if (ActualAutoCurve)
            {
                snapResult = Snap(worldPosition);
                if (null == snapResult)
                {
                    // hack if obligatory snapping failed mimic result. This is not valid for NewNetworkFeatureTool
                    // Think of solution within snaprule
                    snapResult = new SnapResult(worldPosition, null, null, null, -1, -1);
                }
                    
                if (TemporalEnd)
                    lineString = (ILineString)GeometryHelper.SetCoordinate(lineString, lineString.Coordinates.Length - 1, snapResult.Location);
                else
                    lineString = AppendCurvePoint(lineString, snapResult.Location);
                    
                if (!KeepDuplicates)
                {
                    lineString = RemoveDuplicatePoints(lineString);
                }
                adding = false;
                newLineGeometry[0] = lineString;
                //Flush();
                SelectTool selectTool = MapControl.SelectTool;

                if (null != lineString && snapResult != null)
                {
                    // TODO: call interactor here instead of feature provider
                    IFeature newFeature;
                    try
                    {
                        if (CloseLine)
                        {
                            lineString = CloseLineString(lineString);
                        }

                        newFeature = VectorLayer.DataSource.Add(lineString); // will add Cross Section and call ConnectCrossSectionToBranch

                        // was adding succesfull?
                        if (null != newFeature)
                        {
                            //Layer.RenderRequired = true;
                            MapControl.SelectTool.Select(VectorLayer, newFeature);
                        }
                    }
                    catch (Exception exception)
                    {
                        // an exception during add operation can fail; for example when adding a branch feature
                        log.Warn(exception.Message);
                    }
                }
                else
                {
                    // do not add a linestring with zero length
                    selectTool.Clear();
                }
                adding = false;
                StopDrawing();
                newLineGeometry.Clear();
                isBusy = false;
            }
            else
            {
                isBusy = true;
            }
            
            VectorLayer.RenderRequired = true;
            
            MapControl.Refresh();
        }

        public bool KeepDuplicates { get; set; }

        private static ILineString CloseLineString(ILineString lineString)
        {
            var coordinates = lineString.Coordinates.OfType<ICoordinate>();
            return new LineString(coordinates.Concat(new[] {coordinates.First()}).ToArray());
        }

        public override void OnMouseDoubleClick(object sender, MouseEventArgs e)
        {
            if (newLineGeometry.Count == 0)
            {
                return;
            }

            if (!ActualAutoCurve)
            {
                ILineString lineString = (ILineString)newLineGeometry[0];
                if (null == lineString)
                {
                    isBusy = false;
                    return;
                }
                Flush();
                MapControl.Refresh();
            }
            adding = false;
        }

        public override void ActiveToolChanged(IMapTool newTool)
        {
            adding = false;
        }

        private bool isBusy;
        public override bool IsBusy
        {
            get { return isBusy; }
        }

        public Keys AutoAppendKey { get; set; }
        public Keys ManualAppendKey { get; set; }
        public Keys RemoveLastPointKey { get; set; }
        public Keys SnapKey { get; set; }
        public Keys DecreaseMinDistanceKey { get; set; }
        public Keys IncreaseMinDistanceKey { get; set; }
        public bool ActualSnapping { get; set; }
        public bool TemporalEnd { get; set; }
        public bool CloseLine { get; set; }

        public override void Render(Graphics graphics, Map mapBox)
        {
            if (null == newLineLayer) 
                return;
            if (newLineLayer.Map == null)
            {
                newLineLayer.Map = mapBox;
            }
            newLineLayer.Render();
            graphics.DrawImage(newLineLayer.Image, 0, 0);
            MapControl.SelectTool.Render(graphics, mapBox); 
            MapControl.SnapTool.Render(graphics, mapBox);
        }
        static int counter;
        public override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);

            if (e.KeyCode == AutoAppendKey)
            {
                Trace.WriteLine("AutoAppendKey " + counter++);
                ActualAutoCurve = true;
            }
            else if (e.KeyCode == ManualAppendKey)
            {
                Trace.WriteLine("ManualAppendKey " + counter++);
                ActualAutoCurve = false;
            }
            else if (e.KeyCode == DecreaseMinDistanceKey)
            {
                ActualMinDistance /= 2;
            }
            else if (e.KeyCode == IncreaseMinDistanceKey)
            {
                ActualMinDistance *= 2;
            }
            else if (e.KeyCode == RemoveLastPointKey)
            {
                Trace.WriteLine("RemoveLastPointKey " + counter++);
                if (1 == newLineGeometry.Count)
                {
                    ILineString lineString = (ILineString)newLineGeometry[0];
                    newLineGeometry.Clear();
                    Point point = MapControl.PointToClient(Control.MousePosition);
                    ICoordinate worldPosition = Map.ImageToWorld(point);
                    lineString = RemoveCurvePoint(lineString, ActualAutoCurve ? 2 : 1, ActualAutoCurve ? worldPosition : null);
                    if (null != lineString)
                    {
                        newLineGeometry.Add(lineString);
                        if (!ActualAutoCurve)
                        {
                            // also update the snapping line
                            ShowSnap(lineString, worldPosition);
                        }
                    }
                    else
                    {
                        adding = false;
                    }
                    StartDrawing();
                    DoDrawing(true);
                    StopDrawing();
                }
            }
            else if (e.KeyCode == SnapKey)
            {
                //Trace.WriteLine("SnapKey " + counter++);
                ActualSnapping = !ActualSnapping;
                if (!ActualSnapping)
                    MapControl.SnapTool.Reset();
                if (IsActive)
                {
                    log.Info(ActualSnapping ? "Snapping Enabled" : "Snapping Disabled");
                }
            }
        }
        public void Flush()
        {
            var lineString = (ILineString)newLineGeometry[0];
            if (null == lineString) return;

            // MouseDoubleClick has added 2 points at the end of the line; remove the last point.
            lineString = RemoveDuplicatePoints(lineString);
            
            if (null == lineString) return;

            if (CloseLine)
            {
                lineString = CloseLineString(lineString);
            }

            var featureProvider = VectorLayer.DataSource;
            if (null != lineString)
            {
                featureProvider.Add(lineString);
            }
            StopDrawing();

            int featureCount = featureProvider.GetFeatureCount();
            var feature = featureCount > 0
                                   ? featureProvider.GetFeature(featureCount - 1)
                                   : null;

            // hack? sourceLayer doesn't have to be part of a network; thus we are
            // required to force repaint. DataSource has no knowledge of layer.
            VectorLayer.RenderRequired = true;

            if (null != feature)
            {
                MapControl.SelectTool.Select(VectorLayer, feature);
            }
            newLineGeometry.Clear();
            isBusy = false;
        }

        public override void Cancel()
        {
            RemoveDrawingLayer();
            adding = false;
            newLineGeometry.Clear();
            MapControl.SnapTool.Cancel();
            isBusy = false;
        }

        #endregion
    }
}
