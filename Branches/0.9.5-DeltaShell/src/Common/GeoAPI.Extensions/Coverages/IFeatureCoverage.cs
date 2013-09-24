using System;
using DelftTools.Functions;
using DelftTools.Functions.Filters;
using DelftTools.Utils.Collections.Generic;
using GeoAPI.Extensions.Feature;

namespace GeoAPI.Extensions.Coverages
{
    public interface IFeatureCoverage : ICoverage
    {
        /// <summary>
        /// Evaluates value of coverage for a feature.
        /// </summary>
        /// <param name="feature">The feature to evealuate a value over.</param>
        /// <returns>The evaluated value for the given feature.</returns>
        T Evaluate<T>(IFeature feature);

        /// <summary>
        /// The feature argument in the coverage function. Feature coverage is actually a function,
        /// like any other coverage, defined as f = f(feature), so feature is used as an argument 
        /// of the coverage function.
        /// </summary>
        IVariable FeatureVariable { get; }

        /// <summary>
        /// The list of features that can be used in the FeatureVariable.
        /// After Features property is set - FeatureVariable still has no values defined.
        /// </summary>
        IEventedList<IFeature> Features { get; set; }

        /// <summary>
        /// Returns filtered feature coverage. See <see cref="IFunction.Filter"/> for more details.
        /// </summary>
        /// <param name="filters"></param>
        /// <returns></returns>
        //new IFeatureCoverage Filter(params IVariableFilter[] filters);

        IFeatureCoverage FilterAsFeatureCoverage(params IVariableFilter[] filters);

        /// <summary>
        /// Evaluates the value at the give time for the given feature. 
        /// NOTE: If the time is after the last time in the coverage, the last time will be used for the evaluation.
        /// </summary>
        /// <param name="dateTime"></param>
        /// <param name="feature"></param>
        /// <returns></returns>
        double Evaluate(DateTime dateTime, IFeature feature);

        IFunction GetTimeSeries(IFeature feature);
    }
}