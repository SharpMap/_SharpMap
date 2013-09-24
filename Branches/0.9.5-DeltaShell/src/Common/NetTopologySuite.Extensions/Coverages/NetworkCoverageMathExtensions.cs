using System;
using System.Collections.Generic;
using System.Linq;
using DelftTools.Functions.Filters;
using DelftTools.Functions.Generic;
using DelftTools.Units;
using GeoAPI.Extensions.Coverages;
using GeoAPI.Extensions.Networks;

namespace NetTopologySuite.Extensions.Coverages
{
    /// <summary>
    /// Class can add and substract coverages. In the future this should be moved up to function
    /// Kept separate from network coverage to keep responsibilities minimal...SRP
    /// </summary>
    public static class NetworkCoverageMathExtensions
    {
        public static void Substract(this INetworkCoverage coverageA, INetworkCoverage coverageB)
        {
            Func<double, double, double> operation = (a, b) =>
                {
                    if (IsNoDataValue(coverageA, a))
                    {
                        if (IsNoDataValue(coverageB, b))
                        {
                            return a; // Note: a is NoDataValue!
                        }
                        return -b; // Nothing - b = -b
                    }
                    if (IsNoDataValue(coverageB, b))
                    {
                        return a; // a - Nothing = a
                    }

                    return a - b;
                };

            Operate(coverageA, coverageB,operation);
        }

        public static void Add(this INetworkCoverage coverageA, INetworkCoverage coverageB)
        {
            Func<double, double, double> operation = (a, b) =>
                {
                    if (IsNoDataValue(coverageA, a))
                    {
                        if (IsNoDataValue(coverageB, b))
                        {
                            return a; // Note: a is NoDataValue!
                        }
                        return b; // Nothing + b = b
                    }
                    if (IsNoDataValue(coverageB, b))
                    {
                        return a; // a + Nothing = a
                    }

                    return a + b;
                };

            Operate(coverageA, coverageB, operation);
        }

        /// <summary>
        /// Creates a new coverage, based on <paramref name="coverage"/>, with a <see cref="TimeSpan"/> value 
        /// for the duration where <paramref name="operation"/> resolves to true.
        /// </summary>
        /// <param name="coverage">Coverage to perform the measurement on.</param>
        /// <param name="referenceValue">A scalar reference value.</param>
        /// <param name="operation">A <see cref="Func{T1,T2,TResult}"/> where the first argument are values for <paramref name="coverage"/>,
        /// the second argument is <paramref name="referenceValue"/> and returns a bool value.</param>
        /// <param name="timeInterpolation">What type of interpolation to use for expressing the component value interpolated over time.</param>
        /// <param name="expressInDaysAsDouble">Optional argument for expressing the result as double instead of <see cref="TimeSpan"/>, using <see cref="TimeSpan.TotalDays"/></param>
        /// <returns>Returns a time independent <see cref="INetworkCoverage"/> with 1 TimeSpan (or Ticks as double) component.</returns>
        public static INetworkCoverage MeasureDurationWhereTrue(this INetworkCoverage coverage, double referenceValue, Func<double, double, bool> operation, InterpolationType timeInterpolation, bool expressInDaysAsDouble = false)
        {
            var outputCoverage = CopyNetworkCoverage(coverage);
            outputCoverage.IsTimeDependent = false;
            outputCoverage.Components.RemoveAt(0);
            if (expressInDaysAsDouble)
            {
                outputCoverage.Components.Add(new Variable<double>());
            }
            else
            {
                outputCoverage.Components.Add(new Variable<TimeSpan>());
            }
            outputCoverage.Components[0].Unit = new Unit("","");

            foreach (var location in coverage.Locations.Values)
            {
                var totalDuration = new TimeSpan(0);
                var operationWasTrue = false;
                var dateTimePrevious = new DateTime();
                var dateTimeTransitionToTrue = new DateTime();
                foreach (var dateTime in coverage.Time.Values)
                {
                    var coverageValue = coverage.Evaluate(dateTime, location);
                    if (IsNoDataValue(coverage, coverageValue)) continue;

                    if (operation(coverageValue, referenceValue))
                    {
                        if (!operationWasTrue)
                        {
                            operationWasTrue = true;
                            dateTimeTransitionToTrue = CalculateDateTimeTransition(coverage, referenceValue, location, dateTimePrevious, dateTime, timeInterpolation);
                        }
                    }
                    else
                    {
                        if (operationWasTrue)
                        {
                            operationWasTrue = false;
                            var transitionDateTimeToFalse = CalculateDateTimeTransition(coverage, referenceValue, location, dateTimePrevious, dateTime, timeInterpolation);

                            totalDuration += transitionDateTimeToFalse - dateTimeTransitionToTrue;
                        }
                    }

                    // Remember last with valid data value:
                    dateTimePrevious = dateTime;
                }

                if (operationWasTrue)
                {
                    totalDuration += coverage.Time.Values.Last() - dateTimeTransitionToTrue;
                }

                if (expressInDaysAsDouble)
                {
                    outputCoverage[location] = totalDuration.TotalDays;
                }
                else
                {
                    outputCoverage[location] = totalDuration;
                }
            }

            return outputCoverage;
        }
        
