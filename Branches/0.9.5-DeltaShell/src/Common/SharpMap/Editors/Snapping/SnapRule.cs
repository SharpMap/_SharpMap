using System;
using GeoAPI.Extensions.Feature;
using GeoAPI.Geometries;
using GisSharpBlog.NetTopologySuite.Geometries;
using NetTopologySuite.Extensions.Geometries;
using System.Collections.Generic;
using SharpMap.Api;
using SharpMap.Api.Editors;
using SharpMap.Layers;

namespace SharpMap.Editors.Snapping
{
    public class SnapRule : ISnapRule
    {
        /// <summary>
        /// Criteria is used to select (filter) feature candidates (where can we snap)
        /// </summary>
        public Func<ILayer, IFeature, bool> Criteria { get; set; }
        
        /// <summary>
        /// Target layer, where new features will be created.
        /// </summary>
        public ILayer NewFeatureLayer { get; set; }
        
        public SnapRole SnapRole { get; set; }
        
        public virtual bool Obligatory { get; set; }
        
        /// <summary>
        /// Number of pixels where snapping will start working.
        /// 
        /// Used to construct the envelope used to select features which should be evaluated by this snap rule.
        /// </summary>
        public int PixelGravity { get; set; }

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
        public virtual SnapResult Execute(IFeature sourceFeature, IFeature[] snapCandidates, ILayer[] snapLayers, IGeometry sourceGeometry, IList<IFeature> snapTargets, ICoordinate worldPos, IEnvelope envelope, int trackingIndex)
        {
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
            SnapResult snapResult = null;

            if(snapCandidates == null || snapLayers == null)
            {
                return snapResult;
            }

            for (int i = 0; i < snapCandidates.Length; i++)
            {
                var feature = snapCandidates[i];
                var layer = snapLayers[i];

                if(Criteria != null && !Criteria(layer, feature))
                {
                    continue;
                };

                IGeometry geometry = feature.Geometry;
                if ((null != snapTargets) && (snapTargetGeometries.IndexOf(geometry) == -1))
                    continue;
                if (SnapRole == SnapRole.None) 
                    continue;
                if (geometry is IPolygon)
                {
                    IPolygon polygon = (IPolygon)geometry;
                    switch (SnapRole)
                    {
                        case SnapRole.Free:
                            PolygonSnapFree(ref minDistance, ref snapResult, polygon, worldPos);
                            break;
                        case SnapRole.AllTrackers:
                            GeometrySnapAllTrackers(ref minDistance, ref snapResult, polygon, worldPos);
                            break;
                        default:
                            //case SnapRole.FreeAtObject:
                            PolygonSnapFreeAtObject(ref minDistance, ref snapResult, polygon, worldPos);
                            break;
                    }
                }
                if (geometry is ILineString)
                {
                    ILineString lineString = (ILineString)geometry;

                    switch (SnapRole)
                    {
                        case SnapRole.Free:
                            LineStringSnapFree(ref minDistance, ref snapResult, lineString, worldPos);
                            break;
                        case SnapRole.FreeAtObject:
                            LineStringSnapFreeAtObject(ref minDistance, ref snapResult, feature, lineString, worldPos);
                            break;
                        case SnapRole.TrackersNoStartNoEnd:
                            break;
                        case SnapRole.AllTrackers:
                            LineStringSnapAllTrackers(ref minDistance, ref snapResult, lineString, worldPos);
                            break;
                        case SnapRole.Start:
                            LineStringSnapStart(ref snapResult, lineString);
                            break;
                        case SnapRole.End:
                            LineStringSnapEnd(ref snapResult, lineString);
                            break;
                        case SnapRole.StartEnd:
                            LineStringSnapStartEnd(ref minDistance, ref snapResult, lineString, worldPos);
                            break;
                    }
                }
                else if (geometry is IPoint)
                {
                    snapResult = new SnapResult(geometry.Coordinates[0], null, NewFeatureLayer, geometry, 0, 0) { Rule = this };
                }

                snapResult.NewFeatureLayer = NewFeatureLayer;
            } // foreach (IGeometry geometry in snapCandidates)


            return snapResult;
        }

        public void LineStringSnapStartEnd(ref double minDistance, ref SnapResult snapResult, ILineString lineString, ICoordinate worldPos)
        {
            ICoordinate c1 = lineString.Coordinates[0];
            ICoordinate location;
            int snapIndexPrevious;
            int snapIndexNext;
            double distance = GeometryHelper.Distance(c1.X, c1.Y, worldPos.X, worldPos.Y);
            if (distance < minDistance)
            {
                location = c1;
                snapIndexPrevious = 0;
                snapIndexNext = 0;
                minDistance = distance;
                snapResult = new SnapResult(location, null, null, lineString, snapIndexPrevious, snapIndexNext) { Rule = this };
            }
            ICoordinate c2 = lineString.Coordinates[lineString.Coordinates.Length - 1];
            distance = GeometryHelper.Distance(c2.X, c2.Y, worldPos.X, worldPos.Y);
            if (distance >= minDistance)
                return;
            location = c2;
            snapIndexPrevious = lineString.Coordinates.Length - 1;
            snapIndexNext = lineString.Coordinates.Length - 1;
            snapResult = new SnapResult(location, null, null, lineString, snapIndexPrevious, snapIndexNext) { Rule = this };
        }

