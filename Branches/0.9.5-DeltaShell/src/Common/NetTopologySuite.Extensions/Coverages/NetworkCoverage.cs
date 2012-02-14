using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

using DelftTools.Functions;
using DelftTools.Functions.Filters;
using DelftTools.Functions.Generic;
using DelftTools.Functions.Tuples;
using DelftTools.Units;
using DelftTools.Utils;
using DelftTools.Utils.Aop.NotifyPropertyChange;
using DelftTools.Utils.Collections;
using DelftTools.Utils.Collections.Generic;

using GeoAPI.Extensions.Coverages;
using GeoAPI.Extensions.Networks;
using GeoAPI.Geometries;

using GisSharpBlog.NetTopologySuite.Geometries;

using NetTopologySuite.Extensions.Actions;
using NetTopologySuite.Extensions.Geometries;
using NetTopologySuite.Extensions.Networks;

using IEditableObject = DelftTools.Utils.IEditableObject;

namespace NetTopologySuite.Extensions.Coverages
{
    [NotifyPropertyChange]
    public class NetworkCoverage : Coverage, INetworkCoverage, IEditableObject
    {
        // private static readonly ILog log = LogManager.GetLogger(typeof(NetworkCoverage));
        private const double ErrorMargin = 1.0e-3;
        private const string DefaultNetworkCoverageName = "network coverage";

        private bool initialized; // lazy initialization of attributes

        /// <summary>
        /// Called afterload
        /// </summary>
        private bool initializing;

        // no bubbling here since there is no parent/child relationship. performance issue
        [NoNotifyPropertyChange]
        private INetwork network;

        private SegmentGenerationMethod segmentationType;

        private readonly Variable<INetworkSegment> segments;
        private bool segmentsInitialized;

        //store to know the previous length of branch. If branch changes the locations can be scaled along
        private IDictionary<IBranch, double> branchLengths;

        private IDictionary<IBranch, List<INetworkLocation>> branchNetworkLocations;

        public NetworkCoverage()
            : this(DefaultNetworkCoverageName, false)
        {
        }

        public NetworkCoverage(string name, bool isTimeDependend)
            : this(name, isTimeDependend, name, "-")
        {
        }

        public NetworkCoverage(string name, bool isTimeDependend, string outputName, string outputUnit)
        {
            base.Name = name;
            Components.Add(new Variable<double>(outputName) { Unit = new Unit(outputUnit, outputUnit) });

            var locations = new Variable<INetworkLocation>("network_location");
            locations.ExtrapolationType = ExtrapolationType.Constant;
            if (isTimeDependend)
            {
                Arguments.Add(new Variable<DateTime>("time"));
                Time = (IVariable<DateTime>)Arguments[0];
            }

            Arguments.Add(locations);
            //does segments have to be a variable?
            segments = new Variable<INetworkSegment>("segment") {IsAutoSorted = false};
            Geometry = new Point(0, 0);
            EvaluateTolerance = 1;
            branchLengths = new Dictionary<IBranch, double>();
            branchNetworkLocations = new Dictionary<IBranch, List<INetworkLocation>>();
            SubscribeEvents();
        }

        private IGeometry geometry;
        [NoNotifyPropertyChange]
        public override IGeometry Geometry
        {
            get
            {
                Initialize();
                return geometry;
            }
            set
            {
                initialized = false;
                geometry = value;
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
                    ((INetworkCoverage)Parent).Network = value;
                    return;
                }

                UnsubscribeFromNetwork();

                network = value;
                StoreBranchLengths();
                updateLocationsDictionary = true;

                SubscribeToNetwork();

                initialized = false;
            }
        }

        private void UnsubscribeFromNetwork()
        {
            if (null != network && network is INotifyCollectionChange)
            {
                ((INotifyPropertyChanged)network).PropertyChanged -= NetworkPropertyChanged;
                ((INotifyCollectionChange)network).CollectionChanged -= OnNetworkCollectionChanged;
            }
        }