        public static void Operate(this INetworkCoverage coverageA, INetworkCoverage coverageB, Func<double, double, double> operation)
        {
            var allLocations = GetAllLocations(coverageA, coverageB);

            if (coverageA.IsTimeDependent && coverageB.IsTimeDependent)
            {
                ThrowIfCoverageTimeValuesNotEqual(coverageA, coverageB);

                foreach(var time in coverageA.Time.Values)
                {
                    OperateOneTimestep(coverageA.AddTimeFilter(time), coverageB.AddTimeFilter(time), allLocations, operation);
                }
                return;
            }
            if (coverageA.IsTimeDependent)
            {
                foreach (var time in coverageA.Time.Values)
                {
                    OperateOneTimestep(coverageA.AddTimeFilter(time), coverageB, allLocations, operation);
                }
                return;
            }
            if (coverageB.IsTimeDependent)
            {
                foreach (var time in coverageB.Time.Values)
                {
                    OperateOneTimestep(coverageA, coverageB.AddTimeFilter(time), allLocations, operation);
                }
                return;
            }
            OperateOneTimestep(coverageA, coverageB, allLocations, operation);
        }

        public static void Operate(this INetworkCoverage coverage, double referenceValue, Func<double, double, double> operation)
        {
            PerformOperation(coverage, referenceValue, operation);
        }

        public static INetworkCoverage OperateToNewCoverage<T>(this INetworkCoverage coverage, IEnumerable<INetworkCoverage> otherCoverages, Func<IEnumerable<double>, T> operation)
        {
            var outputCoverage = CreateOutputCoverage<T>(coverage);

            PerformOperation(coverage, otherCoverages, operation, outputCoverage);

            return outputCoverage;
        }

        /// <summary>
        /// Creates a new coverage, based on <paramref name="coverage"/>, with a <see cref="bool"/> value
        /// defined by <paramref name="operation"/>.
        /// </summary>
        /// <param name="coverage">Coverage to perform the operation on.</param>
        /// <param name="referenceValue">A scalar reference value.</param>
        /// <param name="operation">A <see cref="Func{T1,T2,TResult}"/> where the first argument are values from <paramref name="coverage"/>,
        /// the second argument is <paramref name="referenceValue"/> and returns a bool value.</param>
        /// <returns>Returns a <see cref="INetworkCoverage"/>, with the same time dependency as <paramref name="coverage"/>, 
        /// that has 1 bool component.</returns>
        public static INetworkCoverage OperateToNewCoverage<T>(this INetworkCoverage coverage, double referenceValue, Func<double, double, T> operation)
        {
            var outputCoverage = CreateOutputCoverage<T>(coverage);

            PerformOperation(coverage, referenceValue, operation, outputCoverage);

            return outputCoverage;
        }

        private static INetworkCoverage CreateOutputCoverage<T>(INetworkCoverage coverage)
        {
            var outputCoverage = CopyNetworkCoverage(coverage);
            outputCoverage.Components.RemoveAt(0);
            outputCoverage.Components.Add(new Variable<T>());
            outputCoverage.Components[0].Unit = new Unit("", "");

            return outputCoverage;
        }

        private static void PerformOperation<T>(INetworkCoverage coverage, double referenceValue, Func<double, double, T> operation,
                                                INetworkCoverage outputCoverage = null)
        {
            var allLocations = coverage.Locations.Values.Distinct().OrderBy(loc => loc).ToList();

            if (coverage.IsTimeDependent)
            {
                foreach (var time in coverage.Time.Values)
                {
                    if (outputCoverage != null)
                    {
                        OperateOneTimeStep(coverage.AddTimeFilter(time), referenceValue, allLocations, operation,
                                       outputCoverage.AddTimeFilter(time));
                    }
                    else
                    {
                        OperateOneTimeStep(coverage.AddTimeFilter(time), referenceValue, allLocations, operation);
                    }
                }
            }
            else
            {
                OperateOneTimeStep(coverage, referenceValue, allLocations, operation, outputCoverage);
            }
        }

