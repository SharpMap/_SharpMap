using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;
using GeoAPI.Extensions.Feature;
using GeoAPI.Geometries;
using SharpMap.Data.Providers;
using SharpMap.Layers;
using SharpMap.UI.Helpers;
using SharpMap.UI.Snapping;
using GeometryFactory=SharpMap.Converters.Geometries.GeometryFactory;

namespace SharpMap.UI.Tools
{
    public class NewArrowLineTool: MapTool
    {
        private VectorLayer newArrowLineLayer;
        private ILineString newArrowLineGeometry;
        private ICoordinate startCoordinate;
        private ICoordinate endCoordinate;

        public NewArrowLineTool(VectorLayer layer)
        {
            Layer = layer;
            Name = layer.Name;
            OverrideStyle = true;
        }

        protected ICoordinate StartCoordinate
        {
            get { return startCoordinate; }
            set 
            { 
                startCoordinate = value;
                endCoordinate = startCoordinate;
                CreateNewLine();
            }
        }

        protected ICoordinate EndCoordinate
        {
            get { return endCoordinate; }
            set
            {
                endCoordinate = value;
                CreateNewLine();
            }
        }

        public new ILayer Layer
        {
            get { return base.Layer; }
            set
            {
                base.Layer = value;

                var style = ((VectorLayer) base.Layer).Style;

                if (style != null && OverrideStyle)
                {
                    style.Line.StartCap = LineCap.Round;
                    style.Line.EndCap = LineCap.ArrowAnchor;
                }
            }
        }
        
        public override void Render(Graphics graphics, Map mapBox)
        {
            if (null == newArrowLineLayer)
                return;

            newArrowLineLayer.Render();
            graphics.DrawImage(newArrowLineLayer.Image, 0, 0);

            MapControl.SelectTool.Render(graphics, mapBox);
            MapControl.SnapTool.Render(graphics, mapBox);
        }

        private void AddDrawingLayer()
        {
            newArrowLineLayer = new VectorLayer((VectorLayer)Layer)
                                    {
                                        RenderRequired = true,
                                        Name = "newArrowLine",
                                        Map = Layer.Map
                                    };

            var trackingProvider = new DataTableFeatureProvider(newArrowLineGeometry);
            newArrowLineLayer.DataSource = trackingProvider;
            MapControlHelper.PimpStyle(newArrowLineLayer.Style, true);
        }
        private void RemoveDrawingLayer()
        {
            newArrowLineLayer = null;
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

        private void CreateNewLine()
        {
            if (newArrowLineLayer == null) return;
            
            newArrowLineGeometry = GeometryFactory.CreateLineString(new[]{startCoordinate,endCoordinate});

            ((DataTableFeatureProvider)newArrowLineLayer.DataSource).Clear();
            newArrowLineLayer.DataSource.Add(newArrowLineGeometry);
        }

        private bool isBusy;
        public override bool IsBusy
        {
            get { return isBusy; }
        }

        public bool OverrideStyle { get; set; }

        private static bool HasSameStartAndEndPoint(ILineString lineString1, ILineString lineString2)
        {
            return (lineString1.StartPoint.Equals(lineString2.StartPoint) && lineString1.EndPoint.Equals(lineString2.EndPoint)) ||
                   (lineString1.StartPoint.Equals(lineString2.EndPoint) && lineString1.EndPoint.Equals(lineString2.StartPoint));
        }

        public override void OnMouseDown(ICoordinate worldPosition, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left)
                return;

            isBusy = true;
            StartDrawing();

            var snapResult = Snap(worldPosition);

            if (startCoordinate == null)
            {
                if (snapResult == null) return;
                StartCoordinate = snapResult.Location;
            }
            else
            {
                if (snapResult == null || snapResult.Location.Equals(startCoordinate)) return;

                EndCoordinate = snapResult.Location;

                var lineStrings = Layer.DataSource.Features.Cast<IFeature>().Select(f => f.Geometry).OfType<ILineString>();
                
                // check if there are linestings (links) with the same begin and endpoint 
                if (lineStrings.Where(f => HasSameStartAndEndPoint(f, newArrowLineGeometry)).Count() > 0 ) return;

                startCoordinate = null;
                endCoordinate = null;
                
                Layer.DataSource.Add(newArrowLineGeometry);

                Layer.RenderRequired = true;
                Layer.Map.Render();

                StartDrawing();
                DoDrawing(true);
                StopDrawing();
            }
        }

        private void ShowSnap(ILineString lineString, ICoordinate worldPosition)
        {
            ((DataTableFeatureProvider)MapControl.SnapTool.Layer.DataSource).Clear();

            var snapResult = Snap(worldPosition);
            if (snapResult == null) return;

            MapControl.SnapTool.AddSnap(lineString, snapResult.Location, snapResult.Location);
        }

        protected ISnapResult Snap(ICoordinate worldPosition)
        {
            var editingStarted = (startCoordinate != null);

            return MapControl.SnapTool.ExecuteLayerSnapRules(Layer, null, 
                                                             editingStarted? newArrowLineGeometry : null,
                                                             worldPosition,
                                                             editingStarted ? newArrowLineGeometry.Coordinates.Length - 1 : -1);
        }

        public override void OnMouseMove(ICoordinate worldPosition, MouseEventArgs e)
        {
            if (startCoordinate == null) return;

            if (!((MouseButtons.None == e.Button) || (MouseButtons.Left == e.Button)))
            {
                return;
            }

            EndCoordinate = GeometryFactory.CreateCoordinate(worldPosition.X, worldPosition.Y);
            ShowSnap(newArrowLineGeometry, worldPosition);
            DoDrawing(true);
        }

        public void Flush()
        {
            var lineString = newArrowLineGeometry;

            if (null == lineString)
                return;

            FeatureProvider.Add(lineString);
            
            StopDrawing();
            
            IFeature feature = FeatureProvider.GetFeature(FeatureProvider.GetFeatureCount() - 1);
            
            // hack? sourceLayer doesn't have to be part of a network; thus we are
            // required to force repaint. DataSource has no knowledge of layer.
            Layer.RenderRequired = true;

            if (null != feature)
            {
                MapControl.SelectTool.Select(Layer, feature, 0);
            }

            newArrowLineGeometry = null;
        }

        public override void Cancel()
        {
            RemoveDrawingLayer();
            newArrowLineGeometry = null;
            startCoordinate = null;
            endCoordinate = null;
            MapControl.SnapTool.Cancel();
        }
    }
}