        private void SubscribeToNetwork()
        {
            if (null != network && network is INotifyCollectionChange)
            {
                ((INotifyPropertyChanged)network).PropertyChanged += NetworkPropertyChanged;
                ((INotifyCollectionChange)network).CollectionChanged += OnNetworkCollectionChanged;
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
                StoreBranchLength(branch);
                
            }
        }

        void OnNetworkCollectionChanged(object sender, NotifyCollectionChangingEventArgs e)
        {
            if (network == null)
            {
                return;
            }

            FireNetworkCollectionChanged(sender,e);

            if (networkIsInKnownEditAction)
                return;

            if (network.Branches == sender)
            {
                segmentsInitialized = false;
                StoreBranchLengths();
                if (e.Action == NotifyCollectionChangeAction.Remove)
                {
                    var removedBranch = (IBranch)e.Item;
                    //remove the values for this branch
                    var locations = Locations.Values.Where(nl => nl.Branch == removedBranch).ToList();
                    foreach (var loc in locations)
                    {
                        Locations.Values.Remove(loc);
                    }
                }
                //NetworkCoverageHelper.UpdateSegments(this);    
            }
            
        }

        private void FireNetworkCollectionChanged(object sender, NotifyCollectionChangingEventArgs notifyCollectionChangingEventArgs)
        {
            if (NetworkCollectionChanged != null)
            {
                NetworkCollectionChanged(sender, notifyCollectionChangingEventArgs);
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

                var variable = Arguments.FirstOrDefault(a => a.ValueType == typeof(INetworkLocation));

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
            get { return segmentationType; }
            set
            {
                segmentationType = value;
                segmentsInitialized = false;
                initialized = false;
            }
        }

        public virtual double DefaultValue
        {
            get { return (double)Components[0].DefaultValue; }
            set { Components[0].DefaultValue = value; }
        }

        public override object Evaluate(ICoordinate coordinate)
        {
            return Evaluate<object>(coordinate.X, coordinate.Y);
        }

        public override T Evaluate<T>(ICoordinate coordinate)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Tolerance used when evaluating values based on coordinate.
        /// </summary>
        public virtual double EvaluateTolerance { get; set; }

        public override T Evaluate<T>(double x, double y)
        {
            // find location on nearest branch
            var point = new Point(x, y);
            var branch = NetworkHelper.GetNearestBranch(Network.Branches, point, EvaluateTolerance);

            if (branch != null)
            {
                var coordinateOnBranch = GeometryHelper.GetNearestPointAtLine((ILineString) branch.Geometry,
                                                                              point.Coordinate, EvaluateTolerance);

                INetworkLocation locationOnBranch = new NetworkLocation
                                                        {
                                                            Branch = branch,
                                                            Geometry = new Point(coordinateOnBranch)
                                                        };

                NetworkHelper.UpdateBranchFeatureOffsetFromGeometry(locationOnBranch);

                return (T) (object) Evaluate(locationOnBranch);
            }
            return default(T);
        }

        // TODO: make it work for T, make interpolators injectable
        //public override T GetInterpolatedValue<T>(params IVariableFilter[] filters)

        public double Evaluate(DateTime time, IBranchFeature branchFeature)
        {
            return Evaluate(time, new NetworkLocation(branchFeature.Branch, branchFeature.Offset));
        }

        public virtual IFunction GetTimeSeries(INetworkLocation networkLocation)
        {
            IFunction filteredFunction = Filter(
                new VariableValueFilter<INetworkLocation>(Locations, networkLocation),
                new VariableReduceFilter(Locations));
            filteredFunction.Name = Name + " at " + networkLocation;
            //create an offline timeseris...
            var timeSeries = new TimeSeries();
            var component = new Variable<double>
                                {
                                    Name = filteredFunction.Name,
                                    Unit = (IUnit) filteredFunction.Components[0].Unit.Clone(),
                                    NoDataValue = filteredFunction.Components[0].NoDataValue
                                };
            timeSeries.Components.Add(component);
            var times = (IEnumerable<DateTime>)filteredFunction.Arguments[0].Values;
            var values = filteredFunction.Components[0].Values;
            timeSeries.SetValues(values, new VariableValueFilter<DateTime>(timeSeries.Time, times));
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

            SetValues(values, new VariableValueFilter<DateTime>(Time, time));
        }

        public virtual double Evaluate(INetworkSegment segment)
        {
            //assumes the segments' index matches the location index
#if MONO
			var location = (INetworkLocation)((IMultiDimensionalArray)Locations.Values)[Segments.Values.IndexOf(segment)];
#else
			var location = Locations.Values[Segments.Values.IndexOf(segment)];
#endif			
			return Evaluate(location);
        }

        public IList<INetworkLocation> GetLocationsForBranch(IBranch branch)
        {
            if (Parent != null)
            {
                return ((INetworkCoverage)Parent).GetLocationsForBranch(branch);
            }

            if (updateLocationsDictionary)
            {
                StoreNetworkLocationsPerBranch();
                updateLocationsDictionary = false;
            }

            if (branchNetworkLocations.ContainsKey(branch))
            {
                return new List<INetworkLocation>(branchNetworkLocations[branch]); //clone!
            }
            else
            {
                return new List<INetworkLocation>();
            }
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

        private void LocationsPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (Network == null)
                return;

            if (sender is INetworkLocation && e.PropertyName == "Geometry")
            {
                segmentsInitialized = false;

                if (Locations.IsAutoSorted)
                {
                    INetworkLocation networkLocation = (INetworkLocation) sender;

                    // if the geometry of a network location is changed (in the map) update sort of location arguments. 
                    // Function does not support sorting by argument.
                    // For value based or immutable argument types this is not an issue.
                    // notes: checking for index change of networkLocation in Locations does not work
                    // MultiDimensionalArrayHelper.GetInsertionIndex is unreliable since Locations.Values are not sorted
                    var locationValues =
                        GetValues(new VariableValueFilter<INetworkLocation>(Locations, new[] {networkLocation}));
                    object[] values = new object[locationValues.Count];
                    for (int i = 0; i < locationValues.Count; i++)
                    {
                        values[i] = locationValues[i];
                    }
                    IsSorting = true;
                    RemoveValues(new VariableValueFilter<INetworkLocation>(Locations, new[] {networkLocation}));
                    SetValues(values, new VariableValueFilter<INetworkLocation>(Locations, new[] {networkLocation}));
                    IsSorting = false;
                }
            }

            updateLocationsDictionary = true;
        }

        [NoNotifyPropertyChange]
        public virtual bool IsSorting { get; private set; }

        public virtual event EventHandler<NotifyCollectionChangingEventArgs> NetworkCollectionChanged;

        private bool updateLocationsDictionary = true;
        private bool networkIsInKnownEditAction;

        private void StoreNetworkLocationsPerBranch()
        {
            branchNetworkLocations = Locations.Values.GroupBy(nl => nl.Branch).ToDictionary(v => v.Key, v => v.ToList());
        }

        private void Initialize()
        {
            if (initialized || initializing)
            {
                return;
            }

            initializing = true;

            if (Components.Count == 0 || Arguments.Count == 0 || Network == null)
            {
                initializing = false;
                return;
            }

            if (Locations == null)
            {
                initializing = false;
                return;
            }

            
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
                Locations.ValuesChanged += LocationsValuesChanged;
                ((INotifyPropertyChanged)Locations).PropertyChanged += LocationsPropertyChanged;
            }
        }