        private static void PerformOperation<T>(INetworkCoverage coverage, IEnumerable<INetworkCoverage> otherCoverages,
                                                Func<IEnumerable<double>, T> operation, INetworkCoverage outputCoverage = null)
        {
            var allLocations = GetAllLocations(coverage, otherCoverages);

            if (coverage.IsTimeDependent)
            {
                ThrowIfCoverageTimeValuesNotEqual(coverage, otherCoverages);

                foreach (var time in coverage.Time.Values)
                {
                    foreach (var networkCoverage in otherCoverages)
                    {
                        networkCoverage.AddTimeFilter(time);
                    }
                    if (outputCoverage != null)
                    {
                        OperateOneTimestep(coverage.AddTimeFilter(time), otherCoverages, allLocations, operation, outputCoverage.AddTimeFilter(time));
                    }
                    else
                    {
                        OperateOneTimestep(coverage.AddTimeFilter(time), otherCoverages, allLocations, operation);
                    }
                }
            }
            else
            {
                OperateOneTimestep(coverage, otherCoverages, allLocations, operation, outputCoverage);
            }
        }

        private static void OperateOneTimeStep<T>(INetworkCoverage coverage, double referenceValue,
                                                    IEnumerable<INetworkLocation> allLocations, Func<double, double, T> operation,
                                                    INetworkCoverage outputCoverage = null)
        {
            IDictionary<INetworkLocation, T> locationToValueMapping = new Dictionary<INetworkLocation, T>();

            foreach (var loc in allLocations)
            {
                //in case of different networks the actual location is retrieved using coordinates:
                var locationInCoverageA = GetLocationInCoverage(coverage, loc);

                if (locationInCoverageA != null)
                {
                    var valueForA = coverage.Evaluate(locationInCoverageA);

                    var value = operation(valueForA, referenceValue);

                    locationToValueMapping.Add(locationInCoverageA, value);
                }
            }

            //apply values in a second loop to prevent evaluation being affected!
            SetResults(locationToValueMapping, outputCoverage ?? coverage);
        }

        private static void OperateOneTimestep(INetworkCoverage coverageA, INetworkCoverage coverageB, 
                                                IEnumerable<INetworkLocation> allLocations,
                                                Func<double, double, double> operation)
        {
            IDictionary<INetworkLocation, double> locationToValueMapping = new Dictionary<INetworkLocation, double>();

            foreach(var loc in allLocations)
            {
                //in case of different networks the actual location is retrieved using coordinates:
                var locationInCoverageA = GetLocationInCoverage(coverageA, loc); 

                if (locationInCoverageA != null)
                {
                    var valueForA = coverageA.Evaluate(locationInCoverageA);

                    var locationInCoverageB = GetLocationInCoverage(coverageB, loc);
                    var valueForB = locationInCoverageB != null ? coverageB.Evaluate(locationInCoverageB) : 0.0;

                    var value = operation(valueForA, valueForB);
                    
                    locationToValueMapping.Add(locationInCoverageA, value);
                }
            }

            //apply values in a second loop to prevent evaluation being affected!
            SetResults(locationToValueMapping, coverageA);
        }

        private static void OperateOneTimestep<T>(INetworkCoverage coverage, IEnumerable<INetworkCoverage> otherCoverages,
                                                    IEnumerable<INetworkLocation> allLocations, Func<IEnumerable<double>, T> operation,
                                                    INetworkCoverage outputCoverage = null)
        {
            IDictionary<INetworkLocation, T> locationToValueMapping = new Dictionary<INetworkLocation, T>();

            foreach (var loc in allLocations)
            {
                //in case of different networks the actual location is retrieved using coordinates:
                var locationInCoverageA = GetLocationInCoverage(coverage, loc);

                if(locationInCoverageA != null)
                {
                    var valuesAtLocation = new List<double>();
                    var valueForA = coverage.Evaluate(locationInCoverageA);
                    valuesAtLocation.Add(valueForA);

                    foreach (var networkCoverage in otherCoverages)
                    {
                        var locationInOtherCoverage = GetLocationInCoverage(networkCoverage, loc);
                        var valueForOther = locationInOtherCoverage != null
                                                ? networkCoverage.Evaluate(locationInOtherCoverage)
                                                : double.NaN;
                        valuesAtLocation.Add(valueForOther);
                    }

                    var value = operation(valuesAtLocation);

                    locationToValueMapping.Add(locationInCoverageA, value);
                }
            }

            //apply values in a second loop to prevent evaluation being affected!
            SetResults(locationToValueMapping, outputCoverage ?? coverage);
        }

        private static INetworkLocation GetLocationInCoverage(INetworkCoverage coverage, INetworkLocation location)
        {
            if (CheckIfNetworksMismatch(coverage.Network, location.Network)) //defined on different network
            {
                var coordinate = location.Geometry.Coordinate;
                var loc = coverage.GetLocationOnBranch(coordinate.X, coordinate.Y);
                return loc;
            }
            return location;
        }

        private static void SetResults<T>(IDictionary<INetworkLocation, T> locationToValueMapping, INetworkCoverage coverage)
        {
            foreach (var loc in locationToValueMapping.Keys)
            {
                var value = locationToValueMapping[loc];
                coverage.SetValues(new[] { value }, new VariableValueFilter<INetworkLocation>(coverage.Locations, loc));
            }
        }

