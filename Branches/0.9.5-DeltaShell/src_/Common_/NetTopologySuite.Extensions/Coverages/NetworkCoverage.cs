using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using DelftTools.Functions;
using DelftTools.Functions.Filters;
using DelftTools.Functions.Generic;
using DelftTools.Units;
using DelftTools.Utils.Aop.NotifyPropertyChanged;
using DelftTools.Utils.Collections;
using DelftTools.Utils.Collections.Generic;
using GeoAPI.Extensions.Coverages;
using GeoAPI.Extensions.Feature;
using GeoAPI.Extensions.Networks;
using GeoAPI.Geometries;
using GisSharpBlog.NetTopologySuite.Geometries;
using NetTopologySuite.Extensions.Features;
using NetTopologySuite.Extensions.Geometries;
using NetTopologySuite.Extensions.Networks;

namespace NetTopologySuite.Extensions.Coverages
{
    [NotifyPropertyChanged]
    public class NetworkCoverage : Coverage, INetworkCoverage
    {
        private const double errorMargin = 1.0e-6;
        private const string DefaultNetworkCoverageName = "network coverage";

        private bool initialized; // lazy initialization of attributes

        /// <summary>
        /// Called afterload
        /// </summary>
        private bool initializing;

        //no bubbling here since there is no parent/child relationship. performance issue
        [NoBubbling]
        private INetwork network;

        private SegmentGenerationMethod segmenationType;
        
        private readonly Variable<INetworkSegment> segments;
        private bool segmentsInitialized;
        
        //store to know the previous length of branch. If branch changes the locations can be scaled along
        private IDictionary<IBranch,double> branchLengths;

        public NetworkCoverage() : this(DefaultNetworkCoverageName, false)
        {
        }

        public NetworkCoverage(string name, bool isTimeDependend) : this(name, isTimeDependend, name, "-")
        {
        }

        public NetworkCoverage(string name, bool isTimeDependend, string outputName, string outputUnit)
        {
            base.Name = name;
            Components.Add(new Variable<double>(outputName){Unit = new Unit(outputUnit, outputUnit)});

            var locations = new Variable<INetworkLocation>("network_location");
            if (isTimeDependend)
            {
                Arguments.Add(new Variable<DateTime>("Time"));
                Time = (IVariable<DateTime>)Arguments[0];
            }

            Arguments.Add(locations);
            segments = new Variable<INetworkSegment>("Segment") {AutoSort = false};
            Geometry = new Point(0, 0);
            EvaluateTolerance = 1;
            branchLengths = new Dictionary<IBranch, double>();
            SubscribeEvents();
        }

        public override IGeometry Geometry
        {
            get
            {
                Initialize();
                return base.Geometry;
            }
            set
            {
                initialized = false;
                base.Geometry = value;
            }
        }

        public override IEventedList<IVariable> Arguments
        {
            get { return base.Arguments; }
            set
            {
                UnsubscribeEvents();
                initialized = false;
                base.Arguments = value;
                SubscribeEvents();
            }
        }

        public override IMultiDimensionalArray GetValues(params IVariableFilter[] filters)
        {
            Initialize();
            return base.GetValues(filters);
        }

        public override IMultiDimensionalArray<T> GetValues<T>(params IVariableFilter[] filters) 
        {
            Initialize();
            return base.GetValues<T>(filters);
        }

        public virtual INetwork Network
        {
            get
            {
                if (Parent != null)
                {
                    return ((INetworkCoverage)Parent).Network;
                }
                return network;
            }
            set
            {
                if (Parent != null)
                {
                    ((INetworkCoverage) Parent).Network = value;
                    return;
                }
                

                if (null != network && network is INotifyPropertyChanged)
                {
                    ((INotifyPropertyChanged) network).PropertyChanged -= Network_PropertyChanged;
                }
                if (null != network)
                {
                    network.Branches.CollectionChanged -= Branches_CollectionChanged;
                }

                network = value;
                StoreBranchLengths();

                if (null != network && network is INotifyPropertyChanged)
                {
                    ((INotifyPropertyChanged) network).PropertyChanged += Network_PropertyChanged;
                }
                if (null != network)
                {
                    network.Branches.CollectionChanged += Branches_CollectionChanged;
                    //NetworkCoverageHelper.UpdateSegments(this);
                }

                initialized = false;
            }
        }

        private void StoreBranchLengths()
        {
            branchLengths.Clear();
            if (network == null)
            {
                return;
            }
            foreach (var branch in network.Branches)
            {
                branchLengths[branch] = branch.Length;
            }
        }

