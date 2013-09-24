using System;
using System.Collections;
using System.Linq;
using System.Collections.Generic;
using DelftTools.Functions;
using DelftTools.Functions.Filters;
using DelftTools.Functions.Generic;
using DelftTools.Utils.Collections;
using GeoAPI.Extensions.Coverages;
using GeoAPI.Extensions.Networks;
using GeoAPI.Geometries;
using GisSharpBlog.NetTopologySuite.Index.Quadtree;
using GisSharpBlog.NetTopologySuite.LinearReferencing;
using NetTopologySuite.Extensions.Geometries;
using log4net;
using NetTopologySuite.Extensions.Networks;

namespace NetTopologySuite.Extensions.Coverages
{
    /// <summary>
    /// Utilities for <see cref="NetworkCoverage"/>
    /// </summary>
    public class NetworkCoverageHelper
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof (NetworkCoverageHelper));

        public static void UpdateSegments(INetworkCoverage coverage)
        {
            if (coverage.Network == null)
            {
                return;
            }

            try
            {
                coverage.Segments.Clear();

                switch (coverage.SegmentGenerationMethod)
                {
                    case SegmentGenerationMethod.SegmentPerLocation:
                        UpdateSegmentsSegmentPerLocation(coverage);
                        break;
                    case SegmentGenerationMethod.RouteBetweenLocations:
                        UpdateSegmentsRouteBetweenLocations(coverage);
                        break;
                    case SegmentGenerationMethod.SegmentBetweenLocations:
                        UpdateSegmentsSegmentBetweenLocations(coverage, false);
                        break;
                    case SegmentGenerationMethod.SegmentBetweenLocationsFullyCovered:
                        UpdateSegmentsSegmentBetweenLocations(coverage, true);
                        break;
                    case SegmentGenerationMethod.None:
                        UpdateSegmentsNone(coverage);
                        break;
                }
            }
                //why catch all exceptions here??? there might be something wrong that is WORTH crashing the app
                //TODO: only catch the exception you know and want to catch
            catch (Exception exception)
            {
                Log.ErrorFormat("Fatal error updating network coverage {0}; reason {1}", coverage.Name,
                                exception.Message);
                // do not rethrow, it will crash ui
            }

            for (int i = 1; i < coverage.Segments.Values.Count + 1; i++)
            {
                coverage.Segments.Values[i - 1].SegmentNumber = i;
            }

        }

        private static void UpdateSegmentsNone(INetworkCoverage coverage)
        {
            coverage.Segments.Values.Clear();
        }

        /// <summary>
        /// Updates the segments for all branches with branch location following this scheme:
        ///                b1
        /// n1---------------------------------n2
        /// 
        /// A----------C---------B----------D--    - network locations
        ///                                        - segments
        /// </summary>
        /// <param name="coverage"></param>
        /// <param name="fullyCover"></param>
        /// when set to true the segment of the branch 
        private static void UpdateSegmentsSegmentBetweenLocations(INetworkCoverage coverage, bool fullyCover)
        {
            coverage.Segments.Values.Clear();

            //sorting is done in coverage locations..don't need to do it here. This supports for reversed segments.
            coverage.Segments.SetValues(coverage.Locations.Values.GroupBy(l => l.Branch)
                                                .SelectMany(
                                                    g =>
                                                    UpdateSegmentsBranchSegmentBetweenLocations(fullyCover, g.Key, g)));
        }

        ///<summary>
        ///</summary>
        ///<param name="networkCoverage"></param>
        ///<param name="branch"></param>
        ///<param name="offsets"></param>
        public static void CreateSegments(INetworkCoverage networkCoverage, IBranch branch, IList<double> offsets)
        {
            //ClearSegments(networkCoverage, branch);
            foreach (var offset in offsets)
            {
                networkCoverage[new NetworkLocation(branch, offset)] = 0.0;
            }
        }

        //INetworkCoverage coverage, 
        private static IList<INetworkSegment> UpdateSegmentsBranchSegmentBetweenLocations(bool fullyCover,
                                                                                          IBranch branch,
                                                                                          IEnumerable<INetworkLocation>
                                                                                              branchLocations)
        {
            var segments = new List<INetworkSegment>();

            var length = branch.Length;

            // select all locations that have an offset within the branch
            var factor = 1.0; // branch.IsLengthCustom ? (branch.Geometry.Length / branch.Length) : 1.0;
            IList<double> offsets =
                branchLocations.Where(l => l.Chainage <= length).Select(l => factor*l.Chainage).ToList();

            if (0 == offsets.Count)
            {
                if (fullyCover)
                {
                    offsets.Add(0);
                    offsets.Add(length);
                }
                else
                {
                    return segments;
                }
            }
            else
            {
                if (fullyCover)
                {
                    if (Math.Abs(offsets[0]) > BranchFeature.Epsilon)
                    {
                        offsets.Insert(0, 0.0);
                    }
                    if (Math.Abs(offsets[offsets.Count - 1] - length) > BranchFeature.Epsilon)
                    {
                        offsets.Add(length);
                    }
                }
            }

            var lengthIndexedLine = new LengthIndexedLine(branch.Geometry);

            for (int i = 1; i < offsets.Count; i++)
            {
                var segment = new NetworkSegment
                    {
                        Branch = branch,
                        Chainage = offsets[i - 1],
                        Length = Math.Abs(offsets[i] - offsets[i - 1]),
                        DirectionIsPositive = offsets[i] >= offsets[i - 1],
                        // thousand bombs and grenades: ExtractLine will give either a new coordinate or 
                        // a reference to an existing object
                        Geometry = (IGeometry) lengthIndexedLine.ExtractLine(offsets[i - 1], offsets[i]).Clone()
                    };
                segments.Add(segment);
            }
            return segments;
        }


        private static void UpdateSegmentsRouteBetweenLocations(INetworkCoverage coverage)
        {
            coverage.Segments.Values.Clear();

            var coverageLocations = coverage.Locations.Values;
            for (var i = 0; i < coverageLocations.Count - 1; i++)
            {
#if MONO				
                var source = (INetworkLocation)((IMultiDimensionalArray)coverageLocations)[i];
                var target = (INetworkLocation)((IMultiDimensionalArray)coverageLocations)[i + 1];
#else
                var source = coverageLocations[i];
                var target = coverageLocations[i + 1];
#endif
                var segments = NetworkHelper.GetShortestPathBetweenBranchFeaturesAsNetworkSegments(coverage.Network,
                                                                                                   source, target);
                foreach (var segment in segments)
                {
                    if (segment.Chainage < 0 || segment.EndChainage < 0)
                    {
                        throw new ArgumentException("EndOffset or segment offset invalid");
                    }
                    coverage.Segments.Values.Add(segment);
                }
            }
        }

        private static void UpdateSegmentsSegmentPerLocation(INetworkCoverage coverage)
        {
            //for performance reasons..get the locations all at once.
            var allLocations = coverage.Locations.Values;
            foreach (var branch in coverage.Network.Branches)
            {
                UpdateSegments(coverage, branch, allLocations);
            }
        }

        public static void UpdateSegments(INetworkCoverage coverage, IBranch branch,
                                          IMultiDimensionalArray<INetworkLocation> allLocations)
        {
            if (coverage.Network == null)
            {
                return;
            }

            // remove old segments for selected branch
            foreach (var segment in coverage.Segments.Values.Where(s => s.Branch == branch).ToArray())
            {
                coverage.Segments.Values.Remove(segment);
            }

            var branchNetworkLocations = allLocations.Where(l => l.Branch == branch).Cast<INetworkLocation>();
            var skipFirst = branchNetworkLocations.FirstOrDefault() == allLocations.FirstOrDefault();
            var skipLast = branchNetworkLocations.LastOrDefault() == allLocations.LastOrDefault();

            IEnumerable<INetworkSegment> segments;
            switch (coverage.SegmentGenerationMethod)
            {
                case SegmentGenerationMethod.RouteBetweenLocations:
                    segments = NetworkHelper.GenerateSegmentsBetweenLocations(branchNetworkLocations, branch, skipFirst,
                                                                              skipLast);
                    break;
                case SegmentGenerationMethod.SegmentBetweenLocations:
                    //segments = NetworkHelper.GenerateSegmentsPerLocation(branchNetworkLocations, branch);
                    segments = UpdateSegmentsBranchSegmentBetweenLocations(false, branch, branchNetworkLocations);
                    break;
                case SegmentGenerationMethod.SegmentPerLocation:
                    segments = NetworkHelper.GenerateSegmentsPerLocation(branchNetworkLocations, branch);
                    break;
                default:
                    throw new ArgumentException(
                        string.Format("Method {0} not supported", coverage.SegmentGenerationMethod), "coverage");
            }

            foreach (var s in segments)
            {
                // todo set branch and offset to NetworkSegmentAttributeAccessor?
                // assume number of location to be at least number of segments per branch 
                coverage.Segments.Values.Add(s);
            }
        }

        public static void ExtractTimeSlice(INetworkCoverage source, INetworkCoverage targetSlice /* bad name */,
                                            DateTime dateTime,
                                            bool copyLocations)
        {
            if (!source.IsTimeDependent)
            {
                throw new ArgumentException("ExtractTimeSlice: source network coverage should be time dependent.",
                                            "source");
            }

            IMultiDimensionalArray<INetworkLocation> networkLocations = copyLocations
                                                                            ? source.Locations.Values
                                                                            : targetSlice.Locations.Values;

            IMultiDimensionalArray values;

            if (copyLocations)
            {
                values = source.GetValues(new VariableValueFilter<DateTime>(source.Arguments[0], dateTime));
            }
            else
            {
                values = source.GetValues(new VariableValueFilter<DateTime>(source.Arguments[0], dateTime),
                                          new VariableValueFilter<INetworkLocation>(source.Arguments[1],
                                                                                    networkLocations));
            }

            var clonedValues = new ArrayList(values);
            var clonedLocations = networkLocations.ToArray();

            if (copyLocations)
            {
                targetSlice.Clear();
                targetSlice.Locations.Values.AddRange(clonedLocations);
                targetSlice.Components[0].NoDataValues = new ArrayList(source.Components[0].NoDataValues);
            }

            targetSlice.SetValues(clonedValues);
        }

        /// <summary>
        /// Extract a time slice for 1 time step out a time dependent network coverage
        /// </summary>
        /// <param name="source"></param>
        /// <param name="dateTime"></param>
        /// <returns></returns>
        public static INetworkCoverage ExtractTimeSlice(INetworkCoverage source, DateTime dateTime)
        {
            if (!source.Arguments[0].Values.Contains(dateTime))
            {
                throw new ArgumentException("ExtractTimeSlice: invalid time.",
                                            "dateTime");
            }
            INetworkCoverage slice = new NetworkCoverage(source.Name, false);
            //slice.Components[0].NoDataValues = new ArrayList(source.Components[0].NoDataValues);
            //slice.Locations.Values.AddRange(source.Arguments[1].Values);
            //var values = source.GetValues(new VariableValueFilter(source.Arguments[0], new [] {dateTime}));
            //slice.SetValues(values);

            // extract a time slice from the source and also copy the networklocations.
            ExtractTimeSlice(source, slice, dateTime, true);
            return slice;
        }

        /// <summary>
        /// Snaps the points to the network coverage, given the tolerance. Existing locations/values will be cleared only
        /// when there are new points mapped to that branch.
        /// </summary>
        /// <param name="pointValuePairs"></param>
        /// <param name="networkCoverage"></param>
        /// <param name="tolerance"></param>
        public static void SnapToCoverage(IEnumerable<Tuple<IPoint, double>> pointValuePairs,
                                          INetworkCoverage networkCoverage, double tolerance)
        {
            var tree = new Quadtree();
            networkCoverage.Network.Branches.ForEach(b => tree.Insert(b.Geometry.EnvelopeInternal, b));
            
            // match points to branch buffers
            var locations = new List<Tuple<INetworkLocation, double>>();
            foreach (var pointValue in pointValuePairs)
            {
                var envelope = pointValue.Item1.EnvelopeInternal;
                envelope.ExpandBy(tolerance);
                var branches = tree.Query(envelope).Cast<IBranch>();

                var location = MapPointToClosestBranch(pointValue.Item1, branches, tolerance);
                if (location != null)
                {
                    locations.Add(new Tuple<INetworkLocation, double>(location, pointValue.Item2));
                }
            }

            // remove values for all branches that have new values imported
            var branchesToClear = locations.Select(l => l.Item1.Branch).Distinct();
            branchesToClear.ForEach(b => NetworkHelper.ClearLocations(networkCoverage, b));

            // add new values/locations to coverage
            locations.ForEach(l => networkCoverage[l.Item1] = l.Item2);
        }

        private static INetworkLocation MapPointToClosestBranch(IPoint point, IEnumerable<IBranch> branches,
                                                                double tolerance)
        {
            IBranch targetBranch = null;
            var pointEnvelope = point.EnvelopeInternal;
            pointEnvelope.ExpandBy(tolerance);

            // this is cheaper than multiple calls to Distance(..) below
            var overlappingBranches = branches.Where(b => b.Geometry.EnvelopeInternal.Intersects(pointEnvelope));

            double distance = tolerance;
            foreach (var branch in overlappingBranches)
            {
                var d = branch.Geometry.Distance(point);
                if (!(d < distance)) continue;

                targetBranch = branch;
                distance = d;
            }

            if (targetBranch == null) return null;

            var line = targetBranch.Geometry as ILineString;
            if (line == null) return null;

            var coordinate = GeometryHelper.GetNearestPointAtLine(line, point.Coordinate, tolerance);
            var chainage = NetworkHelper.CalculationChainage(targetBranch,
                                                             GeometryHelper.Distance(
                                                                 (ILineString) targetBranch.Geometry, coordinate));

            return new NetworkLocation(targetBranch, chainage);
        }
    }
}