        public void LineStringSnapEnd(ref SnapResult snapResult, ILineString lineString)
        {
            snapResult = new SnapResult(lineString.Coordinates[lineString.Coordinates.Length - 1], null, null, lineString,
                                  lineString.Coordinates.Length - 1, lineString.Coordinates.Length - 1) { Rule = this };
        }

        public void LineStringSnapStart(ref SnapResult snapResult, ILineString lineString)
        {
            snapResult = new SnapResult(lineString.Coordinates[0], null, null, lineString, 0, 0) { Rule = this };
        }

        public void LineStringSnapAllTrackers(ref double minDistance, ref SnapResult snapResult, ILineString lineString, ICoordinate worldPos)
        {
            GeometrySnapAllTrackers(ref minDistance, ref snapResult, lineString, worldPos);
        }

        private void GeometrySnapAllTrackers(ref double minDistance, ref SnapResult snapResult, IGeometry geometry,
                                                ICoordinate worldPos)
        {
            var coordinates = geometry.Coordinates;
            for (int i = 0; i < coordinates.Length; i++)
            {
                ICoordinate c1 = coordinates[i];
                double distance = GeometryHelper.Distance(c1.X, c1.Y, worldPos.X, worldPos.Y);
                if (distance >= minDistance)
                    continue;
                minDistance = distance;
                snapResult = new SnapResult(coordinates[i], null, null, geometry, i, i) {Rule = this};
            }
        }

        public void LineStringSnapFreeAtObject(ref double minDistance, ref SnapResult snapResult, IFeature feature, ILineString lineString, ICoordinate worldPos)
        {
            int vertexIndex;
            var nearestPoint = GeometryHelper.GetNearestPointAtLine(lineString, worldPos, minDistance, out vertexIndex);

            if (nearestPoint == null)
            {
                return;
            }

            minDistance = GeometryHelper.Distance(nearestPoint.X, nearestPoint.Y, worldPos.X, worldPos.Y);
            snapResult = new SnapResult(nearestPoint, feature, null, lineString, vertexIndex - 1, vertexIndex) { Rule = this };
        }

        public void LineStringSnapFree(ref double minDistance, ref SnapResult snapResult, ILineString lineString, ICoordinate worldPos)
        {
            for (int i = 1; i < lineString.Coordinates.Length; i++)
            {
                ICoordinate c1 = lineString.Coordinates[i - 1];
                ICoordinate c2 = lineString.Coordinates[i];
                double distance = GeometryHelper.LinePointDistance(c1.X, c1.Y, c2.X, c2.Y,
                                                                   worldPos.X, worldPos.Y);
                if (distance >= minDistance)
                    continue;
                minDistance = distance;
                snapResult = new SnapResult(GeometryFactory.CreateCoordinate(worldPos.X, worldPos.Y), null, null, lineString, i - 1, i) { Rule = this };
            }
        }

        public void PolygonSnapFreeAtObject(ref double minDistance, ref SnapResult snapResult, IPolygon polygon, ICoordinate worldPos)
        {
            for (int i = 1; i < polygon.Coordinates.Length; i++)
            {
                ICoordinate c1 = polygon.Coordinates[i - 1];
                ICoordinate c2 = polygon.Coordinates[i];
                double distance = GeometryHelper.LinePointDistance(c1.X, c1.Y, c2.X, c2.Y,
                                                                   worldPos.X, worldPos.Y);
                if (distance >= minDistance)
                    continue;
                minDistance = distance;
                ICoordinate min_c1 = polygon.Coordinates[i - 1];
                ICoordinate min_c2 = polygon.Coordinates[i];
                snapResult = new SnapResult(GeometryHelper.NearestPointAtSegment(min_c1.X, min_c1.Y,
                                                                           min_c2.X, min_c2.Y, worldPos.X,
                                                                           worldPos.Y), null, null, polygon, i - 1, i) { Rule = this };
            }
        }

        public void PolygonSnapFree(ref double minDistance, ref SnapResult snapResult, IPolygon polygon, ICoordinate worldPos)
        {
            for (int i = 1; i < polygon.Coordinates.Length; i++)
            {
                ICoordinate c1 = polygon.Coordinates[i - 1];
                ICoordinate c2 = polygon.Coordinates[i];
                double distance = GeometryHelper.LinePointDistance(c1.X, c1.Y, c2.X, c2.Y, worldPos.X, worldPos.Y);
                if (distance >= minDistance)
                    continue;
                minDistance = distance;
                snapResult = new SnapResult(GeometryFactory.CreateCoordinate(worldPos.X, worldPos.Y), null, null, polygon,
                                            i - 1, i) { Rule = this };
            }
        }


    }
}
