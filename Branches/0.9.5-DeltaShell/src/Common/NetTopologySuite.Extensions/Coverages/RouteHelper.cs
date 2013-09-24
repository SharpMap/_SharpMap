using System;
using System.Collections.Generic;
using System.Linq;
using DelftTools.Units;
using GeoAPI.Extensions.Coverages;
using GeoAPI.Extensions.Networks;
using NetTopologySuite.Extensions.Networks;

namespace NetTopologySuite.Extensions.Coverages
{
    public static class RouteHelper
    {
        private static readonly double ErrorMargin = BranchFeature.Epsilon;
        /// <summary>
        /// Returns all locations in the route. Route cannot contain doubles for now because side view should
        /// draw structures double etc. Would complicate more then the use-case would justify
        /// </summary>
        /// <param name="source"></param>
        /// <param name="route"></param>
        /// <returns></returns>
        public static IList<INetworkLocation> GetLocationsInRoute(INetworkCoverage source, Route route)
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
            double min = Math.Min(segment.EndChainage, segment.Chainage);
            double max = Math.Max(segment.EndChainage, segment.Chainage);

            IList<INetworkLocation> locations = new List<INetworkLocation>();
            
            if (addNewLocations)
            {
                var startChainage = segment.DirectionIsPositive ? segment.Chainage : segment.EndChainage;
                locations.Add(new NetworkLocation(segment.Branch, startChainage));
            }
            //need a cast here ..but we are sure since we just built it.
            foreach (var location in source.Locations.Values.Where(
                    nl =>
                    nl.Branch == segment.Branch && nl.Chainage >= min && nl.Chainage <= max))
            {
                if (!locations.Contains(location))
                {
                    locations.Add(location);
                }
            }

            if (addNewLocations)
            {
                var endChainage = segment.DirectionIsPositive ? segment.EndChainage : segment.Chainage;
                var location = new NetworkLocation(segment.Branch, endChainage);
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
        public static INetworkSegment GetSegmentForNetworkLocation(Route route, INetworkLocation networkLocation)
        {
            var segments = route.Segments.Values.ToArray();
            foreach (var segment in segments)
            {
                if (segment.Branch != networkLocation.Branch)
                {
                    continue;
                }
                if ((networkLocation.Chainage > segment.Chainage) && (networkLocation.Chainage < segment.EndChainage))
                {
                    return segment;
                }
                // segment can be reversed in coverage
                if ((networkLocation.Chainage < segment.Chainage) && (networkLocation.Chainage > segment.EndChainage))
                {
                    return segment;
                }
            }
            return null;
        }

        //this code gives the geometry length, not the physical length!!
        public static double GetRouteLength(Route coverage)
        {
            return coverage.Segments.Values.Sum(seg => seg.Length);
        }

        public static bool LocationsAreUniqueOnRoute(INetworkCoverage source, Route route)
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
        public static Route CreateRoute(params INetworkLocation[] locations)
        {
            var network = locations[0].Branch.Network;

            ThrowIfInputInvalid(locations, network);

            var route = new Route
                            {
                                Network = network,
                            };
            route.Components[0].Unit = new Unit("meters", "m");
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

        public static bool IsBranchFeatureInRoute(Route route, IBranchFeature branchFeature)
        {
            return GetRouteChainage(route, branchFeature) >= 0;
        }

        /// <summary>
        /// returns the chainage of the branchfeature in the route. -1 if branchfeature is not in the route.
        /// </summary>
        /// <param name="route"></param>
        /// <param name="branchFeature"></param>
        /// <returns></returns>
        private static double GetRouteChainageInternal(Route route, IBranchFeature branchFeature)
        {
            double chainage = 0;
            foreach (var segment in route.Segments.Values)
            {
                if (branchFeature.Branch != segment.Branch)
                {
                    chainage += segment.Length;
                }
                else
                {
                    if (segment.DirectionIsPositive)
                    {
                        if (branchFeature.Chainage > segment.Chainage + segment.Length + ErrorMargin)
                        {
                            chainage += segment.Length;
                        }
                        else
                        {
                            chainage += (branchFeature.Chainage - segment.Chainage);
                            return chainage;
                        }
                    }
                    else
                    {
                        if (branchFeature.Chainage > segment.Chainage)// + segment.Length)
                        {
                            chainage += segment.Length;
                        }
                        else
                        {
                            chainage += (segment.Chainage - branchFeature.Chainage);
                            return chainage;
                        }
                    }
                }
            }
            return -1;
        }
        
        public static double GetRouteChainage(Route route, IBranchFeature branchFeature)
        {
            var chainage = GetRouteChainageInternal(route, branchFeature);
            if (chainage >= 0)
            {
                if (chainage > GetRouteLength(route))
                {
                    chainage = -1;
                }
            }
            else
            {
                chainage = -1;
            }
            
            return chainage;
        }

        private static bool IsWithinSegment(INetworkSegment segment,INetworkLocation location)
        {
            if (segment.Branch != location.Branch)
                return false;

            var begin = segment.Chainage;
            var end = segment.EndChainage;

            if (!segment.DirectionIsPositive)
            {
                begin = segment.EndChainage;
                end = segment.Chainage;
            }

            //add some rounding margin
            begin += 0.000001;
            end -= 0.000001;

            if (begin < location.Chainage && location.Chainage < end)
                return true;

            return false;
        }

        public static bool RouteContainLoops(Route networkRoute)
        {
            //see if there is any location which lies *within* (so excluding start & end) a segment
            return networkRoute.Locations.Values.Any(location => 
                networkRoute.Segments.Values.Any(segment => IsWithinSegment(segment,location)));
        }

        public static bool IsDisconnected(Route networkRoute)
        {
            for(int i = 0; i < networkRoute.Locations.Values.Count-1; i++)
            {
                var segments = NetworkHelper.GetShortestPathBetweenBranchFeaturesAsNetworkSegments(networkRoute.Network,
                                                                                    networkRoute.Locations.Values[i],
                                                                                    networkRoute.Locations.Values[i + 1]);

                if (segments.Count == 0)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
