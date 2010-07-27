using System;
using System.Collections.Generic;
using System.Linq;
using DelftTools.Functions;
using DelftTools.Functions.Filters;
using DelftTools.Functions.Generic;
using DelftTools.Units;
using GeoAPI.Extensions.Coverages;
using GeoAPI.Extensions.Feature;
using GeoAPI.Geometries;
using GisSharpBlog.NetTopologySuite.Geometries;
using NetTopologySuite.Extensions.Geometries;

namespace NetTopologySuite.Extensions.Coverages
{
    public class CurveCoverage : Coverage, ICurveCoverage
    {
        private const double errorMargin = 1.0e-6;
        private const string DefaultNetworkCoverageName = "curve coverage";

        private SegmentGenerationMethod segmenationType;
        /// <summary>
        /// Tolerance used when evaluating values based on coordinate.
        /// todo move into interface
        /// </summary>
        public virtual double EvaluateTolerance { get; set; }

        public virtual IFeature Feature { get; set; }

        public CurveCoverage() : this(DefaultNetworkCoverageName, false)
        {
        }

        public CurveCoverage(string name, bool isTimeDependend) : this(name, isTimeDependend, name, "-")
        {
        }

        public CurveCoverage(string name, bool isTimeDependend, string outputName, string outputUnit)
        {
            base.Name = name;
            Components.Add(new Variable<double>(outputName){Unit = new Unit(outputUnit, outputUnit)});

            var locations = new Variable<IFeatureLocation>("feature_location");
            if (isTimeDependend)
            {
                Arguments.Add(new Variable<DateTime>("Time"));
                Time = (IVariable<DateTime>)Arguments[0];
            }

            Arguments.Add(locations);
            Geometry = new Point(0, 0);
            EvaluateTolerance = 1;
            SubscribeEvents();
        }

        private void SubscribeEvents()
        {
            //if (Locations != null)
            //{
            //    Locations.ValuesChanging += locations_ValuesChanging;
            //    Locations.ValuesChanged += locations_ValuesChanged;
            //    ((INotifyPropertyChanged)Locations).PropertyChanged += NetworkCoverage_PropertyChanged;
            //}
        }

        private void UnsubscribeEvents()
        {
            //if (Locations != null)
            //{
            //    Locations.ValuesChanging -= locations_ValuesChanging;
            //    Locations.ValuesChanged -= locations_ValuesChanged;
            //    ((INotifyPropertyChanged)Locations).PropertyChanged -= NetworkCoverage_PropertyChanged;
            //}
        }


        public override object Evaluate(ICoordinate coordinate)
        {
            var nearestLocation = GeometryHelper.GetNearestFeature(coordinate, Locations.Values.Cast<IFeature>(), EvaluateTolerance);
            if (nearestLocation == null)
            {
                return double.NaN; // use missing value
            }

            return this[nearestLocation];
        }

        public override T Evaluate<T>(ICoordinate coordinate)
        {
            throw new NotImplementedException();
        }

        public override T Evaluate<T>(double x, double y)
        {
            var coordinate = new Coordinate(x, y);
            return (T)Evaluate(coordinate);
        }

        public IVariable<IFeatureLocation> Locations
        {
            get
            {
                if (Arguments == null)
                {
                    return null;
                }

                var variable = Arguments.FirstOrDefault(a => a.ValueType == typeof(IFeatureLocation));

                if (variable == null)
                {
                    return null;
                }

                //--Initialize();

                return (IVariable<IFeatureLocation>)variable;
            }
        }

        public virtual SegmentGenerationMethod SegmentGenerationMethod
        {
            get { return segmenationType; }
            set
            {
                segmenationType = value;
                //--initialized = false;
            }
        }

        public virtual double DefaultValue
        {
            get { return (double)Components[0].DefaultValue; }
            set { Components[0].DefaultValue = value; }
        }