        private void UnsubscribeEvents()
        {
            if (Locations != null)
            {
                Locations.ValuesChanged -= LocationsValuesChanged;
                ((INotifyPropertyChanged)Locations).PropertyChanged -= LocationsPropertyChanged;
            }
        }

        protected virtual void LocationsValuesChanged(object sender, FunctionValuesChangingEventArgs e)
        {
            if (SegmentGenerationMethod != SegmentGenerationMethod.None)
            {
                segmentsInitialized = false;
            }

            initialized = false;
            return;
        }
        /// <summary>
        /// Just finished editing
        /// </summary> 
        private void DoEditAction()
        {
            if (Network.CurrentEditAction is BranchSplitAction)
            {
                HandleBranchSplit((BranchSplitAction)network.CurrentEditAction);
                return;
            }
            if (Network.CurrentEditAction is BranchMergeAction)
            {
                HandleBranchMerge((BranchMergeAction)network.CurrentEditAction);
                return;
            }
            throw new InvalidOperationException("Should not get here");
        }


        private void NetworkPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (Network == null)
            {
                return;
            }
            // handle only IsEditing if the action is 'known'
            if ((sender is INetwork && e.PropertyName == "IsEditing") &&
                (Network.CurrentEditAction is BranchSplitAction || Network.CurrentEditAction is BranchMergeAction))
            {
                //only 'monitor' known actions...other actions should be handled as normal changes
                if (Network.IsEditing)
                {
                    networkIsInKnownEditAction = Network.IsEditing;    
                    BeginEdit(Network.CurrentEditAction);
                }
                else
                {
                    if (Network.EditWasCancelled)
                    {
                        CancelEdit();
                    }
                    else
                    {
                        DoEditAction();
                        EndEdit();
                    }
                }
            }

