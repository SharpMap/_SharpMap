using System;
using System.Linq;
using DelftTools.Functions.Generic;
using DelftTools.Units;

namespace DelftTools.Functions
{
    public class TimeSeriesFactory
    {
        public static TimeSeries CreateFlowTimeSeries()
        {
            TimeSeries ts = new TimeSeries()
            {
                Components = { new Variable<double>("flow", new Unit("m3/s", "m3/s")) },
                Name = "flow time series"
            };
            ((IVariable)ts.Arguments.First()).DefaultValue = new DateTime(2000, 1, 1);
            ((IVariable)ts.Arguments.First()).InterpolationType = InterpolationType.Linear;
            ((IVariable)ts.Arguments.First()).ExtrapolationType = ExtrapolationType.Constant;
            return ts;
        }

        public static TimeSeries CreateWaterLevelTimeSeries()
        {
            TimeSeries ts = new TimeSeries()
            {
                Components = { new Variable<double>("level", new Unit("m AD", "m AD")) },
                Name = "water level time series"
            };
            ((IVariable)ts.Arguments.First()).DefaultValue = new DateTime(2000, 1, 1);
            ((IVariable)ts.Arguments.First()).InterpolationType = InterpolationType.Linear;
            ((IVariable)ts.Arguments.First()).ExtrapolationType = ExtrapolationType.Constant;
            return ts;
        }

    }
}