using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using GeoAPI.Extensions.Feature;
using SharpMap.Api;
using SharpMap.Api.Editors;
using SharpMap.Editors;
using SharpMap.Editors.Snapping;
using SharpMap.Layers;
using GeoAPI.Geometries;
using System.Collections.ObjectModel;
using SharpMap.Converters.Geometries;
using SharpMap.Data.Providers;
using SharpMap.Rendering;
using SharpMap.Styles;

namespace SharpMap.UI.Tools
{
    /// <summary>
    /// ISnapper basic function for snapping to objects. Polygon or Line basesd object should make there
    /// own implementation
    /// </summary>
    public class SnapTool : MapTool
    {
        private readonly VectorLayer snapLayer = new VectorLayer(String.Empty);
        private readonly Collection<IGeometry> geometries = new Collection<IGeometry>();
        private readonly Bitmap activeTracker;

        public bool Failed { get; private set; }

        private VectorStyle GetTrackerStyle(IFeature feature)
        {
            var style = (VectorStyle)snapLayer.Style.Clone();
            style.Symbol = activeTracker;
            return style;
        }

        public SnapTool()
        {
            activeTracker = TrackerSymbolHelper.GenerateSimple(new Pen(Color.DarkGreen),
                                                               new SolidBrush(Color.Orange), 8, 8);
            Failed = true;
            snapLayer.Name = "snapping";
            
            var provider = new DataTableFeatureProvider(geometries);
            snapLayer.DataSource = provider;
            snapLayer.Style.Line = new Pen(Color.DarkViolet, 2);
            snapLayer.Style.Fill = new SolidBrush(Color.FromArgb(127, Color.DarkSlateBlue));
            snapLayer.Style.Symbol = activeTracker;
            provider.Attributes.Columns.Add("id", typeof(string));

            var iTheme = new Rendering.Thematics.CustomTheme(GetTrackerStyle);
            snapLayer.Theme = iTheme;
        }

        public VectorLayer SnapLayer { get { return snapLayer; } }

        public override void Render(Graphics graphics, Map mapBox)
        {
            snapLayer.OnRender(graphics, mapBox);
        }
        public void AddSnap(IGeometry geometry, ICoordinate from, ICoordinate to)
        {
            var vertices = new List<ICoordinate> {from, to};
            var snapLineString = GeometryFactory.CreateLineString(vertices.ToArray());
            snapLayer.DataSource.Add(snapLineString);
        }
        public void Reset()
        {
            SnapResult = null;
            Failed = true;
            snapLayer.DataSource.Features.Clear();
        }

        /// <summary>
        /// Executes snapRule and shows the result in the snaptools' layer
        /// </summary>
        /// <param name="snapRule"></param>
        /// <param name="sourceFeature"></param>
        /// <param name="snapSource"></param>
        /// <param name="snapTargets"></param>
        /// <param name="worldPos"></param>
        /// <param name="trackingIndex"></param>
        /// <returns></returns>
        public SnapResult ExecuteSnapRule(ISnapRule snapRule, IFeature sourceFeature, IGeometry snapSource, IList<IFeature> snapTargets, ICoordinate worldPos, int trackingIndex)
        {
            float marge = (float)MapHelper.ImageToWorld(Map, snapRule.PixelGravity);
            IEnvelope envelope = MapHelper.GetEnvelope(worldPos, marge);

            var snapCandidates = GetSnapCandidates(envelope);

            var candidates = snapCandidates as DelftTools.Utils.Tuple<IFeature, ILayer>[] ?? snapCandidates.ToArray();
            var features = candidates.Select(c => c.First).ToArray();
            var layers = candidates.Select(c => c.Second).ToArray();

            SnapResult = snapRule.Execute(sourceFeature, features, layers, snapSource, snapTargets, worldPos, envelope, trackingIndex);
            ShowSnapResult(SnapResult);
            return SnapResult;
        }

        IEnumerable<DelftTools.Utils.Tuple<IFeature, ILayer>> GetSnapCandidates(IEnvelope envelope)
        {
            foreach (var layer in Map.GetAllVisibleLayers(true).Where(l => l.DataSource != null))
            {
                foreach(var feature in layer.GetFeatures(envelope))
                {
                    yield return new DelftTools.Utils.Tuple<IFeature, ILayer>(feature, layer);
                }
            }
        }
        /// <summary>
        /// Update snapping 
        /// </summary>
        /// <param name="sourceLayer"></param>
        /// The layer of feature. 
        /// <param name="feature"></param>
        /// Feature that is snapped. Feature is not always available. 
        /// <param name="geometry"></param>
        /// actual geometry of the feature that is snapped. 
        /// <param name="worldPosition"></param>
        /// <param name="trackerIndex"></param>
        public SnapResult ExecuteLayerSnapRules(ILayer sourceLayer, IFeature feature, IGeometry geometry, 
                                                ICoordinate worldPosition, int trackerIndex)
        {
            var snapRules = sourceLayer.FeatureEditor.SnapRules;

            SnapResult = null;
            for (int i = 0; i < snapRules.Count; i++)
            {
                ISnapRule rule = snapRules[i];
                ExecuteSnapRule(rule, feature, geometry, null, worldPosition, trackerIndex);
                if (null != SnapResult)
                    break;
                // If snapping failed for the last rule and snapping is obligatory 
                // any position is valid
                // todo add rule with SnapRole.Free?
                if ((!rule.Obligatory) && (i == snapRules.Count - 1))
                {
                    SnapResult = new SnapResult(worldPosition, null, sourceLayer, null, -1, -1) { Rule = rule };
                }
            }
            if (0 == snapRules.Count)
            {
                SnapResult = new SnapResult(worldPosition, null, sourceLayer, null, -1, -1);
            }
            return SnapResult;
        }

        private void ShowSnapResult(SnapResult snapResult)
        {
            ((DataTableFeatureProvider)snapLayer.DataSource).Clear();
            if (null == snapResult)
                return;
            IList<IGeometry> visibleSnaps = snapResult.VisibleSnaps;
            //if (null == visisbleSnaps)
            if (0 == visibleSnaps.Count)
            {
                List<ICoordinate> vertices = new List<ICoordinate>();
                if (-1 != snapResult.SnapIndexPrevious)
                {
                    vertices.Add(GeometryFactory.CreateCoordinate(snapResult.NearestTarget.Coordinates[snapResult.SnapIndexPrevious].X,
                        snapResult.NearestTarget.Coordinates[snapResult.SnapIndexPrevious].Y));
                }
                vertices.Add(GeometryFactory.CreateCoordinate(snapResult.Location.X, snapResult.Location.Y));
                IGeometry active = GeometryFactory.CreatePoint(snapResult.Location.X, snapResult.Location.Y);

                if (-1 != snapResult.SnapIndexNext)
                {
                    vertices.Add(GeometryFactory.CreateCoordinate(snapResult.NearestTarget.Coordinates[snapResult.SnapIndexNext].X,
                        snapResult.NearestTarget.Coordinates[snapResult.SnapIndexNext].Y));
                }

                if (vertices.Count > 1)
                {
                    ILineString snapLineString = GeometryFactory.CreateLineString(vertices.ToArray());
                    ((DataTableFeatureProvider)snapLayer.DataSource).Add(snapLineString);
                }
                ((DataTableFeatureProvider)snapLayer.DataSource).Add(active);
            }
            else
            {
                foreach (var snap in visibleSnaps)
                {
                    ((DataTableFeatureProvider)snapLayer.DataSource).Add(snap);
                }
            }
        }

        public SnapResult SnapResult { get; set; }

        public override void Cancel()
        {
            Reset();
        }
    }
}
