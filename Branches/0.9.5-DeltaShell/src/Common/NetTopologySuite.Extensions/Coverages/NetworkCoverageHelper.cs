using System;
using System.Collections;
using System.Linq;
using System.Collections.Generic;
using DelftTools.Functions;
using DelftTools.Functions.Filters;
using DelftTools.Functions.Generic;
using GeoAPI.Extensions.Coverages;
using GeoAPI.Extensions.Networks;
using GeoAPI.Geometries;
using GisSharpBlog.NetTopologySuite.LinearReferencing;
using log4net;
using NetTopologySuite.Extensions.Networks;

namespace NetTopologySuite.Extensions.Coverages
{
    /// <summary>
    /// Utilities for <see cref="NetworkCoverage"/>
    /// </summary>
    public class NetworkCoverageHelper
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(NetworkCoverageHelper));

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
            catch (Exception exception)
            {
                Log.ErrorFormat("Fatal error updating network coverage {0}; reason {1}", coverage.Name, exception.Message);
                // do not rethrow, it will crash ui
            }
        }

        private static void UpdateSegmentsNone(INetworkCoverage coverage)
        {
            coverage.Segments.Values.Clear();
        }

        /// <summary>
        /// Updates the segments for all branches with branchlocation following this scheme:
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
            //var coverageLocations = coverage.Locations.Values.OrderBy(l => l.Offset).OrderBy(l => l.Branch);
            //sorting is done in coverage locations..don't need to do it here. This supports for reversed segments.
            var coverageLocations = coverage.Locations.Values;

            IBranch branch = null;
            INetworkLocation previous = null;
            IList<INetworkLocation> branchLocations = new List<INetworkLocation>();
            foreach (var location in coverageLocations)
            {
                if ((null != previous) && (previous.Branch != location.Branch))
                {
                    IEnumerable<INetworkSegment> segments = UpdateSegmentsBranchSegmentBetweenLocations(fullyCover, branch, branchLocations);
                    foreach (var segment in segments)
                    {
                        coverage.Segments.Values.Add(segment);
                    }
                    branchLocations.Clear();
                }
                branchLocations.Add(location);
                branch = location.Branch;
                previous = location;
            }
            if (branchLocations.Count > 0)
            {
                IEnumerable<INetworkSegment> segments = UpdateSegmentsBranchSegmentBetweenLocations(fullyCover, branch, branchLocations);
                foreach (var segment in segments)
                {
                    coverage.Segments.Values.Add(segment);
                }
            }
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
        private static IEnumerable<INetworkSegment> UpdateSegmentsBranchSegmentBetweenLocations(bool fullyCover, 
            IBranch branch, IEnumerable<INetworkLocation> branchLocations)
        {
            var segments = new List<INetworkSegment>();

            var length = branch.Length;
            //note do we really want this?? should this not be handled in branch!
            /*if (branch.Geometry != null)
            {
                length = branch.Geometry.Length; // TODO: check if it is geometry-based, length can be in local units
            }*/

            // select all locations that have an offset within the branch
            IList<double> offsets = branchLocations.Where(l => l.Offset <= length).Select(l => l.Offset).ToList();

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
                    if (Math.Abs(offsets[0]) > 1.0e-6)
                    {
                        offsets.Add(0.0);
                    }
                    if (Math.Abs(offsets[offsets.Count - 1] - length) > 1.0e-6)
                    {
                        offsets.Add(length);
                    }
                }
            }

            var lengthIndexedLine = new LengthIndexedLine(branch.Geometry);

            for (int i=1; i< offsets.Count; i++)
            {
                var segment = new NetworkSegment
                                  {
                                      Branch = branch,
                                      Offset = offsets[i - 1],
                                      Length = Math.Abs(offsets[i] - offsets[i - 1]),
                                      DirectionIsPositive = offsets[i] >= offsets[i - 1],
                                      // thousand bombs and granates: ExtractLine will give either a new coordinate or 
                                      // a reference to an existing object
                                      Geometry = (IGeometry)lengthIndexedLine.ExtractLine(offsets[i - 1], offsets[i]).Clone()
                };
                segments.Add(segment);
                //coverage.Segments.Values.Add(segment);
            }
            return segments;
        }


        private static void UpdateSegmentsRouteBetweenLocations(INetworkCoverage coverage)
        {
            coverage.Segments.Values.Clear();

            var coverageLocations = coverage.Locations.Values;
            for (var i = 0; i < coverageLocations.Count - 1; i++)
            {
                var source = coverageLocations[i];
                var target = coverageLocations[i + 1];
                var segments = NetworkHelper.GetShortestPathBetweenBranchFeaturesAsNetworkSegments(coverage.Network, source, target);
                foreach (var segment in segments)
                {
                    if (segment.Offset < 0 || segment.EndOffset < 0)
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
                UpdateSegments(coverage, branch,allLocations);
            }
        }

        public static void UpdateSegments(INetworkCoverage coverage, IBranch branch, IMultiDimensionalArray<INetworkLocation> allLocations)
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
                    throw new ArgumentException(string.Format("Method {0} not supported", coverage.SegmentGenerationMethod), "coverage");
            }
            
            foreach (var s in segments)
            {
                // todo set branch and offset to NetworkSegmentAttributeAccessor?
                // assume number of location to be at least number of segments per branch 
                coverage.Segments.Values.Add(s);
            }
        }
        
        public static void ExtractTimeSlice(INetworkCoverage source, INetworkCoverage targetSlice /* bad name */, DateTime dateTime, 
            bool copyLocations)
        {
            if (!source.IsTimeDependent)
            {
                throw new ArgumentException("ExtractTimeSlice: source network coverage should be time dependent.",
                                            "source");
            }

            IEnumerable<INetworkLocation> networkLocations = null;
            if (copyLocations)
            {
                networkLocations = source.Arguments[1].GetValues().Cast<INetworkLocation>();
                targetSlice.Clear();
                targetSlice.Locations.Values.AddRange(networkLocations);   
            }
            else
            {
                networkLocations = targetSlice.Arguments[0].GetValues().Cast<INetworkLocation>();
            //    networkLocations = new ArrayList(targetSlice.Arguments[0].GetValues());
            }

            targetSlice.Components[0].NoDataValues = new ArrayList(source.Components[0].NoDataValues);

            IMultiDimensionalArray values;
            //var values = source.GetValues(new VariableValueFilter(source.Arguments[0], new[] { dateTime }));
            if (copyLocations)
            {
                values = source.GetValues(new VariableValueFilter<DateTime>(source.Arguments[0], dateTime));
            }
            else
            {
                values = source.GetValues(new VariableValueFilter<DateTime>(source.Arguments[0], dateTime),
                                              new VariableValueFilter<INetworkLocation>(source.Arguments[1], networkLocations));
            }
            targetSlice.SetValues(values);
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
    }
}