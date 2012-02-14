using System.Drawing;
using System.Windows.Forms;
using GeoAPI.Geometries;
using NetTopologySuite.Extensions.Geometries;
using SharpMap.Converters.Geometries;
using SharpMap.Data.Providers;
using SharpMap.Layers;
using SharpMap.UI.Editors;
using SharpMap.UI.Helpers;
using System.Collections.ObjectModel;
using SharpMap.Styles;
using GeoAPI.Extensions.Feature;
using SharpMap.UI.Snapping;

namespace SharpMap.UI.Tools
{
    public class NewNodeTool : MapTool
    {
        private bool isBusy;
        private IPoint newNode;

        public NewNodeTool(ILayer layer)
        {
            Name = "NewNodeTool";
            Layer = layer;
        }

        public override void Render(Graphics graphics, Map mapBox)
        {
            if (null == newNodeLayer) 
                return;
            newNodeLayer.Render();
            graphics.DrawImage(newNodeLayer.Image, 0, 0);
            MapControl.SnapTool.Render(graphics, mapBox);
        }

        private ISnapResult snapResult;

        public override void OnMouseDown(ICoordinate worldPosition, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left)
            {
                return;
            }
            isBusy = true;
            StartDrawing();
            newNode = GeometryFactory.CreatePoint(worldPosition);
            ((DataTableFeatureProvider)newNodeLayer.DataSource).Clear();
            newNodeLayer.DataSource.Add(newNode);
            snapResult = MapControl.SnapTool.ExecuteLayerSnapRules(Layer, null, newNode, worldPosition, - 1);

            if (snapResult != null)
            {
                newNode.Coordinates[0].X = snapResult.Location.X;
                newNode.Coordinates[0].Y = snapResult.Location.Y;
            }
            newNodeLayer.Style = MapControl.SnapTool.Failed ? errorNodeStyle : nodeStyle;
        }
        public override void OnMouseMove(ICoordinate worldPosition, MouseEventArgs e)
        {
            //to avoid listening to the mousewheel in the mean time
            if (!(e.Button == MouseButtons.None || e.Button == MouseButtons.Left))
            {
                return;
            }
            StartDrawing();
            if (!isBusy)
            {
                // If the newNodeTool is active but not actual dragging a new 'Node' show the snap position.
                IPoint point = GeometryFactory.CreatePoint(worldPosition);
                snapResult = MapControl.SnapTool.ExecuteLayerSnapRules(Layer, null, point, worldPosition, -1);
            }
            else 
            {
                IPoint point = GeometryFactory.CreatePoint(worldPosition);
                snapResult = MapControl.SnapTool.ExecuteLayerSnapRules(Layer, null, point, worldPosition, -1);
                if (snapResult != null)
                {
                    newNode.Coordinates[0].X = snapResult.Location.X;
                    newNode.Coordinates[0].Y = snapResult.Location.Y;
                    newNodeLayer.Style = nodeStyle;
                }
                else
                {
                    newNode.Coordinates[0].X = worldPosition.X;
                    newNode.Coordinates[0].Y = worldPosition.Y;
                    newNodeLayer.Style = errorNodeStyle;
                }
            }
            DoDrawing(true);
            StopDrawing();
        }

        public override void OnMouseUp(ICoordinate worldPosition, MouseEventArgs e)
        {
            if (!isBusy)
            {
                return;
            }
            if (null == snapResult)
            {
                MapControl.SelectTool.Clear();
            }
            else
            {
                GeometryHelper.SetCoordinate(newNode, 0, snapResult.Location);
                var feature = FeatureProvider.Add(newNode);

                if(feature == null)
                {
                    isBusy = false;
                    return;
                }

                Layer.RenderRequired = true;
                MapControl.SelectTool.Select(Layer, feature, 0);
            }
            isBusy = false;
            StopDrawing();
            MapControl.Refresh();
        }

        public override bool IsBusy
        {
            get { return isBusy; }
        }

        // note the layer in MapTool is the layer that is the target of the newLineTool. newLineLayer is 
        // layer that enables fast updates during creation of a new line.
        private VectorLayer newNodeLayer;
        private readonly Collection<IGeometry> newNodeGeometry = new Collection<IGeometry>();
        private VectorStyle nodeStyle;
        private VectorStyle errorNodeStyle;
        private void AddDrawingLayer()
        {
            newNodeLayer = new VectorLayer((VectorLayer)Layer) {Name = "newNode", Map = Layer.Map};

            DataTableFeatureProvider trackingProvider = new DataTableFeatureProvider(newNodeGeometry);
            newNodeLayer.DataSource = trackingProvider;

            nodeStyle = (VectorStyle)newNodeLayer.Style.Clone();
            errorNodeStyle = (VectorStyle)newNodeLayer.Style.Clone();
            MapControlHelper.PimpStyle(nodeStyle, true);
            MapControlHelper.PimpStyle(errorNodeStyle, false);
            newNodeLayer.Style = nodeStyle;
        }
        private void RemoveDrawingLayer()
        {
            newNodeGeometry.Clear();
            newNodeLayer = null;
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

    }
}
