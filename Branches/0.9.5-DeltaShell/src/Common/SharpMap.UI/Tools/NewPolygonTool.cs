using System.Collections.Generic;
using System.Windows.Forms;
using GeoAPI.Geometries;
using GisSharpBlog.NetTopologySuite.Features;
using SharpMap.Converters.Geometries;
using SharpMap.Layers;
using SharpMap.UI.Mapping;

namespace SharpMap.UI.Tools
{
    class NewPolygonTool : MapTool
    {
        // when adding a new geometry object store the index. 
        private int newObjectIndex = -1;
        //protected ICoordinate mouseDownLocation; // TODO: remove me

        private bool autoCurve;
        public bool AutoCurve
        {
            get { return autoCurve; }
            set { autoCurve = value; }
        }
        /// <summary>
        /// minimum distance to predecessor when using the AutoCurve property is set.
        /// </summary>
        private double minDistance;

        private bool isBusy;

        public double MinDistance
        {
            get { return minDistance; }
            set { minDistance = value; }
        }

        #region IEditTool Members

        public NewPolygonTool(ILayer layer)
        {
            Layer = layer;
            minDistance = 0.0;
            autoCurve = false;
        }

        // 0 0 0 0
        // 0 1 0 0
        // 0 1 2 0
        // 0 1 2 3 0 ...
        private void AppendCurvePoint(IPolygon polygon, ICoordinate worldPos)
        {
            List<ICoordinate> vertices = new List<ICoordinate>();

            ILineString linearRing = polygon.ExteriorRing;
            for (int i = 0; i < linearRing.Coordinates.Length; i++)
            {
                if (linearRing.Coordinates.Length <= 4)
                {
                    if (1 == i)
                    {
                        if (linearRing.Coordinates[0].Equals2D(linearRing.Coordinates[1]))
                        {
                            // 0 0 ? 0 -> 0 1 ? 0 
                            vertices.Add(worldPos);
                        }
                        else
                        {
                            // 0 1 ? 0 -> 0 1 ? 0
                            vertices.Add(linearRing.Coordinates[i]);
                        }
                    }
                    else if (2 == i)
                    {
                        if (linearRing.Coordinates[1].Equals2D(linearRing.Coordinates[2]))
                        {
                            // 0 0 0 0 -> 0 1 1 0
                            vertices.Add(worldPos);
                        }
                        else
                        {
                            // 0 1 2 0 -> 0 1 2 3 0
                            vertices.Add(linearRing.Coordinates[i]);
                            vertices.Add(worldPos);
                        }
                    }
                    else
                    {
                        vertices.Add(linearRing.Coordinates[i]);
                    }
                }
                else
                {
                    if (i == (linearRing.Coordinates.Length - 1))
                    {
                        // insert before last point to keep ring closed
                        vertices.Add(worldPos);
                    }
                    vertices.Add(linearRing.Coordinates[i]);
                }
            }
            int index = FeatureProvider.GetFeatureCount() - 1;
            ILinearRing newLinearRing = GeometryFactory.CreateLinearRing(vertices.ToArray());
            IPolygon newPolygon = GeometryFactory.CreatePolygon(newLinearRing, null);
            //##layerEditor.UpdateCurvePointInserted(index, newPolygon, vertices.Count - 1);
            //((FeatureProvider)layerEditor.VectorLayer.DataSource).UpdateGeometry(index, newPolygon);
            ((Feature)FeatureProvider.Features[index]).Geometry = newPolygon;
            Layer.RenderRequired = true;
            // do not remove see newline MapControl.SelectTool.Select((VectorLayer)Layer, newPolygon, -1);
        }
        private void StartNewPolygon(ICoordinate worldPos)
        {
            List<ICoordinate> vertices = new List<ICoordinate>();
            vertices.Add(GeometryFactory.CreateCoordinate(worldPos.X, worldPos.Y));
            vertices.Add(GeometryFactory.CreateCoordinate(worldPos.X, worldPos.Y));
            vertices.Add(GeometryFactory.CreateCoordinate(worldPos.X, worldPos.Y));
            vertices.Add(GeometryFactory.CreateCoordinate(worldPos.X, worldPos.Y)); // ILinearRing must have > 3 points
            ILinearRing linearRing = GeometryFactory.CreateLinearRing(vertices.ToArray());
            IPolygon polygon = GeometryFactory.CreatePolygon(linearRing, null);

            FeatureProvider.Add(polygon);
            newObjectIndex = FeatureProvider.GetFeatureCount() - 1;

            // do not remove see newline MapControl.SelectTool.Select((VectorLayer)Layer, polygon, 1);
        }
        public void OnMouseDown(GeoAPI.Geometries.ICoordinate worldPosition, System.Windows.Forms.MouseEventArgs e)
        {
            //isBusy = true;

            //if ((-1 == newObjectIndex) || (AutoCurve))
            //{
            //    StartNewPolygon(worldPosition);
            //}
            //else
            //{
            //    SelectTool selectTool = MapControl.SelectTool;
            //    AppendCurvePoint((IPolygon)MapControl.SelectTool.MultiSelection[0].ClonedGeometry, worldPosition);
            //}
        }
        public void OnMouseMove(GeoAPI.Geometries.ICoordinate worldPosition, System.Windows.Forms.MouseEventArgs e)
        {
            //if (-1 != newObjectIndex)
            //{
            //    IPolygon polygon = (IPolygon)MapControl.SelectTool.MultiSelection[0].ClonedGeometry;
            //    if (AutoCurve)
            //    {
            //        if (worldPosition.Distance(polygon.Coordinates[polygon.Coordinates.Length - 2]) > minDistance)
            //        {
            //            AppendCurvePoint(polygon, worldPosition);
            //        }
            //    }
            //    else
            //    {
            //        MapControl.SnapTool.AddSnap(polygon, polygon.Coordinates[polygon.Coordinates.Length - 2], worldPosition, polygon.Coordinates[polygon.Coordinates.Length - 1]);
            //    }
            //}
        }

        public void OnMouseUp(GeoAPI.Geometries.ICoordinate worldPosition, System.Windows.Forms.MouseEventArgs e)
        {
            if (AutoCurve)
            {
                newObjectIndex = -1;
                MapControl.MoveTool.OnMouseUp(worldPosition, e);
            }
            isBusy = false;
        }

        public override void OnMouseDoubleClick(object sender, MouseEventArgs e)
        {
            newObjectIndex = -1;
        }

        public override void ActiveToolChanged(IMapTool newTool)
        {
            newObjectIndex = -1;
        }

        public bool IsBusy
        {
            get { return isBusy; }
        }

        #endregion
    }
}
