using System.Collections;
using GeoAPI.Extensions.Feature;
using GeoAPI.Geometries;
using SharpMap.Layers;
using System.Collections.Generic;

namespace SharpMap.UI.Snapping
{
    public class SnapRule : ISnapRule
    {
        public ILayer SourceLayer { get; set; }
        public ILayer TargetLayer { get; set; }
        public SnapRole SnapRole { get; set; }
        public virtual bool Obligatory { get; set; }
        public int PixelGravity { get; set; } // TODO: explain it

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sourceFeature"></param>
        /// <param name="sourceGeometry"></param>
        /// <param name="snapTargets"></param>
        /// <param name="worldPos"></param>
        /// <param name="envelope"></param>
        /// <param name="trackingIndex"></param>
        /// based of the selected tracker in the snapSource the rule can behave differently
        /// (only snap branches' first and last coordinate).
        /// <returns></returns>
        public virtual ISnapResult Execute(IFeature sourceFeature, IGeometry sourceGeometry, IList<IFeature> snapTargets, ICoordinate worldPos, IEnvelope envelope, int trackingIndex)
        {
            return Snap(TargetLayer, SnapRole, sourceGeometry, worldPos, envelope, PixelGravity, snapTargets);
        }

        public ISnapResult Snap(ILayer layer, SnapRole snapRole, IGeometry snapSource, ICoordinate worldPos, IEnvelope envelope, int pixelGravity, IList<IFeature> snapTargets)
        {
            var targetVectorLayer = (VectorLayer)layer;
            var snapCandidates = targetVectorLayer.DataSource.GetFeatures(envelope);

            if (!layer.Visible)
                return null;
            // hack preserve snapTargets functionality
            IList<IGeometry> snapTargetGeometries = new List<IGeometry>();
            if (null != snapTargets)
            {
                for (int i = 0; i < snapTargets.Count; i++)
                {
                    snapTargetGeometries.Add(snapTargets[i].Geometry);
                }
            }
            double minDistance = double.MaxValue; // TODO: incapsulate minDistance in ISnapResult
            ISnapResult snapResult = null;
            foreach (IFeature feature in snapCandidates)
            {
                IGeometry geometry = feature.Geometry;
                if ((null != snapTargets) && (snapTargetGeometries.IndexOf(geometry) == -1))
                    continue;
                if (snapRole == SnapRole.None) 
                    continue;
                if (geometry is IPolygon)
                {
                    IPolygon polygon = (IPolygon)geometry;
                    switch (snapRole)
                    {
                        case SnapRole.Free:
                            SnappingHelper.PolygonSnapFree(ref minDistance, ref snapResult, polygon, worldPos);
                            break;
                        default:
                            //case SnapRole.FreeAtObject:
                            SnappingHelper.PolygonSnapFreeAtObject(ref minDistance, ref snapResult, polygon, worldPos);
                            break;
                    }
                }
                if (geometry is ILineString)
                {
                    ILineString lineString = (ILineString)geometry;

                    switch (snapRole)
                    {
                        case SnapRole.Free:
                            SnappingHelper.LineStringSnapFree(ref minDistance, ref snapResult, lineString, worldPos);
                            break;
                        case SnapRole.FreeAtObject:
                            SnappingHelper.LineStringSnapFreeAtObject(ref minDistance, ref snapResult, feature, lineString, worldPos);
                            break;
                        case SnapRole.TrackersNoStartNoEnd:
                            break;
                        case SnapRole.AllTrackers:
                            SnappingHelper.LineStringSnapAllTrackers(ref minDistance, ref snapResult, lineString, worldPos);
                            break;
                        case SnapRole.Start:
                            SnappingHelper.LineStringSnapStart(ref snapResult, lineString);
                            break;
                        case SnapRole.End:
                            SnappingHelper.LineStringSnapEnd(ref snapResult, lineString);
                            break;
                        case SnapRole.StartEnd:
                            SnappingHelper.LineStringSnapStartEnd(ref minDistance, ref snapResult, lineString, worldPos);
                            break;
                    }
                }
                else if (geometry is IPoint)
                {
                    SnappingHelper.PointSnap(ref snapResult, geometry);
                }
            } // foreach (IGeometry geometry in snapCandidates)
            return snapResult;
        }

    }
}
