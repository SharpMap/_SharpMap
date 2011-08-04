using System;
using System.Collections;
using System.Collections.Generic;
using DelftTools.Functions;
using DelftTools.Functions.Generic;
using GeoAPI.Extensions.Networks;

namespace GeoAPI.Extensions.Coverages
{
    /// <summary>
    /// Network coverage is a function defined on the network in a linear way.
    /// TODO: introduce IDiscreteCurveCoverage (see ISO/OGC) and use it as a base interface for INetworkCoverage
    /// </summary>
    public interface INetworkCoverage : ICoverage
    {
        INetwork Network { get; set; }

        IVariable<INetworkLocation> Locations { get; }

        IVariable<INetworkSegment> Segments { get; }

        SegmentGenerationMethod SegmentGenerationMethod { get; set; }

        // TODO: remove it, use default value from component
        double DefaultValue { get; set; }

        

        /// <summary>
        /// Returns the value for the networkLocation. If there is no data available at the location
        /// the value will be interpolated.
        /// </summary>
        /// <param name="networkLocation"></param>
        /// <returns></returns>
        double Evaluate(INetworkLocation networkLocation);

        double Evaluate(DateTime dateTime, INetworkLocation networkLocation);

        IFunction GetTimeSeries(INetworkLocation networkLocation);

        INetworkCoverage AddTimeFilter(DateTime time);

        void SetLocations(IEnumerable<INetworkLocation> locations);

        void AddValuesForTime(IEnumerable values, DateTime time);

        double Evaluate(INetworkSegment segment);
    }
}