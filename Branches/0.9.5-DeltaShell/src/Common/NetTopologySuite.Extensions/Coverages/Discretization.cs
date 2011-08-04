using System;
using DelftTools.Functions;
using DelftTools.Functions.Filters;
using GeoAPI.Extensions.Coverages;

namespace NetTopologySuite.Extensions.Coverages
{
    /// <summary>
    /// TODO: move it out (next to HydroNetwork?), it is very model-related and not GIS!
    /// </summary>
    public class Discretization : NetworkCoverage, IDiscretization
    {
        public Discretization()
        {
            SegmentGenerationMethod = SegmentGenerationMethod.SegmentBetweenLocationsFullyCovered;
            Name = "computational grid";
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

        //public override string ToString()
        //{
        //    return Name;
        //}
    }
}