        void Branches_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (network.Branches == sender)
            {
                segmentsInitialized = false;
                StoreBranchLengths();
                //NetworkCoverageHelper.UpdateSegments(this);    
            }
        }

        public virtual IVariable<INetworkLocation> Locations
        {
            get
            {
                if (Arguments == null)
                {
                    return null;
                }

                var variable = Arguments.FirstOrDefault(a => a.ValueType == typeof (INetworkLocation));

                if (variable == null)
                {
                    return null;
                }
                
                Initialize();

                return (IVariable<INetworkLocation>)variable;
            }
        }

        public virtual IVariable<INetworkSegment> Segments
        {
            get
            {
                //if we are time filtered we use the segments of the parent
                if ((Parent != null) && Filters.All(f => f.Variable == ((INetworkCoverage)Parent).Time))
                {
                    return ((INetworkCoverage)Parent).Segments;
                }

                if (!segmentsInitialized)
                {
                    segmentsInitialized = true;
                    UpdateSegments();
                }
                //Initialize();

                return segments;
            }
        }

        public virtual SegmentGenerationMethod SegmentGenerationMethod
        {
            get { return segmenationType; }
            set
            {
                segmenationType = value;
                segmentsInitialized = false;
                initialized = false;
            }
        }
        
        public virtual double DefaultValue
        {
            get { return (double) Components[0].DefaultValue; }
            set { Components[0].DefaultValue = value; }
        }

        public override T Evaluate<T>(ICoordinate coordinate)
        {
            throw new NotImplementedException();
        }

        public override object Evaluate(ICoordinate coordinate)
        {
            var nearestLocation = GeometryHelper.GetNearestFeature(coordinate, Locations.Values.Cast<IFeature>(), EvaluateTolerance);
            if(nearestLocation == null)
            {
                return double.NaN; // use missing value
            }

            return this[nearestLocation];
        }


        /// <summary>
        /// Tolerance used when evaluating values based on coordinate.
        /// </summary>
        public virtual double EvaluateTolerance { get; set; }

        public override T Evaluate<T>(double x, double y)
        {
            // find location on nearest branch
            var point = new Point(x, y);
            var branch = NetworkHelper.GetNearestBranch(network.Branches, point, EvaluateTolerance);

            var coordinateOnBranch = GeometryHelper.GetNearestPointAtLine((ILineString) branch.Geometry, point.Coordinate, EvaluateTolerance);

            INetworkLocation locationOnBranch = new NetworkLocation
                                                    {
                                                        Branch = branch,
                                                        Geometry = new Point(coordinateOnBranch)
                                                    };

            NetworkHelper.UpdateBranchFeatureOffsetFromGeometry(locationOnBranch);

            return (T)(object)Evaluate(locationOnBranch);


/*
            var nearestLocation = GeometryHelper.GetNearestFeature(new Coordinate(x, y), Locations.Values.Cast<IFeature>(), EvaluateTolerance);
            if (nearestLocation == null)
            {
                return (T)(object)double.NaN; // use missing value
            }
*/
/*
            

            return (T)this[nearestLocation];
*/
        }

        // TODO: make it work for T, make interpolators injectable
        //public override T GetInterpolatedValue<T>(params IVariableFilter[] filters)

        
        public virtual IFunction GetTimeSeries(INetworkLocation networkLocation)
        {
            IFunction filteredFunction = Filter(
                new VariableValueFilter<INetworkLocation>(Locations, networkLocation),
                new VariableReduceFilter(Locations));
            filteredFunction.Name = Name + " at " + networkLocation;
            //create an offline timeseris...
            var timeSeries = new TimeSeries();
            timeSeries.Components.Add(new Variable<double>());
            var times = (IEnumerable<DateTime>) filteredFunction.Arguments[0].Values;
            var values = filteredFunction.Components[0].Values;
            timeSeries.SetValues(values,new VariableValueFilter<DateTime>(timeSeries.Time,times));
            return timeSeries;
        }

        public virtual INetworkCoverage AddTimeFilter(DateTime time)
        {
            var selectedTime = Time.Values.FirstOrDefault(t => t >= time);
            if (selectedTime == default(DateTime))
            {
                selectedTime = Time.Values.Last();
            }

            return (NetworkCoverage)Filter(new VariableValueFilter<DateTime>(Time, selectedTime));
        }

        /// <summary>
        /// Sets the locations. Temporarily switches segmentgenerationmethod to none for speed.
        /// </summary>
        /// <param name="locations"></param>
        public virtual void SetLocations(IEnumerable<INetworkLocation> locations)
        {
            SegmentGenerationMethod oldMethod = SegmentGenerationMethod;
            SegmentGenerationMethod = SegmentGenerationMethod.None;
            Locations.SetValues(locations);
            SegmentGenerationMethod = oldMethod;
        }

