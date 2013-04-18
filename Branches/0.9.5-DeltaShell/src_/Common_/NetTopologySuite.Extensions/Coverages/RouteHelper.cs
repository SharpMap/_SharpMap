using System;
using System.Collections.Generic;
using System.Linq;
using DelftTools.Functions;
using DelftTools.Functions.Conversion;
using DelftTools.Functions.Filters;
using DelftTools.Functions.Tuples;
using DelftTools.Units;
using GeoAPI.Extensions.Coverages;
using GeoAPI.Extensions.Networks;

namespace NetTopologySuite.Extensions.Coverages
{
    public static class RouteHelper
    {
        
        /// <summary>
        /// Returns all locations in the route. Route cannot contain doubles for now because sideview should
        /// draw structures double etc. Would complicate more then the use-case would justify
        /// </summary>
        /// <param name="source"></param>
        /// <param name="route"></param>
        /// <returns></returns>
        public static IList<INetworkLocation> GetLocationsInRoute(INetworkCoverage source,INetworkCoverage route)
        {
            IList<INetworkLocation> locations = new List<INetworkLocation>();
            
            foreach (INetworkSegment segment in route.Segments.Values)
            {
                var locationsForSegment = GetLocationsForSegment(segment, source, true);
                foreach (var location in locationsForSegment)
                {
                    //add location if we didn't just add it..this can happens when segments go like 1-->3 3-->4 4-->5 
                    //don't want the start and endnodes causing doubles so should be like 1-->3-->4-->5
                    if (!location.Equals(locations.LastOrDefault()))
                    {
                        locations.Add(location);    
                    }
                    
                }
            }
            return locations;
        }

        /// <summary>
        /// Returns the NetworkLocations for segment in the source coverage. If the start and/or end location of the 
        /// segment are not in the source they are added.
        /// </summary>
        /// <param name="segment"></param>
        /// <param name="source"></param>
        /// <param name="addNewLocations">Should the end and start of the segment be returned?</param>
        /// <returns></returns>
        public static IEnumerable<INetworkLocation> GetLocationsForSegment(INetworkSegment segment, INetworkCoverage source, bool addNewLocations)
        {
            double min = Math.Min(segment.EndOffset, segment.Offset);
            double max = Math.Max(segment.EndOffset, segment.Offset);

            IList<INetworkLocation> locations = new List<INetworkLocation>();
            
            if (addNewLocations)
            {
                var startOffSet = segment.DirectionIsPositive ? segment.Offset : segment.EndOffset;
                locations.Add(new NetworkLocation(segment.Branch, startOffSet));
            }
            //need a cast here ..but we are sure since we just built it.
            foreach (var location in source.Locations.Values.Where(
                    nl =>
                    nl.Branch == segment.Branch && nl.Offset >= min && nl.Offset <= max))
            {
                if (!locations.Contains(location))
                {
                    locations.Add(location);
                }
            }

            if (addNewLocations)
            {
                var endOffSet = segment.DirectionIsPositive ? segment.EndOffset : segment.Offset;
                var location = new NetworkLocation(segment.Branch, endOffSet);
                if (!locations.Contains(location))
                {
                    locations.Add(location);
                }
            }
            
            return !segment.DirectionIsPositive ? locations.Reverse() : locations;
        }

        /// <summary>
        /// returns the segment where networkLocation is located.
        /// networkLocation does not have to be a networkLocation in route.Locations.
        /// </summary>
        /// <param name="route"></param>
        /// <param name="networkLocation"></param>
        /// <returns></returns>
        public static INetworkSegment GetSegmentForNetworkLocation(INetworkCoverage route, INetworkLocation networkLocation)
        {
            var segments = route.Segments.Values.ToArray();
            foreach (var segment in segments)
            {
                if (segment.Branch != networkLocation.Branch)
                {
                    continue;
                }
                if ((networkLocation.Offset > segment.Offset) && (networkLocation.Offset < segment.EndOffset))
                {
                    return segment;
                }
                // segment can be reversed in coverage
                if ((networkLocation.Offset < segment.Offset) && (networkLocation.Offset > segment.EndOffset))
                {
                    return segment;
                }
            }
            return null;
        }

        
        public static double GetRouteLength(INetworkCoverage coverage)
        {
            return coverage.Segments.Values.Sum(seg => seg.Length);
        }

        public static bool LocationsAreUniqueOnRoute(INetworkCoverage source, INetworkCoverage route)
        {
            //find all locations and checks for doubles.
            var locationsSet = new HashSet<INetworkLocation>();
            foreach (var segment in route.Segments.Values)
            {
                IEnumerable<INetworkLocation> locations = GetLocationsForSegment(segment, source, false);
                if (locationsSet.Overlaps(locations))
                    return false;
                foreach (var location in locations)
                {
                    locationsSet.Add(location);
                }
            }
            return true;
        }

        /// <summary>
        /// Creates a route with the given locations. Now route is stored in argument.
        /// This makes it impossible to have doubles. :( But makes drawing etc easy.
        /// </summary>
        /// <param name="locations"></param>
        /// <returns></returns>
        public static INetworkCoverage CreateRoute(params INetworkLocation[] locations)
        {
            var network = locations[0].Branch.Network;

            ThrowIfInputInvalid(locations, network);

            var route = new NetworkCoverage
                            {
                                Network = network,
                                SegmentGenerationMethod = SegmentGenerationMethod.RouteBetweenLocations
                            };
            
            route.Components[0].Unit = new Unit("meters", "m");
            route.Locations.AutoSort = false;
            route.SetLocations(locations);
            return route;
        }

        private static void ThrowIfInputInvalid(IEnumerable<INetworkLocation> locations, INetwork network)
        {
            //locations should be of the same network and network should be filled out
            if ((network == null) || (locations.Any(l=>l.Network != network)))
            {
                throw new InvalidOperationException("Invalid locations.");
            }
        }

        /// <summary>
        /// returns the offset of the branchfeature in the route. -1 if branchfeature is not in the route.
        /// </summary>
        /// <param name="route"></param>
        /// <param name="branchFeature"></param>
        /// <returns></returns>
        public static double GetRouteOffset(INetworkCoverage route, IBranchFeature branchFeature)
        {
            double offset = 0;
            foreach (var segment in route.Segments.Values)
            {
                if (branchFeature.Branch != segment.Branch)
                {
                    offset += segment.Length;
                }
                else
                {
                    if (segment.DirectionIsPositive)
                    {
                        if (branchFeature.Offset > segment.Offset + segment.Length)
                        {
                            offset += segment.Length;
                        }
                        else
                        {
                            offset += (branchFeature.Offset - segment.Offset);
                            return offset;
                        }
                    }
                    else
                    {
                        if (branchFeature.Offset > segment.Offset)// + segment.Length)
                        {
                            offset += segment.Length;
                        }
                        else
                        {
                            offset += (segment.Offset - branchFeature.Offset);
                            return offset;
                        }
                    }
                }
            }
            return -1;
        }

        
    }
}
