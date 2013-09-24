using DelftTools.Functions.Generic;
using GeoAPI.Extensions.Coverages;

namespace NetTopologySuite.Extensions.Coverages
{
    public class NetworkLocationNextValueGenerator : NextValueGenerator<INetworkLocation>
    {
        private readonly INetworkCoverage coverage;

        public NetworkLocationNextValueGenerator(INetworkCoverage coverage)
        {
            this.coverage = coverage;
        }
        /// <summary>
        /// Returns the next network location on the highest (in index) 
        /// branch which has network locations and adds 1.0 to offset.
        /// </summary>
        /// <returns></returns>
        public override INetworkLocation GetNextValue()
        {
            if (coverage.Locations.Values.Count == 0)
            {
                return new NetworkLocation(coverage.Network.Branches[0], 0);
            }

            //get 'next' location
            var lastLoc = (INetworkLocation) coverage.Locations.Values.MaxValue;
            return new NetworkLocation(lastLoc.Branch, lastLoc.Chainage + 1);
        }
    }
}