        public double Evaluate(IFeatureLocation featureLocation)
        {
            if ((Parent != null) && (IsTimeDependent))
            {
                //can't just convert to filters and handle by function since interpolation logic 
                //is defined at networkcoverage level
                //return Parent.Evaluate<double>(GetFiltersInParent(Filters));
                if (Filters.Count != 1 || !(Filters[0] is VariableValueFilter<DateTime>))
                {
                    throw new ArgumentException(
                        "Please specify time filter to retrieve value from time related network coverage");
                }
                var currentTime = ((VariableValueFilter<DateTime>)Filters[0]).Values[0];

                return ((ICurveCoverage)Parent).Evaluate(currentTime, featureLocation);
            }
            //we might have a local filter
            if (IsTimeDependent && Filters.Count == 1 && Filters[0] is VariableValueFilter<DateTime>)
            {
                var time = ((VariableValueFilter<DateTime>)Filters[0]).Values[0];
                return Evaluate(time, featureLocation);
            }
            if ((IsTimeDependent))
                throw new ArgumentException(
                    "Please specify time filter to retrieve value from time related network coverage");


            return GetInterpolatedValue(new VariableValueFilter<IFeatureLocation>(Locations, featureLocation));
        }

        public double Evaluate(DateTime dateTime, IFeatureLocation featureLocation)
        {
            if (!IsTimeDependent)
                throw new ArgumentException(
                    "Please do not specify time filter to retrieve value from time related network coverage");
            return GetInterpolatedValue(new VariableValueFilter<DateTime>(Time, dateTime),
                                        new VariableValueFilter<IFeatureLocation>(Locations, featureLocation));
        }

        public virtual double GetInterpolatedValue(params IVariableFilter[] filters)
        {
            //--Initialize();

            if (filters.Length != Arguments.Count || filters[0].Variable != Arguments[0] ||
                !(filters[0] is IVariableValueFilter))
            {
                throw new ArgumentOutOfRangeException("filters",
                                                      "Please specify networkCoverage using VariableValueFilter in filters.");
            }
            int timeFilterIndex = 0;
            int locationFilterIndex = 0;
            if (IsTimeDependent)
            {
                if (!filters[0].Variable.ValueType.Equals(typeof(DateTime)))
                {
                    timeFilterIndex = 1;
                }
                else
                {
                    locationFilterIndex = 1;
                }

                var dateTime = ((VariableValueFilter<DateTime>)(filters[timeFilterIndex])).Values[0];

                if (!Arguments[timeFilterIndex].Values.Contains(dateTime))
                {
                    throw new ArgumentException("Invalid time for network coverage", "filters");
                }
            }
            var networkLocation = ((VariableValueFilter<IFeatureLocation>)(filters[locationFilterIndex])).Values[0];
            if (Arguments[locationFilterIndex].Values.Contains(networkLocation))
            {
                // The network location is known; return the exact value
                return (double)GetValues(filters)[0];
            }
            // oops value not available; interpolation or default value
            var networkLocations = new List<IFeatureLocation>(Locations.Values);
            IList<IFeatureLocation> branchLocations =
                networkLocations.Where(bf => bf.Feature == networkLocation.Feature).OrderBy(bf => bf.Offset).ToList();
            // no netork location for branch; return default value
            if (0 == branchLocations.Count)
            {
                return DefaultValue;
            }
            // only 1 networklocation for branch, value of networklocation applies to entire branch
            if (1 == branchLocations.Count)
            {
                if (IsTimeDependent)
                {
                    return
                        (double)
                        GetValues(filters[timeFilterIndex],
                                  new VariableValueFilter<IFeatureLocation>(Arguments[locationFilterIndex],
                                                                            branchLocations[0]))[0];
                }
                return
                    (double)
                    GetValues(new VariableValueFilter<IFeatureLocation>(Arguments[locationFilterIndex],
                                                                        branchLocations[0]))[0];
            }
            // Multiple values available; interpolate
            var x = new double[branchLocations.Count + 2];
            x[x.Length - 1] = branchLocations[0].Feature.Geometry.Length;

            for (int i = 0; i < branchLocations.Count; i++)
            {
                x[i + 1] = branchLocations[i].Offset;
            }

            if (IsTimeDependent)
            {
                return Interpolate(branchLocations,
                                   ((VariableValueFilter<DateTime>)(filters[timeFilterIndex])).Values[0], x, /*y,*/
                                   networkLocation.Offset);
            }
            return Interpolate(branchLocations, x, networkLocation.Offset);
        }

