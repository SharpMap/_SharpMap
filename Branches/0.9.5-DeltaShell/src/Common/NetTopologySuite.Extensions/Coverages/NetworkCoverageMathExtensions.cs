using System;
using System.Linq;
using GeoAPI.Extensions.Coverages;

namespace NetTopologySuite.Extensions.Coverages
{
    /// <summary>
    /// Class can add and substract coverages. In the future this should be moved up to function
    /// Kept separate from network coverage to keep responsibilities minimal...SRP
    /// </summary>
    public static class NetworkCoverageMathExtensions
    {
        public static void Substract(this INetworkCoverage coverageA, INetworkCoverage coverageB)
        {
            Func<double, double, double> operation = (a, b) => a - b;

            Operate(coverageA, coverageB,operation);
        }

        public static void Add(this INetworkCoverage coverageA, INetworkCoverage coverageB)
        {
            Func<double, double, double> operation = (a, b) => a + b;

            Operate(coverageA, coverageB, operation);
        }

        private static void Operate(INetworkCoverage coverageA, INetworkCoverage coverageB, Func<double, double, double> operation)
        {
            ThrowIfInputIsInvalid(coverageA, coverageB);

            var allLocations =
                coverageA.Locations.Values.Concat(coverageB.Locations.Values).Distinct().OrderBy(loc => loc).ToList();

            var locationToValueMapping =
                allLocations.ToDictionary(location => location,
                                          location =>
                                          operation(coverageA.Evaluate(location), coverageB.Evaluate(location)));

            foreach (var location in allLocations)
            {
                coverageA[location] = locationToValueMapping[location];
            }
        }

        
        private static void ThrowIfInputIsInvalid(INetworkCoverage coverageA,INetworkCoverage coverageB)
        {
            if (coverageB.Network != coverageA.Network)
            {
                throw new InvalidOperationException("Network of coverage {0} does not math network of {1}," +
                                                    " math operations are not possible");
                
            }
        }
    }
}