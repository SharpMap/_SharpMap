using DelftTools.Functions.Generic;

namespace DelftTools.Functions
{
    public static class TimeSeriesExtensions
    {
        public static bool IsFlowSeries(this TimeSeries t)
        {
            return (t.Components.Count > 0 && t.Components[0] is Variable<double> && t.Components[0].Unit.Name.Equals("m3/s") && t.Components[0].Unit.Symbol.Equals("m3/s"));
        }

        public static bool IsWaterLevelSeries(this TimeSeries t)
        {
            return (t.Components.Count > 0 && t.Components[0] is Variable<double> && t.Components[0].Unit.Name.Equals("m AD") && t.Components[0].Unit.Symbol.Equals("m AD"));
        }
    }
}