        private static bool CheckIfNetworksMismatch(INetwork network1, INetwork network2)
        {
            return !ReferenceEquals(network1, network2); //if network1 empty, assume no mismatch
        }

        private static bool CheckIfNetworksMismatch(INetwork network1, IEnumerable<INetwork> otherNetworks)
        {
            // Return true if any in 'otherNetworks' does not have same network
            return otherNetworks.Any(n => CheckIfNetworksMismatch(network1, n));
        }

        private static IEnumerable<INetworkLocation> GetAllLocations(INetworkCoverage coverageA, INetworkCoverage coverageB)
        {
            if (CheckIfNetworksMismatch(coverageA.Network, coverageB.Network))
            {
                return coverageA.Locations.Values.ToList();
            }
            return coverageA.Locations.Values.Concat(coverageB.Locations.Values).Distinct().OrderBy(loc => loc).ToList();
        }

        private static IEnumerable<INetworkLocation> GetAllLocations(INetworkCoverage coverage, IEnumerable<INetworkCoverage> otherCoverages)
        {
            if (CheckIfNetworksMismatch(coverage.Network, otherCoverages.Select(c => c.Network)))
            {
                return coverage.Locations.Values.ToList();
            }
            return coverage.Locations.Values.Concat(otherCoverages.SelectMany(c => c.Locations.Values)).Distinct().OrderBy(loc => loc).ToList();
        }

        private static void ThrowIfCoverageTimeValuesNotEqual(INetworkCoverage coverageA, INetworkCoverage coverageB)
        {
            if (!coverageA.IsTimeDependent)
            {
                throw new ArgumentException("Coverage is not timedependent: " + coverageA);
            }

            if (!coverageB.IsTimeDependent)
            {
                throw new ArgumentException("Coverage is not timedependent: " + coverageB);
            }

            if (coverageA.Time.Values.Count != coverageB.Time.Values.Count)
            {
                throw new ArgumentException("Number of time values does not match for coverages");
            }

            //todo: more checking
        }

        private static void ThrowIfCoverageTimeValuesNotEqual(INetworkCoverage coverage, IEnumerable<INetworkCoverage> otherCoverages)
        {
            foreach (var networkCoverage in otherCoverages)
            {
                ThrowIfCoverageTimeValuesNotEqual(coverage, networkCoverage);
            }
        }

        private static INetworkCoverage CopyNetworkCoverage(INetworkCoverage originalCoverage)
        {
            //clone works strange for NetCdf, for now just create a manual copy

            var result = new NetworkCoverage("", originalCoverage.IsTimeDependent) { Network = originalCoverage.Network };

            if (originalCoverage.IsTimeDependent)
            {
                result.Time.SetValues(originalCoverage.Time.Values);
            }

            result.Locations.SetValues(originalCoverage.Locations.Values.Select(loc => (INetworkLocation)loc.Clone()));
            result.Components[0].SetValues(originalCoverage.Components[0].Values);

            if (result.Components.Count > 1)
            {
                throw new NotImplementedException("Support for coverages with multiple components is not implemented");
            }

            return result;
        }

        private static DateTime CalculateDateTimeTransition(INetworkCoverage coverage, double referenceValue, INetworkLocation location, DateTime dateTimePrevious, DateTime dateTimeCurrent, InterpolationType timeInterpolation)
        {
            var dateTimeIndex = coverage.Time.Values.IndexOf(dateTimeCurrent);
            if (dateTimeIndex == 0)
            {
                return dateTimeCurrent;
            }

            TimeSpan timeSpan;
            switch (timeInterpolation)
            {
                case InterpolationType.None:
                    return dateTimeCurrent;
                case InterpolationType.Constant:
                    // Constant interpolation: value changes in the middle of 'dateTime' and 'dateTimePrevious'
                    timeSpan = dateTimeCurrent - dateTimePrevious;
                    return dateTimePrevious + new TimeSpan(timeSpan.Ticks / 2);
                case InterpolationType.Linear:
                    var currentValue = (double)coverage[dateTimeCurrent, location];
                    var previousValue = (double)coverage[dateTimePrevious, location];
                    timeSpan = dateTimeCurrent - dateTimePrevious;

                    return dateTimePrevious + new TimeSpan(Convert.ToInt64(timeSpan.Ticks * Math.Abs(previousValue - referenceValue) / Math.Abs(currentValue - previousValue)));
                default:
                    throw new NotImplementedException(String.Format("Interpolation method {0} not supported", coverage.Time.InterpolationType));
            }
        }

        private static bool IsNoDataValue(INetworkCoverage coverageA, double a)
        {
            return coverageA.Components[0].NoDataValues != null &&
                   coverageA.Components[0].NoDataValues.Contains(a);
        }
    }
}