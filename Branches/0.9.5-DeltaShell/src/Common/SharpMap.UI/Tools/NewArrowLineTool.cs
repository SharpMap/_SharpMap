using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;
using GeoAPI.Extensions.Feature;
using GeoAPI.Geometries;
using SharpMap.Api;
using SharpMap.Api.Editors;
using SharpMap.Data.Providers;
using SharpMap.Layers;
using SharpMap.UI.Helpers;
using GeometryFactory=SharpMap.Converters.Geometries.GeometryFactory;

namespace SharpMap.UI.Tools
{
    public class NewArrowLineTool: MapTool
    {
        private VectorLayer newArrowLineLayer;
        private ILineString newArrowLineGeometry;
        private ICoordinate startCoordinate;
        private ICoordinate endCoordinate;

        public NewArrowLineTool(Func<ILayer, bool> layer, string name)
        {
            LayerFilter = layer;
            Name = name;
        }

        protected ICoordinate StartCoordinate
        {
            get { return startCoordinate; }
            set 
            { 
                startCoordinate = value;
                endCoordinate = startCoordinate;
                CreateNewLineGeometry();
            }
        }

        protected ICoordinate EndCoordinate
        {
            get { return endCoordinate; }
            set
            {
                endCoordinate = value;
                CreateNewLineGeometry();
            }
        }

        public SnapResult StartSnapResult { get; private set; }

        public VectorLayer VectorLayer { get { return Layers.OfType<VectorLayer>().FirstOrDefault(); } }

        public override void Render(Graphics graphics, Map mapBox)
        {
            if (newArrowLineLayer != null && newArrowLineGeometry != null)
            {
                newArrowLineLayer.Render();
                graphics.DrawImage(newArrowLineLayer.Image, 0, 0);
            }

            MapControl.SelectTool.Render(graphics, mapBox);
            MapControl.SnapTool.Render(graphics, mapBox);
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



        public override void OnMouseDown(ICoordinate worldPosition, MouseEventArgs e)
        {
            if (VectorLayer == null)
            {
                return;
            }

            if (e.Button != MouseButtons.Left)
                return;

            var snapResult = Snap(worldPosition);

            if (startCoordinate == null)
            {
                IsBusy = true;

                if (snapResult == null) return;
                StartCoordinate = snapResult.Location;
                StartSnapResult = snapResult;
            }
            else
            {
                IsBusy = false;

                if (snapResult == null || snapResult.Location.Equals(startCoordinate))
                {
                    Cancel(); 
                    return;
                }

                EndCoordinate = snapResult.Location;

                var lineStrings = VectorLayer.DataSource.Features.Cast<IFeature>().Select(f => f.Geometry).OfType<ILineString>();
                
                // check if there are linestings (links) with the same begin and endpoint 
                if (lineStrings.Any(f => HasSameStartAndEndPoint(f, newArrowLineGeometry)) ) return;

                startCoordinate = null;
                endCoordinate = null;
                
                VectorLayer.DataSource.Add(newArrowLineGeometry);

                VectorLayer.RenderRequired = true;
                VectorLayer.Map.Render();

                StartDrawing();
                DoDrawing(true);
                StopDrawing();

                Cancel(); // clean-up junk
            }
        }

        public override void OnMouseMove(ICoordinate worldPosition, MouseEventArgs e)
        {
            if (VectorLayer == null)
            {
                return;
            }

            if (!(MouseButtons.None == e.Button || MouseButtons.Left == e.Button))
            {
                return;
            }

            if (startCoordinate != null)
            {
                EndCoordinate = GeometryFactory.CreateCoordinate(worldPosition.X, worldPosition.Y);
            }

            Snap(worldPosition);

            StartDrawing();
            DoDrawing(true);
            StopDrawing();
        }

        public override void Cancel()
        {
            RemoveDrawingLayer();
            newArrowLineGeometry = null;
            startCoordinate = null;
            StartSnapResult = null;
            endCoordinate = null;
            IsBusy = false;
            MapControl.SnapTool.Cancel();
        }

        private void AddDrawingLayer()
        {
            newArrowLineLayer = new VectorLayer(VectorLayer)
                                    {
                                        RenderRequired = true,
                                        Name = "newArrowLine",
                                        Map = VectorLayer.Map,
                                        Style =
                                            {
                                                Line =
                                                    {
                                                        StartCap = LineCap.Round,
                                                        EndCap = LineCap.ArrowAnchor
                                                    }
                                            }
                                    };

            var trackingProvider = new DataTableFeatureProvider(newArrowLineGeometry);
            newArrowLineLayer.DataSource = trackingProvider;
            MapControlHelper.PimpStyle(newArrowLineLayer.Style, true);
        }

        private void RemoveDrawingLayer()
        {
            newArrowLineLayer = null;
        }

        private void CreateNewLineGeometry()
        {
            newArrowLineGeometry = GeometryFactory.CreateLineString(new[]{startCoordinate,endCoordinate});
        }

        private static bool HasSameStartAndEndPoint(ILineString lineString1, ILineString lineString2)
        {
            return (lineString1.StartPoint.Equals(lineString2.StartPoint) && lineString1.EndPoint.Equals(lineString2.EndPoint)) ||
                   (lineString1.StartPoint.Equals(lineString2.EndPoint) && lineString1.EndPoint.Equals(lineString2.StartPoint));
        }

        protected SnapResult Snap(ICoordinate worldPosition)
        {
            var editingStarted = (startCoordinate != null);

            return MapControl.SnapTool.ExecuteLayerSnapRules(VectorLayer, StartSnapResult != null ? StartSnapResult.SnappedFeature : null, 
                                                             editingStarted? newArrowLineGeometry : null,
                                                             worldPosition,
                                                             editingStarted ? newArrowLineGeometry.Coordinates.Length - 1 : -1);
        }
    }
}