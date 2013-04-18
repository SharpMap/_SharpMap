using System;
using System.Collections.Generic;
using System.Drawing;
using GeoAPI.Geometries;
using SharpMap.Converters.Geometries;
using System.Windows.Forms;
using SharpMap.Layers;
using SharpMap.UI.Editors;
using SharpMap.UI.Helpers;
using SharpMap.UI.Snapping;
using GeoAPI.Extensions.Feature;

namespace SharpMap.UI.Tools
{
    public class CurvePointTool : MapTool
    {
        private bool isMoving;
        private bool isBusy;
        private ISnapResult SnapResult { get; set; }

        public CurvePointTool()
        {
            Name = "CurvePoint";
        }

        ///// <summary>
        ///// snapping specific for a tool. Called before layer specific snappping is applied.
        ///// </summary>
        ///// <param name="sourceLayer"></param>
        ///// <param name="snapSource"></param>
        ///// <param name="worldPos"></param>
        ///// <param name="Envelope"></param>
        ///// <returns></returns>
        public void Snap(ILayer sourceLayer, IGeometry snapSource, ICoordinate worldPos, IEnvelope Envelope)
        {
            SnapResult = null;
            IFeature sourceFeature = MapControl.SelectTool.FeatureEditors[0].SourceFeature;
            if (sourceFeature.Geometry != snapSource)
                return;
            SnapRole snapRole = SnapRole.FreeAtObject;
            if ((Control.ModifierKeys & Keys.Control) == Keys.Control)
                snapRole = SnapRole.Free;
            ISnapRule snapRule = new SnapRule
                                     {
                                         SourceLayer = sourceLayer,
                                         TargetLayer = sourceLayer,
                                         Obligatory = true,
                                         SnapRole = snapRole,
                                         PixelGravity = 4
                                     };

            SnapResult = MapControl.SnapTool.ExecuteSnapRule(
                                                snapRule,
                                                sourceFeature,
                                                sourceFeature.Geometry,
                                                new List<IFeature>
                                                    {
                                                        sourceFeature
                                                    },
                                                worldPos,
                                                -1);
        }


        public override bool IsBusy
        {
            get { return isBusy; }
        }

        private void InsertCurvePoint(IFeature feature)
        {
            if (feature.Geometry is ILineString)
            {
                if (null == SnapResult)
                    return;
                List<ICoordinate> vertices = new List<ICoordinate>();

                int curvePointIndex = 0;
                for (int i = 0; i < feature.Geometry.Coordinates.Length; i++)
                {
                    vertices.Add(feature.Geometry.Coordinates[i]);
                    if (i == SnapResult.SnapIndexPrevious)
                    {
                        if ((!double.IsNaN(feature.Geometry.Coordinates[i].Z)) && (!double.IsNaN(feature.Geometry.Coordinates[i + 1].Z)))
                        {
                            double previous = Math.Sqrt(Math.Pow(SnapResult.Location.X - feature.Geometry.Coordinates[i].X, 2) +
                                                Math.Pow(SnapResult.Location.Y - feature.Geometry.Coordinates[i].Y, 2));
                            double next = Math.Sqrt(Math.Pow(feature.Geometry.Coordinates[i + 1].X - SnapResult.Location.X, 2) +
                                            Math.Pow(feature.Geometry.Coordinates[i + 1].Y - SnapResult.Location.Y, 2));
                            double fraction = previous / (previous + next);
                            double z = feature.Geometry.Coordinates[i].Z +
                                       (feature.Geometry.Coordinates[i + 1].Z - feature.Geometry.Coordinates[i].Z)*
                                        fraction;
                            ICoordinate coordinate = GeometryFactory.CreateCoordinate(SnapResult.Location.X,
                                                                                      SnapResult.Location.Y);
                            coordinate.Z = z;
                            vertices.Add(coordinate);
                        }
                        else
                        {
                            vertices.Add(SnapResult.Location);
                        }
                        curvePointIndex = i + 1;
                    }
                }
                ILineString newLineString = GeometryFactory.CreateLineString(vertices.ToArray());
                SelectTool selectTool = MapControl.SelectTool;
                //VectorLayer targetLayer = selectTool.MultiSelection[0].Layer;
                ILayer targetLayer = selectTool.FeatureEditors[0].Layer;
                feature.Geometry = newLineString;
                MapControl.SelectTool.Select(targetLayer, feature, curvePointIndex);
            }
        }

