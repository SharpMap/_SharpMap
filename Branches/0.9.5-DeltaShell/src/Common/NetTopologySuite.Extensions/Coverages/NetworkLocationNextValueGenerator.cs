using System;
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
        /// Returns the next networklocation on the highest (in index) 
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
            INetworkLocation lastLoc = (INetworkLocation) coverage.Locations.Values.MaxValue;
            if (lastLoc.Offset == lastLoc.Branch.Length)
            {
                //find the next branch and return it at offset 0
                var lastlocBranchIndex = coverage.Network.Branches.IndexOf(lastLoc.Branch);
                if (lastlocBranchIndex == coverage.Network.Branches.Count -1)
                {
                    // if the last position is already in use try to find a free location
                    // GetNextValue should be GetNewValue; a NetworkLocation that can be added
                    // to the function should be returned.
                    double offset = lastLoc.Offset - 1.0e-4;

                    var locations = coverage.Locations.Values;
                    while (offset > 0)
                    {
                        if (!locations.Contains(new NetworkLocation(lastLoc.Branch, offset)))
                        {
                            return new NetworkLocation(lastLoc.Branch, offset);
                        }
                        offset -= 1.0e-4;
                    }
                    throw new ArgumentOutOfRangeException("All the branches are full!");
                }
                return new NetworkLocation(coverage.Network.Branches[lastlocBranchIndex + 1], 0);
            }
            return new NetworkLocation(lastLoc.Branch, lastLoc.Offset + 1);
        }
    }
}