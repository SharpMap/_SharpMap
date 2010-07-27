using System.Collections;
using System.Collections.Generic;
using System.IO;
using DelftTools.Functions;
using DelftTools.Functions.Filters;
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
        IVariable FeatureVariable { get; set; }

        /// <summary>
        /// The list of features that the FeatureVariable defines values over.
        /// 
        /// TODO: make it IEventedList or IEnumerable, using IList may create errorneous situations, when using IEnumerable also consider to remove setter.
        /// </summary>
        IList Features { get; set; }

        /// <summary>
        /// Returns filtered feature coverage. See <see cref="IFunction.Filter"/> for more details.
        /// </summary>
        /// <param name="filters"></param>
        /// <returns></returns>
        new IFeatureCoverage Filter(params IVariableFilter[] filters);
    }
}