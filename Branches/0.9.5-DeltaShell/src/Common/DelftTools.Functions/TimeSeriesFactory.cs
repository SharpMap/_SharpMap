using System;
using System.Linq;
using DelftTools.Functions.Generic;
using DelftTools.Units;

namespace DelftTools.Functions
{
    /// <summary>
    /// TODO: this is not a right place for the hydro-specific time series, maybe in DelftTools.Hydro?
    /// </summary>
    public static class TimeSeriesFactory
    {
        public static TimeSeries CreateFlowTimeSeries()
        {
            var ts = new TimeSeries
                {
                    Components = {new Variable<double>("flow", new Unit("m3/s", "m3/s"))},
                    Name = "flow time series"
                };
            ts.Components[0].Attributes[FunctionAttributes.StandardName] = FunctionAttributes.StandardNames.WaterDischarge;
            var variable = ts.Arguments.First();
            variable.DefaultValue = new DateTime(2000, 1, 1);
            variable.InterpolationType = InterpolationType.Linear;
            variable.ExtrapolationType = ExtrapolationType.Constant;
            
            return ts;
        }

        public static TimeSeries CreateWaterLevelTimeSeries()
        {
            var ts = new TimeSeries
                {
                    Components = {new Variable<double>("level", new Unit("m AD", "m AD"))},
                    Name = "water level time series"
                };
            ts.Components[0].Attributes[FunctionAttributes.StandardName] = FunctionAttributes.StandardNames.WaterLevel;
            var variable = ts.Arguments.First();
            variable.DefaultValue = new DateTime(2000, 1, 1);
            variable.InterpolationType = InterpolationType.Linear;
            variable.ExtrapolationType = ExtrapolationType.Constant;

            return ts;
        }

    }
}