        public virtual void AddValuesForTime(IEnumerable values, DateTime time)
        {
            if (!IsTimeDependent)
                throw new InvalidOperationException("Cannot add values for non time-dependend coverage");
            
            SetValues(values,new VariableValueFilter<DateTime>(Time,time));
        }

        public virtual double Evaluate(INetworkSegment segment)
        {
            //assumes the segments' index matches the location index
            var location = Locations.Values[Segments.Values.IndexOf(segment)];
            return Evaluate(location);
        }

        public override IFunction GetTimeSeries(ICoordinate coordinate)
        {
            //convert coordinate to networkLocation
            INetworkLocation nearestLocation = GetNearestNetworkLocation(coordinate);

            if (nearestLocation == null)
            {
                return null;
            }

            return GetTimeSeries(nearestLocation);
        }

        private void Locations_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (sender is INetworkLocation && e.PropertyName == "Geometry")
            {
                segmentsInitialized = false;
                //NetworkCoverageHelper.UpdateSegments(this);
            }
        }

        private void Initialize()
        {
            if (initialized || initializing)
            {
                return;
            }

            initializing = true;

            if(Components.Count == 0 || Arguments.Count == 0 || Network == null)
            {
                initializing = false;
                return;
            }

            if (Locations == null)
            {
                initializing = false;
                return;
            }

            //UpdateSegments();
            

            UpdateCoverageGeometry();

            initialized = true;

            initializing = false;
        }

        protected virtual void UpdateSegments()
        {
            if (!initialized)
            {
                Initialize();
            }
            NetworkCoverageHelper.UpdateSegments(this);
        }

        private void SubscribeEvents()
        {
            if (Locations != null)
            {
                Locations.ValuesChanging += locations_ValuesChanging;
                Locations.ValuesChanged += locations_ValuesChanged;
                ((INotifyPropertyChanged) Locations).PropertyChanged += Locations_PropertyChanged;
            }
        }

        private void UnsubscribeEvents()
        {
            if (Locations != null)
            {
                Locations.ValuesChanging -= locations_ValuesChanging;
                Locations.ValuesChanged -= locations_ValuesChanged;
                ((INotifyPropertyChanged)Locations).PropertyChanged -= Locations_PropertyChanged;
            }
        }

        private void locations_ValuesChanged(object sender, FunctionValuesChangedEventArgs e)
        {
            if (!(e.Item is INetworkLocation))
            {
                return;
            }

            var location = e.Item as INetworkLocation;
            if (location != null && location.Branch.Geometry != null && location.Geometry == null) // generate geometry for new location
            {
                var coordinate = GeometryHelper.LineStringCoordinate((ILineString) location.Branch.Geometry, location.Offset);
                location.Geometry = new Point(coordinate);
            }

            if (SegmentGenerationMethod != SegmentGenerationMethod.None)
            {
                segmentsInitialized = false;
            }
            initialized = false;
        }

        private void locations_ValuesChanging(object sender, FunctionValuesChangedEventArgs e)
        {
            if (!(sender is IVariable<INetworkLocation>))
            {
                return;
            }

            //don;t use 'locations'here it calls initialze and therefore very slow.

            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    var location = (INetworkLocation)e.Item;

                    if (location == null)
                    {
                        location = new NetworkLocation(network.Branches[0], 0);
                        e.Item = location;
                    }
                    break;
                case NotifyCollectionChangedAction.Remove:
                    break;
                default:
                    throw new NotSupportedException();
            }
        }

        private void Network_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            // todo optimize several node and/or branch changes can occur as result of an edit operation
            if (sender is IBranch)
            {
                if (e.PropertyName != "Geometry")
                {
                    return;
                }

                if(initialized) // re-initialize everything once network coverage is loaded
                {
                    UpdateNetworkLocationsForBranch(sender as IBranch);
                    segmentsInitialized = false;
                    initialized = false;
                    //NetworkCoverageHelper.UpdateSegments(this);
                }
                //update the lengths
                StoreBranchLength((IBranch) sender);
            }
            //HACK: WHY don't we get a geometry changed when node moves.
            else if (sender is INode)
            {
                var node = (INode) sender;
                if (e.PropertyName != "Geometry")
                {
                    return;
                }
                //this should result in branch changes!.
                foreach (IBranch branch in node.IncomingBranches)
                {
                    UpdateNetworkLocationsForBranch(branch);
                    StoreBranchLength((IBranch)branch);
                }
                foreach (IBranch branch in node.OutgoingBranches)
                {
                    UpdateNetworkLocationsForBranch(branch);
                    StoreBranchLength((IBranch)branch);
                }
                segmentsInitialized = false;
                
                //NetworkCoverageHelper.UpdateSegments(this);
            }
        }

        private void StoreBranchLength(IBranch branch)
        {
            branchLengths[branch] = branch.Length;
        }

        private void UpdateNetworkLocationsForBranch(IBranch branch)
        {
            // branch has changed; update the network location
            IList<INetworkLocation> locations = Locations.Values
                .Where(bf => bf.Branch == branch)
                .OrderBy(bf => bf.Offset)
                .ToList();

            UpdateOffsetsForBranch(locations, branch);

            foreach (INetworkLocation location in locations)
            {
                ICoordinate coordinate = GeometryHelper.LineStringCoordinate((ILineString) branch.Geometry,
                                                                             location.Offset);
                GeometryHelper.SetCoordinate(location.Geometry, 0, coordinate);
            }
        }

        private void UpdateOffsetsForBranch(IEnumerable<INetworkLocation> locations, IBranch branch)
        {
            var oldLength = branchLengths[branch];
            var newLength = branch.Length;
            //performance
            if (oldLength == newLength)
                return;
            
            foreach (var location in locations)
            {
                //scale the offsets
                location.Offset = location.Offset*(newLength/oldLength);
            }
        }

        public virtual double GetInterpolatedValue(params IVariableFilter[] filters)
        {
            Initialize();

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
                if (!filters[0].Variable.ValueType.Equals(typeof (DateTime)))
                {
                    timeFilterIndex = 1;
                }
                else
                {
                    locationFilterIndex = 1;
                }
                
                var dateTime = ((VariableValueFilter<DateTime>) (filters[timeFilterIndex])).Values[0];
                
                if (!Arguments[timeFilterIndex].Values.Contains(dateTime))
                {
                    throw new ArgumentException("Invalid time for network coverage", "filters");
                }
            }
            var networkLocation = ((VariableValueFilter<INetworkLocation>) (filters[locationFilterIndex])).Values[0];
            if (Arguments[locationFilterIndex].Values.Contains(networkLocation))
            {
                // The network location is known; return the exact value
                return (double) GetValues(filters)[0];
            }
            // oops value not available; interpolation or default value
            var networkLocations = new List<INetworkLocation>(Locations.Values);
            IList<INetworkLocation> branchLocations =
                networkLocations.Where(bf => bf.Branch == networkLocation.Branch).OrderBy(bf => bf.Offset).ToList();
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
                    return (double) GetValues(filters[timeFilterIndex], new VariableValueFilter<INetworkLocation>(Arguments[locationFilterIndex], branchLocations[0]))[0];
                }
                return
                    (double) GetValues(new VariableValueFilter<INetworkLocation>(Arguments[locationFilterIndex], branchLocations[0]))[0];
            }
            // Multiple values available; interpolate
            var x = new double[branchLocations.Count + 2];
            //using length iso geometry.length is that a'right?
            x[x.Length - 1] = branchLocations[0].Branch.Length;
        
            for (int i = 0; i < branchLocations.Count; i++)
            {
                x[i + 1] = branchLocations[i].Offset;
            }

            if (IsTimeDependent)
            {
                return Interpolate(branchLocations,
                                   ((VariableValueFilter<DateTime>) (filters[timeFilterIndex])).Values[0], x, /*y,*/
                                   networkLocation.Offset);
            }
            return Interpolate(branchLocations, x, networkLocation.Offset);
        }

        private double Interpolate(IList<INetworkLocation> branchLocations, DateTime dateTime, double[] x,
                                   /*double []y,*/ double offset)
        {
            var yiminus1 = (double) this[dateTime, branchLocations[0]]; //0;
            var yi = (double) this[dateTime, branchLocations[branchLocations.Count - 1]]; //0;
            for (int i = 1; i < x.Length; i++)
            {
                if (offset.CompareTo(x[i]) > 0)
                    continue;
                if (i > 1)
                {
                    yiminus1 = (double) this[dateTime, branchLocations[i - 2]];
                }
                if (i < x.Length - 1)
                {
                    yi = (double) this[dateTime, branchLocations[i - 1]];
                }
                //y[x.Length - 1] = (double)this[branchLocations[branchLocations.Count - 1]];
                return (yiminus1 + (offset - x[i - 1])*(yi - yiminus1)/(x[i] - x[i - 1]));
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
        private double Interpolate(IList<INetworkLocation> branchLocations, double[] x, double offset)
        {
            if (offset > (x.Max() + errorMargin))
            {
                //HACK: commented out exception
                /*throw new InvalidOperationException(
                    string.Format(
                        "NetworkCoverage ({0}): Offset ({1}) exceeds branch length ({2}). Interpolation impossible",
                        Name, offset, x.Max()));*/
                offset = x.Max();
            }
            var yiminus1 = (double) this[branchLocations[0]]; //0;
            var yi = (double) this[branchLocations[branchLocations.Count - 1]]; //0;
            for (int i = 1; i < x.Length; i++)
            {
                if (offset.CompareTo(x[i]) > 0)
                    continue;
                if (i > 1)
                {
                    yiminus1 = (double) this[branchLocations[i - 2]];
                }
                if (i < x.Length - 1)
                {
                    yi = (double) this[branchLocations[i - 1]];
                }
                return (yiminus1 + (offset - x[i - 1])*(yi - yiminus1)/(x[i] - x[i - 1]));
            }
            // it is safe to return last value; check for out of branch at start of this method
            return (double) this[branchLocations[branchLocations.Count - 1]];
            //throw new ArgumentException(string.Format("NetworkCoverage ({0}): Offset ({1}) out of range; can not interpolate", Name, offset), "offset");
        }

        private INetworkLocation GetNearestNetworkLocation(ICoordinate coordinate)
        {
            //TODO: speed up. this sucks when we have 100000 locations (write performance test!). 
            //TODO add a maximal distance otherwise return null
            double minDistance = double.MaxValue;
            INetworkLocation minLocation = null;
            foreach (INetworkLocation location in Locations.Values)
            {
                double distance = coordinate.Distance(location.Geometry.Coordinate);
                if (distance < minDistance)
                {
                    minDistance = distance;
                    minLocation = location;
                }
            }
            return minLocation;
        }

        public virtual double Evaluate(INetworkLocation networkLocation)
        {
            if ((Parent != null) && (IsTimeDependent))
            {
                //can't just convert to filters and handle by function since interpolation logic 
                //is defined at networkcoverage level
                //return Parent.Evaluate<double>(GetFiltersInParent(Filters));
                if (Filters.Count != 1 ||  !(Filters[0] is VariableValueFilter<DateTime>))
                {
                    throw new ArgumentException(
                        "Please specify time filter to retrieve value from time related network coverage");
                }
                var currentTime = ((VariableValueFilter<DateTime>) Filters[0]).Values[0];

                return ((INetworkCoverage)Parent).Evaluate(currentTime, networkLocation);
            }
            //we might have a local filter
            if (IsTimeDependent && Filters.Count == 1 && Filters[0] is VariableValueFilter<DateTime>)
            {
                var time = ((VariableValueFilter<DateTime>) Filters[0]).Values[0];
                return Evaluate(time, networkLocation);
            }
            if ((IsTimeDependent))
                throw new ArgumentException(
                    "Please specify time filter to retrieve value from time related network coverage");
            
            
            return GetInterpolatedValue(new VariableValueFilter<INetworkLocation>(Locations, networkLocation));
        }

        public virtual double Evaluate(DateTime dateTime, INetworkLocation networkLocation)
        {
            if (!IsTimeDependent)
                throw new ArgumentException(
                    "Please do not specify time filter to retrieve value from time related network coverage");
            return GetInterpolatedValue(new VariableValueFilter<DateTime>(Time, dateTime),
                                        new VariableValueFilter<INetworkLocation>(Locations, networkLocation));
        }
        
        private void UpdateCoverageGeometry()
        {
            if (Locations == null)
            {
                return;
            }

            IMultiDimensionalArray<INetworkLocation> locations = Locations.Values;

            if (locations.Count == 0)
            {
                return;
            }

            // Create a geometry object that is defined by all covered feature geometries
            var geometries = new IGeometry[locations.Count];
            for (int i = 0; i < locations.Count; i++)
            {
                geometries[i] = locations[i].Geometry;
                if (geometries[i] == null)
                {
                    // geometry-less feature
                    return;
                }
            }
            Geometry = new GeometryCollection(geometries);
        }


        public override object Clone(bool copyValues)
        {
            // do not let Function Clone the values; function clone will not clone the networklocations but copy a reference
            NetworkCoverage clone = (NetworkCoverage)base.Clone(false);
            if (copyValues)
            {
                foreach (INetworkLocation location in Locations.Values)
                {
                    clone[location.Clone()] = this[location];
                }
            }
            clone.Network = Network;
            return clone;
        }

        

    }
}