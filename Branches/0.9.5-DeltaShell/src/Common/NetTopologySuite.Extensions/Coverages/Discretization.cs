using System;
using DelftTools.Functions;
using DelftTools.Functions.Filters;
using DelftTools.Units;
using GeoAPI.Extensions.Coverages;

namespace NetTopologySuite.Extensions.Coverages
{
    /// <summary>
    /// TODO: remove it!! It is just an instance of NetworkCoverage, configured in a specific way
    /// </summary>
    public class Discretization : NetworkCoverage, IDiscretization
    {
        public Discretization()
        {
            Locations.AllowSetInterpolationType = false;
            Locations.AllowSetExtrapolationType = false;
            SegmentGenerationMethod = SegmentGenerationMethod.SegmentBetweenLocationsFullyCovered;
            Name = "computational grid";
            Components[0].Name = "Grid point type";
            Components[0].Unit = new Unit("grid type", "0 / 1");
        }

        public void ToggleFixedPoint(INetworkLocation networkLocation)
        {
            var current = Evaluate(networkLocation);

            //new value is invert of current
            var newValue = current == 0?1:0;
            
            SetValues(new[] { newValue}, new VariableValueFilter<INetworkLocation>(Locations, networkLocation));
        }

        public bool IsFixedPoint(INetworkLocation location)
        {
            return Evaluate(location) == 1.0;
        }
    }
}