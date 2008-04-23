using System;
using System.Collections.Generic;
using System.Text;

namespace SharpMap.Utilities
{
    public class MapHelper
    {
        public const double MilliMetresPerInch = 25.4;

        public static double CalculateScale(Map map)
        {
            return (map.PixelWidth * (double)map.Size.Width) / map.Envelope.Width;
        }
    }
}