            if (networkIsInKnownEditAction)
                return;

            if (sender is IBranch)
            {
                if (e.PropertyName == "Geometry")
                {
                    UpdateNetworkLocationsForBranch(sender as IBranch, true);
                    segmentsInitialized = false;
                    initialized = false;

                    StoreBranchLength((IBranch)sender);
                }
                else if (e.PropertyName == "Length")
                {
                    UpdateNetworkLocationsForBranch(sender as IBranch, false);
                    segmentsInitialized = false;
                    initialized = false;

                    StoreBranchLength((IBranch)sender);
                }

            }
        }
        
        /// <summary>
        /// This method can be override for custom logic in moving values / locations
        /// </summary>
        /// <param name="currentEditAction"></param>
        protected virtual void UpdateValuesForBranchSplit(BranchSplitAction currentEditAction)
        {
            double splitAtOffset = currentEditAction.SplittedBranch.Length;
            var splitLocation = new NetworkLocation(currentEditAction.SplittedBranch, splitAtOffset);
            var value = Evaluate(splitLocation);

            IEnumerable<INetworkLocation> networkLocationsToMove =
                Locations.Values.Where(
                    nl => nl.Branch == currentEditAction.SplittedBranch && nl.Offset >= splitAtOffset)
                    .ToList();
            
            foreach (var location in networkLocationsToMove)
            {
                location.Branch = currentEditAction.NewBranch;
                location.Offset = location.Offset - splitAtOffset;
            }

            //add a point at the end of the orignal branch 
            var startLocation = new NetworkLocation(currentEditAction.NewBranch, 0);
            this[splitLocation] = value;
            this[startLocation] = value;
        }

        private void HandleBranchMerge(BranchMergeAction branchMergeAction)
        {
            IBranch extendedBranch = branchMergeAction.ExtendedBranch;
            IBranch removedBranch = branchMergeAction.RemovedBranch;

            var locationsOnExtendedBranch =
                Locations.Values.Where(l => l.Branch == extendedBranch).ToList();

            var locationsOnRemovedBranch =
                Locations.Values.Where(l => l.Branch == removedBranch).ToList();

            //remove the locations of the branches
            foreach (var networkLocation in locationsOnRemovedBranch.Concat(locationsOnExtendedBranch))
            {
                Locations.Values.Remove(networkLocation);
            }

            //reinitialize
            segmentsInitialized = false;
            initialized = false;
            StoreBranchLengths();
        }

        private void HandleBranchSplit(BranchSplitAction currentEditAction)
        {
            var coverageHasLocationsForSplitBranch =
                Locations.Values.Any(l => l.Branch == currentEditAction.SplittedBranch);
            //nothing to DO..leave it at default
            if (coverageHasLocationsForSplitBranch)
            {
                UpdateValuesForBranchSplit(currentEditAction);    
            }
            
            //reinitialize
            segmentsInitialized = false;
            initialized = false;
            StoreBranchLengths();
            
        }

        private void StoreBranchLength(IBranch branch)
        {
            branchLengths[branch] = branch.Length;
        }

        private void UpdateNetworkLocationsForBranch(IBranch branch, bool geometry)
        {
            // branch has changed; update the network location
            IList<INetworkLocation> locations = Locations.Values
                .Where(bf => bf.Branch == branch)
                .OrderBy(bf => bf.Offset)
                .ToList();

            // if geometry has changed update chainage when using map length
            if ((!branch.IsLengthCustom) && (geometry))
            {
                UpdateOffsetsForBranch(locations, branch);
            }
            // if length has changed update chainage when using custom length
            if ((branch.IsLengthCustom) && (!geometry))
            {
                UpdateOffsetsForBranch(locations, branch);
            }

            var factor = (branch.IsLengthCustom) ? branch.Geometry.Length / branch.Length : 1.0;

            foreach (INetworkLocation location in locations)
            {
                ICoordinate coordinate = GeometryHelper.LineStringCoordinate((ILineString)branch.Geometry,
                                                                             factor * location.Offset);
                GeometryHelper.SetCoordinate(location.Geometry, 0, coordinate);
            }
        }

        private void UpdateOffsetsForBranch(IEnumerable<INetworkLocation> locations, IBranch branch)
        {
            //unknown branch..first time? nhibernate load?
            if (!branchLengths.ContainsKey(branch))
            {
                branchLengths[branch] = branch.Length;
                return;
            }
            var oldLength = branchLengths[branch];
            var newLength = branch.Length;
            //performance
            if (oldLength == newLength)
                return;

            foreach (var location in locations)
            {
                //scale the offsets
                location.Offset = location.Offset * (newLength / oldLength);
            }
        }
        
        //TODO:  get this out..should be handled as custom interpolator on Variable level. So that it works on individual components etc.
        public virtual double GetInterpolatedValue(params IVariableFilter[] filters)
        {
            Initialize();

            if (filters.Length != Arguments.Count || filters[0].Variable != Arguments[0] ||
                !(filters[0] is IVariableValueFilter))
            {
                throw new ArgumentOutOfRangeException("filters",
                                                      "Please specify networkCoverage using VariableValueFilter in filters.");
            }
            if (Locations.ExtrapolationType != ExtrapolationType.Constant)
            {
                throw new NotSupportedException("Evaluation failed : currently only constant Extrapolation for locations is supported.");
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
            var networkLocation = ((VariableValueFilter<INetworkLocation>)(filters[locationFilterIndex])).Values[0];
            if (Arguments[locationFilterIndex].Values.Contains(networkLocation))
            {
                // The network location is known; return the exact value
                return (double)GetValues(filters)[0];
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
                    return (double)GetValues(filters[timeFilterIndex], new VariableValueFilter<INetworkLocation>(Arguments[locationFilterIndex], branchLocations[0]))[0];
                }
                return
                    (double)GetValues(new VariableValueFilter<INetworkLocation>(Arguments[locationFilterIndex], branchLocations[0]))[0];
            }
            // Multiple values available; interpolate
            var x = new double[branchLocations.Count + 2];
            // never use Branch.Geametry.Length. Branch.Length will also work for IsCustomLength
            x[x.Length - 1] = branchLocations[0].Branch.Length;

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

        private double DoubleCompareWithBuffer(double value, double compare)
        {
            if (Math.Abs(value - compare) < ErrorMargin)
            {
                return 0;
            }
            return value.CompareTo(compare);
        }

        private double Interpolate(IList<INetworkLocation> branchLocations, DateTime dateTime, double[] x,
            /*double []y,*/ double offset)
        {
            var yiminus1 = (double)this[dateTime, branchLocations[0]]; //0;
            var yi = (double)this[dateTime, branchLocations[branchLocations.Count - 1]]; //0;
            for (int i = 1; i < x.Length; i++)
            {
                // Do buffered compare to avoid exceptions based on rounding errors
                if (DoubleCompareWithBuffer(offset, x[i]) > 0)
                    continue;
                if (i > 1)
                {
                    yiminus1 = (double)this[dateTime, branchLocations[i - 2]];
                }
                if (i < x.Length - 1)
                {
                    yi = (double)this[dateTime, branchLocations[i - 1]];
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
        private double Interpolate(IList<INetworkLocation> branchLocations, double[] x, double offset)
        {
            if (offset > (x.Max() + ErrorMargin))
            {
                //HACK: commented out exception
                throw new InvalidOperationException(
                    string.Format(
                        "NetworkCoverage ({0}): Offset ({1}) exceeds branch length ({2}). Interpolation impossible",
                        Name, offset, x.Max()));
            }

            if (offset > x.Max())
            {
                offset = x.Max();
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

                switch (Locations.InterpolationType)
                {
                    case InterpolationType.Linear:
                        return (yiminus1 + (offset - x[i - 1]) * (yi - yiminus1) / (x[i] - x[i - 1]));
                    case InterpolationType.Constant:
                        return yi;
                    case InterpolationType.None:
                        throw new NotImplementedException();
                }
            }

            // it is safe to return last value; check for out of branch at start of this method
            return (double)this[branchLocations[branchLocations.Count - 1]];
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
                if (Filters.Count != 1 || !(Filters[0] is VariableValueFilter<DateTime>))
                {
                    throw new ArgumentException(
                        "Please specify time filter to retrieve value from time related network coverage");
                }
                var currentTime = ((VariableValueFilter<DateTime>)Filters[0]).Values[0];

                return ((INetworkCoverage)Parent).Evaluate(currentTime, networkLocation);
            }
            //we might have a local filter
            if (IsTimeDependent && Filters.Count == 1 && Filters[0] is VariableValueFilter<DateTime>)
            {
                var time = ((VariableValueFilter<DateTime>)Filters[0]).Values[0];
                return Evaluate(time, networkLocation);
            }
            if ((IsTimeDependent))
                throw new ArgumentException(
                    "Please specify time filter to retrieve value from time related network coverage");


            return GetInterpolatedValue(new VariableValueFilter<INetworkLocation>(Locations, networkLocation));
        }

        public double Evaluate(IBranchFeature branchFeature)
        {
            return Evaluate(new NetworkLocation(branchFeature.Branch, branchFeature.Offset));
        }

        public IList<double> EvaluateWithinBranch(IList<INetworkLocation> networkLocations)
        {
            // gel all existing locations for branch
            var existingLocations = GetLocationsForBranch(networkLocations.First().Branch);

            var allLocations = Locations.Values;

            var minIndex = allLocations.IndexOf(existingLocations.First());
            var maxIndex = allLocations.IndexOf(existingLocations.Last());

            var existingValues = base.GetValues<double>(new VariableIndexRangesFilter(Locations, new List<Pair<int, int>>(new[] { new Pair<int, int>(minIndex, maxIndex) })));

            if (existingLocations.Count != existingValues.Count)
                throw new IndexOutOfRangeException("Location to Value (x to y) mapping not 1-to-1.");

            // interpoalte / lookup values for networkLocations
            var x0 = existingLocations[0].Offset;
            var y0 = existingValues[0];
            var x1 = x0;
            var y1 = y0;

            IList<double> allValues = new List<double>();

            for (int i = 0, j = 0; i < networkLocations.Count; i++)
            {
                if (networkLocations[i].Offset == existingLocations[j].Offset)
                {
                    allValues.Add(existingValues[j]);

                    if (j < existingLocations.Count)
                    {
                        x0 = existingLocations[j].Offset;
                        y0 = existingValues[j];

                        if (j < existingLocations.Count - 1)
                        {
                            x1 = existingLocations[j + 1].Offset;
                            y1 = existingValues[j + 1];
                            j++;
                        }
                    }
                }
                else
                {
                    bool firstLocation = i == 0;
                    bool lastLocation = i == (networkLocations.Count - 1);

                    if (firstLocation || lastLocation) //first or last: extrapolation
                    {
                        switch(Locations.ExtrapolationType)
                        {
                            case ExtrapolationType.Constant:
                                allValues.Add(y0);
                                break;
                            case ExtrapolationType.Linear:
                                throw new NotSupportedException("Evaluation failed : Currently only constant Extrapolation for locations is supported.");
                                if (existingValues.Count > 1)
                                {
                                    if (firstLocation)
                                    {
                                        var tempx1 = existingLocations[1].Offset;
                                        var tempy1 = existingValues[1];
                                        allValues.Add(InterpolateLinear(x0, tempx1, networkLocations[i].Offset, y0,
                                                                        tempy1));
                                    }
                                    else
                                    {
                                        int beforeLast = existingLocations.Count - 2;
                                        var tempx0 = existingLocations[beforeLast].Offset;
                                        var tempy0 = existingValues[beforeLast];
                                        allValues.Add(InterpolateLinear(tempx0, x1, networkLocations[i].Offset, tempy0,
                                                                        y1));
                                    }
                                }
                                else //not possible to do linear extrapolation with only 1 known value (throw exception=not user friendly?)
                                {
                                    allValues.Add(this.DefaultValue);
                                }
                                break;
                            case ExtrapolationType.None:
                                throw new NotSupportedException("Evaluation failed : Currently only constant Extrapolation for locations is supported.");
                                //allValues.Add(this.DefaultValue);
                                break;
                            case ExtrapolationType.Periodic:
                                throw new NotSupportedException("ExtrapolationType Periodic not supported!");

                        }
                    }
                    else //within branch: interpolation
                    {
                        if (Locations.InterpolationType == InterpolationType.Linear)
                            allValues.Add(InterpolateLinear(x0, x1, networkLocations[i].Offset, y0, y1));
                        else if (Locations.InterpolationType == InterpolationType.None)
                            allValues.Add(this.DefaultValue);
                        else if (Locations.InterpolationType == InterpolationType.Constant)
                            allValues.Add(InterpolateNearest(x0, x1, networkLocations[i].Offset, y0, y1));
                    }
                }
            }

            return allValues;
        }

        private double InterpolateNearest(double x0, double x1, double offset, double y0, double y1)
        {
            double diff0 = Math.Abs(x0 - offset);
            double diff1 = Math.Abs(x1 - offset);

            return (diff0 < diff1 ? y0 : y1);
        }

        public double InterpolateLinear(double x0, double x1, double x, double y0, double y1)
        {
            double diff = x1 - x0;
            if (diff > 0)
            {
                double ratio = (x - x0) / diff;
                return ratio * y1 + (1 - ratio) * y0;
            }
            return y0;
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

            if(Network.Branches.Count == 0)
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
#if MONO				
                geometries[i] = ((INetworkLocation)(((IMultiDimensionalArray)locations)[i])).Geometry;
#else
                geometries[i] = locations[i].Geometry;
#endif
				
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
            var clone = (NetworkCoverage)base.Clone(copyValues);

            // TODO: move this logic to FunctionStore(s)
            if (copyValues && Store is MemoryFunctionStore)
            {
                for (var i = 0; i < clone.Locations.Values.Count; i++)
                {
                    // replace values by their clone..because location are reference types saved by NH...maybe change this?
                    clone.Locations.Values[i] = (INetworkLocation) clone.Locations.Values[i].Clone();
                }
            }
            clone.Network = Network;
            return clone;
        }

        public bool IsEditing { get; set; }
        
        public bool EditWasCancelled
        {
            get { throw new NotImplementedException(); }
        }

        public IEditAction CurrentEditAction
        {
            get; private set; }

        public void BeginEdit(IEditAction action)
        {
            CurrentEditAction = action;
            IsEditing = true;
        }

        public void EndEdit()
        {
            IsEditing = false;
        }

        public void CancelEdit()
        {
            IsEditing = false;
        }
    }
}