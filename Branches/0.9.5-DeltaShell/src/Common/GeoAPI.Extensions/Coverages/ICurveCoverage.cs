using System;
using System.Collections.Generic;
using DelftTools.Functions;
using DelftTools.Functions.Generic;
using GeoAPI.Extensions.Feature;

namespace GeoAPI.Extensions.Coverages
{
    public interface ICurveCoverage : ICoverage
    {
        //INetwork Network { get; set; }
        IFeature Feature { get; set; }

        IVariable<IFeatureLocation> Locations { get; }

//-->        IVariable<INetworkSegment> Segments { get; }

        SegmentGenerationMethod SegmentGenerationMethod { get; set; }

        // TODO: remove it, use default value from component
        double DefaultValue { get; set; }

        /// <summary>
        /// Returns the value for the featureLocation. If there is no data available at the location
        /// the value will be interpolated.
        /// </summary>
        /// <param name="featureLocation"></param>
        /// <returns></returns>
        double Evaluate(IFeatureLocation featureLocation);

        double Evaluate(DateTime dateTime, IFeatureLocation featureLocation);

        IFunction GetTimeSeries(IFeatureLocation featureLocation);

        //INetworkCoverage AddTimeFilter(DateTime time);

        void SetLocations(IEnumerable<IFeatureLocation> locations);

        //void AddValuesForTime(IEnumerable values, DateTime time);


    }
}