        private bool RemoveCurvePoint(IFeature feature, int curvePointIndex)
        {
            ILineString lineString = (ILineString)feature.Geometry;
            List<ICoordinate> verticies = new List<ICoordinate>();
            for (int i = 0; i < lineString.Coordinates.Length; i++)
            {
                if (i != curvePointIndex)
                {
                    verticies.Add(lineString.Coordinates[i]);
                }
            }
            if (verticies.Count < 2)
            {
                // this will not result in a valid linestring; so ignore
                return false;
            }
            feature.Geometry = GeometryFactory.CreateLineString(verticies.ToArray());
            VectorLayer vectorLayer = GetLayerByFeature(feature) as VectorLayer;
            if (curvePointIndex == verticies.Count - 1)
            {
                curvePointIndex--;
            }
            MapControl.SelectTool.Select(vectorLayer, feature, curvePointIndex);
            return true;
        }

        public override void OnMouseDown(ICoordinate worldPosition, MouseEventArgs e)
        {
            isBusy = true;

            SelectTool selectTool = MapControl.SelectTool;
            if (selectTool.FeatureEditors.Count == 1)
            {
                if ((selectTool.FeatureEditors[0].SourceFeature.Geometry is ILineString) ||
                    (selectTool.FeatureEditors[0].SourceFeature.Geometry is IPolygon))
                {
                    ITrackerFeature trackerFeature = MapControl.SelectTool.GetTrackerAtCoordinate(worldPosition);
                    // hack knowledge of -1 for trackerfeature should not be here
                    // if user click visible tracker it will be handled as move, otherwize a trackers will be added.
                    if ((null != trackerFeature) && (-1 != trackerFeature.Index))
                    {
                        MapControl.MoveTool.OnMouseDown(worldPosition, e);
                        isMoving = true;
                        return;
                    }
                    if (null != SnapResult)
                    {
                        // todo ?move to FeatureMutator and add support for polygon
                        if (selectTool.FeatureEditors[0].SourceFeature.Geometry is ILineString)
                            InsertCurvePoint(selectTool.FeatureEditors[0].SourceFeature);
                        selectTool.FeatureEditors[0].Layer.RenderRequired = true;
                        MapControl.Refresh();
                        return;
                    }
                }
            }
            // if no curvepoint added handle as normal selection
            selectTool.OnMouseDown(worldPosition, e);
        }

        public override void OnMouseMove(ICoordinate worldPosition, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                //MapControl.SnapTool.Reset();
            }
            SelectTool selectTool = MapControl.SelectTool;

            if (isMoving)
            {
                MapControl.MoveTool.OnMouseMove(worldPosition, e);
            }
            else if ((selectTool.FeatureEditors.Count == 1) && (selectTool.FeatureEditors[0].SourceFeature.Geometry is ILineString))
            {
                MapControl.SelectTool.OnMouseMove(worldPosition, e);
                IEnvelope envelope = MapControlHelper.GetEnvelope(worldPosition, (float)MapControlHelper.ImageToWorld(Map, 40));
                Snap(selectTool.FeatureEditors[0].Layer, selectTool.FeatureEditors[0].SourceFeature.Geometry,
                             worldPosition, envelope);

                StartDrawing();
                DoDrawing(true);
                StopDrawing();
            }
        }

        public override void OnMouseUp(ICoordinate worldPosition, MouseEventArgs e)
        {
            if (isMoving)
                MapControl.MoveTool.OnMouseUp(worldPosition, e);
            else
                MapControl.SelectTool.OnMouseUp(worldPosition, e);
            isBusy = false;
            isMoving = false;
        }

        public override void OnKeyDown(KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Delete)
            {
                SelectTool selectTool = MapControl.SelectTool;

                List<int> indices = new List<int>(selectTool.SelectedTrackerIndices);
                indices.Sort();
                for (int i = indices.Count - 1; i >= 0; i--)
                {
                    // todo ?move to FeatureMutator and add support for polygon
                    if (selectTool.FeatureEditors[0].SourceFeature.Geometry is ILineString)
                    {
                        if (RemoveCurvePoint(selectTool.FeatureEditors[0].SourceFeature, indices[i]))
                        {
                            e.Handled = true;
                        }
                    }
                }
                MapControl.Refresh();
            }
        }

        public override void Render(Graphics graphics, Map mapBox)
        {
            MapControl.MoveTool.Render(graphics, mapBox);
        }
    }
}
