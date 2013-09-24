using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using GeoAPI.Extensions.Feature;
using GeoAPI.Geometries;
using NetTopologySuite.Extensions.Geometries;
using SharpMap.Api;
using SharpMap.Api.Editors;
using SharpMap.Converters.Geometries;
using SharpMap.Data.Providers;
using SharpMap.Editors.Snapping;
using SharpMap.Layers;
using SharpMap.UI.Helpers;
using System.Collections.ObjectModel;
using SharpMap.Styles;

namespace SharpMap.UI.Tools
{
    public class NewNetworkFeatureTool : MapTool
    {
        private bool isBusy;
        private IPoint newNetworkFeature;

        /// <summary>
        /// Optional: get a feature from external providers for location IPoint
        /// </summary>
        public Func<IPoint, IEnumerable<IFeature>> GetFeaturePerProvider { get; set; }

        /// <summary>
        /// Type of optional feature
        /// </summary>
        public Type FeatureType { get; set; }

        public NewNetworkFeatureTool(Func<ILayer, bool> layerCriterion, string name)
        {
            Name = name;
            LayerFilter = layerCriterion;
        }

        public override void Render(Graphics graphics, Map mapBox)
        {
            if (null == newNetworkFeatureLayer) 
                return;
            newNetworkFeatureLayer.Render();
            graphics.DrawImage(newNetworkFeatureLayer.Image, 0, 0);
            MapControl.SnapTool.Render(graphics, mapBox);
        }

        private SnapResult snapResult;

        public VectorLayer VectorLayer { get { return Layers.Any() ? Layers.OfType<VectorLayer>().FirstOrDefault() : null; } }

        public override void OnMouseDown(ICoordinate worldPosition, MouseEventArgs e)
        {
            if (VectorLayer == null)
            {
                return;
            }

            if (e.Button != MouseButtons.Left)
            {
                return;
            }
            isBusy = true;
            StartDrawing();
            newNetworkFeature = GeometryFactory.CreatePoint(worldPosition);
            ((DataTableFeatureProvider)newNetworkFeatureLayer.DataSource).Clear();
            newNetworkFeatureLayer.DataSource.Add(newNetworkFeature);

            snapResult = MapControl.SnapTool.ExecuteLayerSnapRules(VectorLayer, null, newNetworkFeature, worldPosition, -1); //TODO check: why is this commented out in trunk?
            if (snapResult != null)
            {
                newNetworkFeature.Coordinates[0].X = snapResult.Location.X;
                newNetworkFeature.Coordinates[0].Y = snapResult.Location.Y;
            }

            newNetworkFeatureLayer.Style = MapControl.SnapTool.Failed ? errorNetworkFeatureStyle : networkFeatureStyle;
        }
        public override void OnMouseMove(ICoordinate worldPosition, MouseEventArgs e)
        {
            if (VectorLayer == null)
            {
                return;
            }

            //to avoid listening to the mousewheel in the mean time
            if (!(e.Button == MouseButtons.None || e.Button == MouseButtons.Left))
            {
                return;
            }
            StartDrawing();

            foreach (var layer in Layers)
            {
                if (!isBusy)
                {
                    // If the newNetworkFeatureTool is active but not actual dragging a new NetworkFeature show the snap position.
                    IPoint point = GeometryFactory.CreatePoint(worldPosition);

                    snapResult = MapControl.SnapTool.ExecuteLayerSnapRules(layer, null, point, worldPosition, -1);

                    if (snapResult != null)
                    {
                        break;
                    }
                }
                else 
                {
                    IPoint point = GeometryFactory.CreatePoint(worldPosition);
                    snapResult = MapControl.SnapTool.ExecuteLayerSnapRules(layer, null, point, worldPosition, -1);
                    if (snapResult != null)
                    {
                        newNetworkFeature.Coordinates[0].X = snapResult.Location.X;
                        newNetworkFeature.Coordinates[0].Y = snapResult.Location.Y;
                        newNetworkFeatureLayer.Style = networkFeatureStyle;
                        
                        break;
                    }
                    else
                    {
                        newNetworkFeature.Coordinates[0].X = worldPosition.X;
                        newNetworkFeature.Coordinates[0].Y = worldPosition.Y;
                        newNetworkFeatureLayer.Style = errorNetworkFeatureStyle;
                    }
                }
            }
            DoDrawing(true);
            StopDrawing();
        }

        public override void OnMouseUp(ICoordinate worldPosition, MouseEventArgs e)
        {
            if (!Layers.Any())
            {
                return;
            }

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
                newNetworkFeature = (IPoint) GeometryHelper.SetCoordinate(newNetworkFeature, 0, snapResult.Location);

                var layer = Layers.First();
                IFeature feature;
                if (GetFeaturePerProvider != null && layer.DataSource is FeatureCollection)
                {
                    feature = GetFeaturePerProvider(newNetworkFeature).First(); //ToDo: give the user the option to choose a provider (read model)
                    if (feature != null)
                    {
                        ((FeatureCollection)layer.DataSource).Add(feature);                     
                    }
                }
                else
                {
                    feature = layer.DataSource.Add(newNetworkFeature); 
                }

                if (feature == null)
                {
                    isBusy = false;
                    return;
                }

                layer.RenderRequired = true;
                MapControl.SelectTool.Select(layer, feature);
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
        private VectorLayer newNetworkFeatureLayer;
        private readonly Collection<IGeometry> newNetworkFeatureGeometry = new Collection<IGeometry>();
        private VectorStyle networkFeatureStyle;
        private VectorStyle errorNetworkFeatureStyle;

        private void AddDrawingLayer()
        {
            newNetworkFeatureLayer = new VectorLayer(VectorLayer) { Name = "newNetworkFeature", Map = VectorLayer.Map };

            DataTableFeatureProvider trackingProvider = new DataTableFeatureProvider(newNetworkFeatureGeometry);
            newNetworkFeatureLayer.DataSource = trackingProvider;

            networkFeatureStyle = (VectorStyle)newNetworkFeatureLayer.Style.Clone();
            errorNetworkFeatureStyle = (VectorStyle)newNetworkFeatureLayer.Style.Clone();
            MapControlHelper.PimpStyle(networkFeatureStyle, true);
            MapControlHelper.PimpStyle(errorNetworkFeatureStyle, false);
            newNetworkFeatureLayer.Style = networkFeatureStyle;
        }
        private void RemoveDrawingLayer()
        {
            newNetworkFeatureGeometry.Clear();
            newNetworkFeatureLayer = null;
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