        private double Interpolate(IList<IFeatureLocation> featureLocations, DateTime dateTime, double[] x,
            /*double []y,*/ double offset)
        {
            var yiminus1 = (double)this[dateTime, featureLocations[0]]; //0;
            var yi = (double)this[dateTime, featureLocations[featureLocations.Count - 1]]; //0;
            for (int i = 1; i < x.Length; i++)
            {
                if (offset.CompareTo(x[i]) > 0)
                    continue;
                if (i > 1)
                {
                    yiminus1 = (double)this[dateTime, featureLocations[i - 2]];
                }
                if (i < x.Length - 1)
                {
                    yi = (double)this[dateTime, featureLocations[i - 1]];
                }
                //y[x.Length - 1] = (double)this[branchLocations[branchLocations.Count - 1]];
                return (yiminus1 + (offset - x[i - 1]) * (yi - yiminus1) / (x[i] - x[i - 1]));
            }
            // or return y[y.Length - 1]?
            throw new ArgumentException(string.Format("NetworkCoverage ({0}): Offset ({1}) out of range; can not interpolate", Name, offset), "offset");
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="branchLocations"></param>
        /// BranchLocations in the branch
        /// <param name="x">
        /// Array with offsets of the branch location. An extra offset 0 is added in front, at the end an extra offset for
        /// the length of the branch.
        /// </param>
        /// <param name="offset">
        /// The offset for which the value has to interpolated
        /// </param>
        /// <returns>
        /// The interpolated value
        /// </returns>
        private double Interpolate(IList<IFeatureLocation> branchLocations, double[] x, double offset)
        {
            if (offset > (x.Max() + errorMargin))
            {
                throw new InvalidOperationException(
                    string.Format(
                        "NetworkCoverage ({0}): Offset ({1}) exceeds branch length ({2}). Interpolation impossible",
                        Name, offset, x.Max()));
            }
            var yiminus1 = (double)this[branchLocations[0]]; //0;
            var yi = (double)this[branchLocations[branchLocations.Count - 1]]; //0;
            for (int i = 1; i < x.Length; i++)
            {
                if (offset.CompareTo(x[i]) > 0)
                    continue;
                if (i > 1)
                {
                    yiminus1 = (double)this[branchLocations[i - 2]];
                }
                if (i < x.Length - 1)
                {
                    yi = (double)this[branchLocations[i - 1]];
                }
                return (yiminus1 + (offset - x[i - 1]) * (yi - yiminus1) / (x[i] - x[i - 1]));
            }
            // it is safe to return last value; check for out of branch at start of this method
            return (double)this[branchLocations[branchLocations.Count - 1]];
            //throw new ArgumentException(string.Format("NetworkCoverage ({0}): Offset ({1}) out of range; can not interpolate", Name, offset), "offset");
        }
        
        public IFunction GetTimeSeries(IFeatureLocation featureLocation)
        {
            throw new NotImplementedException();
        }

        public void SetLocations(IEnumerable<IFeatureLocation> locations)
        {
            SegmentGenerationMethod oldMethod = SegmentGenerationMethod;
            SegmentGenerationMethod = SegmentGenerationMethod.None;
            Locations.SetValues(locations);
            SegmentGenerationMethod = oldMethod;
        }
    }
}
