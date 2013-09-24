using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using DelftTools.Functions;
using DelftTools.Functions.Filters;
using DelftTools.Functions.Generic;
using DelftTools.Functions.DelftTools.Utils.Tuples;
using DelftTools.Units;
using DelftTools.Utils.Aop;
using DelftTools.Utils.Collections;
using DelftTools.Utils.Collections.Generic;
using DelftTools.Utils.Editing;
using GeoAPI.Extensions.Coverages;
using GeoAPI.Extensions.Networks;
using GeoAPI.Geometries;
using GisSharpBlog.NetTopologySuite.Geometries;
using NetTopologySuite.Extensions.Actions;
using NetTopologySuite.Extensions.Geometries;
using NetTopologySuite.Extensions.Networks;

namespace NetTopologySuite.Extensions.Coverages
{
    [Entity(FireOnCollectionChange=false)]
    public class NetworkCoverage : Coverage, INetworkCoverage
    {
        private const string DefaultNetworkCoverageName = "network coverage";

        private bool initialized; // lazy initialization of attributes

        /// <summary>
        /// Called after load
        /// </summary>
        private bool initializing;

        // no bubbling here since there is no parent/child relationship. performance issue
        private INetwork network;

        private SegmentGenerationMethod segmentationType;

        private readonly Variable<INetworkSegment> segments;

        protected bool segmentsInitialized { get; set; }

        //store to know the previous length of branch. If branch changes the locations can be scaled along
        private IDictionary<IBranch, double> branchLengths;

        private IDictionary<IBranch, List<INetworkLocation>> branchNetworkLocations;
        
        private bool updateLocationsDictionary = true;
        
        private bool networkIsInKnownEditAction;

        private IGeometry geometry;

        private static bool updatingBranchGeometry; // performance, when branch Geometry changes - we only need to update location Geometry (faster)

        // default network coverage supports interpolation
        // over ordered branches
        private bool interpolateAcrossNodes = true;

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
            Components.Add(new Variable<double>(outputName) {Unit = new Unit(outputUnit, outputUnit)});

            var locations = new Variable<INetworkLocation>("network_location");
            locations.ExtrapolationType = ExtrapolationType.Constant;
            if (isTimeDependend)
            {
                Arguments.Add(new Variable<DateTime>("time"));
                Time = (IVariable<DateTime>) Arguments[0];
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

        [Aggregation]
        public virtual INetwork Network
        {
            get
            {
                if (Parent != null)
                {
                    return ((INetworkCoverage) Parent).Network;
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
            if (network is INotifyCollectionChange)
            {
                ((INotifyPropertyChanged) network).PropertyChanged -= NetworkPropertyChanged;
                ((INotifyPropertyChanging)network).PropertyChanging -= NetworkPropertyChanging;
                ((INotifyCollectionChange) network).CollectionChanged -= OnNetworkCollectionChanged;
            }
        }

        private void SubscribeToNetwork()
        {
            if (network is INotifyCollectionChange)
            {
                ((INotifyPropertyChanged) network).PropertyChanged += NetworkPropertyChanged;
                ((INotifyPropertyChanging)network).PropertyChanging += NetworkPropertyChanging;
                ((INotifyCollectionChange) network).CollectionChanged += OnNetworkCollectionChanged;
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

        private void OnNetworkCollectionChanged(object sender, NotifyCollectionChangingEventArgs e)
        {
            if (network == null)
            {
                return;
            }

            FireNetworkCollectionChanged(sender, e);

            if (networkIsInKnownEditAction)
                return;

            if (network.Branches == sender)
            {
                segmentsInitialized = false;

                if (e.Action == NotifyCollectionChangeAction.Add)
                {
                    var addedBranch = e.Item as IBranch;
                    if (addedBranch != null) StoreBranchLength(addedBranch);
                    return;
                }

                if (e.Action == NotifyCollectionChangeAction.Remove)
                {
                    var removedBranch = e.Item as IBranch;
                    if (removedBranch != null) RemoveBranchLength(removedBranch);
                    OnBranchRemoved(e);
                    return;
                }
                
                StoreBranchLengths();
            }
        }

        [EditAction]
        private void OnBranchRemoved(NotifyCollectionChangingEventArgs e)
        {
            if (!Store.SupportsPartialRemove)
            {
                Clear();
                return;
            }

            var removedBranch = (IBranch) e.Item;
            //remove the values for this branch
            var locationArray = Locations.Values;
            
            BeginEdit(new DefaultEditAction("Remove locations for removed branch"));
            try
            {
                var oldSorted = locationArray.IsAutoSorted;
                locationArray.IsAutoSorted = false; //ordering no longer reliable since a branch is already removed

                var locations = locationArray.Where(nl => nl.Branch == removedBranch).ToList();
                foreach (var loc in locations)
                {
                    locationArray.Remove(loc);
                }

                locationArray.IsAutoSorted = oldSorted;
            }
            finally
            {
                EndEdit();
            }
        }
        
        private void FireNetworkCollectionChanged(object sender,
                                                  NotifyCollectionChangingEventArgs notifyCollectionChangingEventArgs)
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

                var variable = Arguments.FirstOrDefault(a => a.ValueType == typeof (INetworkLocation));

                if (variable == null)
                {
                    return null;
                }

                return (IVariable<INetworkLocation>) variable;
            }
        }

        public virtual IVariable<INetworkSegment> Segments
        {
            get
            {
                //if we are time filtered we use the segments of the parent
                if ((Parent != null) && Filters.All(f => f.Variable == ((INetworkCoverage) Parent).Time))
                {
                    return ((INetworkCoverage) Parent).Segments;
                }

                if (!segmentsInitialized)
                {
                    UpdateSegments();
                }

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

        [NoNotifyPropertyChange]
        public virtual double DefaultValue
        {
            get { return (double) Components[0].DefaultValue; }
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
            var locationOnBranch = GetLocationOnBranch(x, y);

            if (locationOnBranch != null)
            {
                return (T) (object) Evaluate(locationOnBranch);
            }
            return default(T);
        }

        public override object Evaluate(ICoordinate coordinate, DateTime? time)
        {
            if (IsTimeDependent && time.HasValue)
            {
                return FilterTime(time.Value).Evaluate(coordinate);
            }
            return Evaluate(coordinate);
        }

        public virtual INetworkLocation GetLocationOnBranch(double x, double y)
        {
            var point = new Point(x, y);
            var branch = NetworkHelper.GetNearestBranch(Network.Branches, point, EvaluateTolerance);

            if (branch == null)
                return null;

            var coordinateOnBranch = GeometryHelper.GetNearestPointAtLine((ILineString) branch.Geometry,
                                                                          point.Coordinate, EvaluateTolerance);

            if (coordinateOnBranch == null)
                return null;

            INetworkLocation locationOnBranch = new NetworkLocation
                                                    {
                                                        Branch = branch,
                                                        Geometry = new Point(coordinateOnBranch)
                                                    };

            NetworkHelper.UpdateBranchFeatureChainageFromGeometry(locationOnBranch);

            return locationOnBranch;
        }

        // TODO: make it work for T, make interpolators injectable
        //public override T GetInterpolatedValue<T>(params IVariableFilter[] filters)

        public virtual double Evaluate(DateTime time, IBranchFeature branchFeature)
        {
            return Evaluate(time, new NetworkLocation(branchFeature.Branch, branchFeature.Chainage));
        }

        public virtual IFunction GetTimeSeries(IBranchFeature branchFeature)
        {
            if (Time.Values.Count == 0) //no time values, no timeseries
                return null;

            INetworkLocation nearestLocation = GetNearestNetworkLocation(branchFeature.Geometry.Coordinate,
                                                                         branchFeature.Branch);

            if (nearestLocation == null)
            {
                return null;
            }

            var filteredFunction = Filter(
                new VariableValueFilter<INetworkLocation>(Locations, nearestLocation),
                new VariableReduceFilter(Locations));
            filteredFunction.Name = Name + " at " + nearestLocation;
            return filteredFunction;
        }

        public override IFunction Filter(params IVariableFilter[] filters)
        {
            var filtered = base.Filter(filters);

            var networkCoverage = filtered as INetworkCoverage;
            if (networkCoverage != null)
            {
                // Hack: [bouvrie]: Temporary skip this step in case 1st component is not a double
                if (Components[0].ValueType == typeof (double))
                {
                    networkCoverage.DefaultValue = DefaultValue;
                }
            }

            return filtered;
        }

        public virtual INetworkCoverage AddTimeFilter(DateTime time)
        {
            var selectedTime = Time.Values.FirstOrDefault(t => t >= time);
            if (selectedTime == default(DateTime))
            {
                selectedTime = Time.Values.Last();
            }

            return (NetworkCoverage) Filter(new IVariableFilter[]
                                                {
                                                    new VariableValueFilter<DateTime>(Time, selectedTime),
                                                    new VariableReduceFilter(Time)
                                                });
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

        public virtual IList<INetworkLocation> GetLocationsForBranch(IBranch branch)
        {
            if (Parent != null)
            {
                return ((INetworkCoverage) Parent).GetLocationsForBranch(branch);
            }

            if (updateLocationsDictionary)
            {
                StoreNetworkLocationsPerBranch();
                updateLocationsDictionary = false;
            }

            return branchNetworkLocations.ContainsKey(branch)
                       ? new List<INetworkLocation>(branchNetworkLocations[branch])
                       : new List<INetworkLocation>();
        }

        public override IFunction GetTimeSeries(ICoordinate coordinate)
        {
            //try to convert coordinate to branch based on exact centroid match
            var branch = TryGetExactBranchMatch(coordinate);

            //convert coordinate to networkLocation
            INetworkLocation nearestLocation = GetNearestNetworkLocation(coordinate, branch);

            if (nearestLocation == null)
            {
                return null;
            }

            return GetTimeSeries(nearestLocation);
        }

        private IBranch TryGetExactBranchMatch(ICoordinate coordinate)
        {
            foreach (var branch in Network.Branches)
            {
                if (branch.Geometry != null && branch.Geometry.Centroid.Coordinate.Equals2D(coordinate))
                {
                    return branch;
                }
            }
            return null;
        }

        private void LocationsPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (Network == null)
                return;

            if (sender is INetworkLocation && e.PropertyName == "Geometry")
            {
                segmentsInitialized = false;
                
                // sorting is not needed when correcting the offset and geometry
                if (!updatingBranchGeometry)
                {
                    OnLocationsGeometryChanged((INetworkLocation) sender);
                }
            }

            updateLocationsDictionary = true;
        }

        [EditAction]
        private void OnLocationsGeometryChanged(INetworkLocation networkLocation)
        {
            if (!Locations.IsAutoSorted) return;

            // if the geometry of a network location is changed (in the map) update sort of location arguments. 
            // Function does not support sorting by argument.
            // For value based or immutable argument types this is not an issue.
            // notes: checking for index change of networkLocation in Locations does not work
            // MultiDimensionalArrayHelper.GetInsertionIndex is unreliable since Locations.Values are not sorted
            var locationValues = GetValues(new VariableValueFilter<INetworkLocation>(Locations, new[] {networkLocation}));
            if (locationValues.Count <= 0) return;
            
            // NOTE [Bas]: locationValues can be empty in certain conditions, and Function.SetValues will then throw an ArgumentOutOfRangeException
            var values = new object[locationValues.Count];
            for (int i = 0; i < locationValues.Count; i++)
            {
                values[i] = locationValues[i];
            }
            var oldSorted = Locations.Values.IsAutoSorted;
            Locations.Values.IsAutoSorted = false;
            //IsAutoSorted is unreliable here and leads to wrong results, so disable it here
            RemoveValues(new VariableValueFilter<INetworkLocation>(Locations, new[] {networkLocation}));
            Locations.Values.IsAutoSorted = oldSorted; //re-enable
            SetValues(values, new VariableValueFilter<INetworkLocation>(Locations, new[] {networkLocation}));
            IsSorting = false;
        }

        [NoNotifyPropertyChange]
        public virtual bool IsSorting { get; private set; }

        public virtual event EventHandler<NotifyCollectionChangingEventArgs> NetworkCollectionChanged;

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

        [EditAction]
        protected virtual void UpdateSegments()
        {
            if (!initialized)
            {
                Initialize();
            }
            segmentsInitialized = true;
            NetworkCoverageHelper.UpdateSegments(this);
        }

        private void SubscribeEvents()
        {
            if (Locations != null)
            {
                Locations.ValuesChanged += LocationsValuesChanged;
                ((INotifyPropertyChanged) Locations).PropertyChanged += LocationsPropertyChanged;
            }
        }

        private void UnsubscribeEvents()
        {
            if (Locations != null)
            {
                Locations.ValuesChanged -= LocationsValuesChanged;
                ((INotifyPropertyChanged) Locations).PropertyChanged -= LocationsPropertyChanged;
            }
        }

        public override void Clear()
        {
            base.Clear();
            segmentsInitialized = false;
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
                HandleBranchSplit((BranchSplitAction) network.CurrentEditAction);
                updateLocationsDictionary = true;
                return;
            }
            if (Network.CurrentEditAction is BranchMergeAction)
            {
                HandleBranchMerge((BranchMergeAction) network.CurrentEditAction);
                updateLocationsDictionary = true;
                return;
            }
            if (Network.CurrentEditAction is BranchReverseAction)
            {
                HandleBranchReverse((BranchReverseAction) network.CurrentEditAction);
                updateLocationsDictionary = true;
                return;
            }
            if (Network.CurrentEditAction is BranchReorderAction)
            {
                HandleBranchReorder((BranchReorderAction) network.CurrentEditAction);
                updateLocationsDictionary = true;
                return;
            }
            throw new InvalidOperationException("Should not get here");
        }

        private void NetworkPropertyChanging(object sender, PropertyChangingEventArgs e)
        {
            if (Network == null)
            {
                return;
            } 

            if (networkIsInKnownEditAction)
                return;

            var branch = sender as IBranch;
            if (branch != null)
            {
                if (e.PropertyName == "Geometry" || e.PropertyName == "Length" || e.PropertyName == "IsLengthCustom")
                {
                    StoreBranchLength(branch);
                }
            }
        }

        private void NetworkPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (Network == null)
            {
                return;
            }
            // handle only IsEditing if the action is 'known'
            if (e.PropertyName == "IsEditing" && sender is INetwork)
            {
                OnNetworkEditActionStartingOrEnding();
            }

            if (networkIsInKnownEditAction)
                return;

            if (sender is IBranch)
            {
                if (e.PropertyName == "Geometry" ||
                    (e.PropertyName == "IsLengthCustom" && !((IBranch) sender).IsLengthCustom))
                {
                    // Update Network Locations for the branch based on the Geometry (also do this when switching to IsLengthCustom = false)
                    UpdateNetworkLocationsForBranch(sender as IBranch, true);
                    segmentsInitialized = false;
                    initialized = false;
                    StoreBranchLength((IBranch)sender);
                }
                else if (e.PropertyName == "Length" ||
                         (e.PropertyName == "IsLengthCustom" && ((IBranch) sender).IsLengthCustom))
                {
                    // Update Network Locations for the branch based on Length (also do this when switching to IsLengthCustom = true)
                    UpdateNetworkLocationsForBranch(sender as IBranch, false);
                    segmentsInitialized = false;
                    initialized = false;
                    StoreBranchLength((IBranch)sender);
                }
            }
        }

        [EditAction]
        private void OnNetworkEditActionStartingOrEnding()
        {
            if ((Network.CurrentEditAction is BranchReorderAction || 
                 Network.CurrentEditAction is BranchSplitAction ||
                 Network.CurrentEditAction is BranchMergeAction ||
                 Network.CurrentEditAction is BranchReverseAction))
            {
                //only 'monitor' known actions...other actions should be handled as normal changes
                if (Network.IsEditing)
                {
                    networkIsInKnownEditAction = true;
                }
                else
                {
                    try
                    {
                        if (Network.EditWasCancelled)
                        {
                            if (IsEditing)
                            {
                                CancelEdit();
                            }
                        }
                        else
                        {
                            BeginEdit(Network.CurrentEditAction);
                            DoEditAction();
                            EndEdit();
                        }
                    }
                    finally
                    {
                        // Ensure this is set to false when actions were executed normally or threw an exception
                        networkIsInKnownEditAction = false;
                    }
                }
            }
        }

        /// <summary>
        /// Reassigns locations on <see cref="BranchSplitAction.SplittedBranch"/> to <see cref="BranchSplitAction.NewBranch"/>
        /// and corrects chainage of those locations.
        /// </summary>
        /// <param name="currentEditAction">Assumes to be not null and containing proper data.</param>
        /// <remarks>This method can be override for custom logic in moving values / locations</remarks>
        [EditAction]
        protected virtual void UpdateValuesForBranchSplit(BranchSplitAction currentEditAction)
        {
            double splitAtChainage = currentEditAction.SplittedBranch.Length;

            IEnumerable<INetworkLocation> networkLocationsToMove =
                Locations.Values.Where(
                    nl => nl.Branch == currentEditAction.SplittedBranch && nl.Chainage >= splitAtChainage)
                    .ToList();

            var newBranchLength = currentEditAction.NewBranch.Length;
            foreach (var location in networkLocationsToMove)
            {
                location.Branch = currentEditAction.NewBranch;
                var chainage = location.Chainage - splitAtChainage;
                location.Chainage = BranchFeature.SnapChainage(newBranchLength, chainage);
            }
        }

        private void HandleBranchMerge(BranchMergeAction branchMergeAction)
        {
            IBranch extendedBranch = branchMergeAction.ExtendedBranch;
            IBranch removedBranch = branchMergeAction.RemovedBranch;

            var locationArray = Locations.Values;

            var locationsOnExtendedBranch =
                locationArray.Where(l => l.Branch == extendedBranch).ToList();

            var locationsOnRemovedBranch =
                locationArray.Where(l => l.Branch == removedBranch).ToList();

            //remove the locations of the branches
            var oldSorted = locationArray.IsAutoSorted;
            locationArray.IsAutoSorted = false; //ordering no longer reliable since a branch is already removed

            foreach (var networkLocation in locationsOnRemovedBranch.Concat(locationsOnExtendedBranch))
            {
                locationArray.Remove(networkLocation);
            }

            locationArray.IsAutoSorted = oldSorted;

            //reinitialize
            segmentsInitialized = false;
            initialized = false;
            StoreBranchLength(extendedBranch);
            RemoveBranchLength(removedBranch);
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
            StoreBranchLength(currentEditAction.SplittedBranch);
            StoreBranchLength(currentEditAction.NewBranch);
        }

        /// <summary>
        /// Compensate chainage of coverage locations defined on <paramref name="currentEditAction"/>
        /// for reversal of branch.
        /// </summary>
        /// <param name="currentEditAction">The reversed branch, assumed not to be null</param>
        private void HandleBranchReverse(BranchReverseAction currentEditAction)
        {
            var locationsOnReversedBranch =
                Locations.Values.Where(loc => loc.Branch == currentEditAction.ReversedBranch).ToList();
            if (locationsOnReversedBranch.Count != 0)
            {
                bool storeWasFiringEvents = false;
                if (Store.FireEvents)
                {
                    storeWasFiringEvents = true;
                    Store.FireEvents = false;
                }
                // Create lookup to put values back after compensation
                var functionLookup = (INetworkCoverage) Clone();

                //Remove all values for compensated locations before reversal, to prevent double combinations of NetworkLocations in 'Arguments'
                RemoveValues(new VariableValueFilter<INetworkLocation>(Locations, locationsOnReversedBranch));

                CompensateChainageAndResetComponents(locationsOnReversedBranch, functionLookup, -1,
                                                   currentEditAction.ReversedBranch.Length, 0, new object[] {});

                // disconnect and cleanup our clone
                functionLookup.Clear();
                functionLookup.Network = null;

                if (storeWasFiringEvents)
                {
                    Store.FireEvents = true;
                }
            }

            //reinitialize
            segmentsInitialized = false;
            initialized = false;
        }

        private IList<INetworkLocation> locationsOnReorderedBranch;

        /// <summary>
        /// Compensate chainage of coverage locations defined on <paramref name="action"/>
        /// for reversal of branch.
        /// </summary>
        /// <param name="action">The reversed branch, assumed not to be null</param>
        private void HandleBranchReorder(BranchReorderAction action)
        {
            if (IsTimeDependent)
            {
                return; // not supported
            }

            locationsOnReorderedBranch = Locations.Values.Where(loc => loc.Branch == action.Branch).ToList();
            
            if (locationsOnReorderedBranch.Count != 0)
            {
                var locationsFilter = new VariableValueFilter<INetworkLocation>(Locations, locationsOnReorderedBranch);

                // get values
                var values = GetValues(locationsFilter).Cast<object>().ToArray();

                if (values.Length == 0)
                {
                    return; // empty
                }

                Locations.IsAutoSorted = false; // otherwise Remove does nothing
                RemoveValues(locationsFilter);
                Locations.IsAutoSorted = true;
                
                // re-add locations, should add at correct locations
                SetValues(values, locationsFilter);
            }
        }

        /// <summary>
        /// Recusive algorithm that creates an itterative loop for each <c>Variable</c> in the coverage <see cref="Arguments"/> and
        /// resets the old <see cref="Component"/> onto the new <see cref="Arguments"/> for which the <see cref="locationsOnReversedBranch"/>
        /// Chainage has been compensated.
        /// </summary>
        /// <param name="locationsOnReversedBranch">The List of locations that have to be corrected</param>
        /// <param name="componentsLookup">A reference function to put values back into the NetworkCoverage</param>
        /// <param name="argumentsIndexToNetworkLocations">
        ///   Indicates the dimension index that corresponds to the dimension with NetworkLocations.
        ///   If the dimension is found in the recursion, the method itself will determine this index.
        ///   NOTE: Should be passed as "-1" if the dimension is unkown by caller.
        /// </param>
        /// <param name="branchlength">The length of the reversed branch</param>
        /// <param name="functionDimensionNumber">
        ///   Evaluate the recursive method for this dimension index.
        ///   NOTE: Should be passed as "0" when calling this method to start the recursion
        ///   NOTE: assumes <paramref name="functionDimensionNumber"/> is in range [-1,<paramref name="componentsLookup"/>.Arguments.Count]
        /// </param>
        /// <param name="arguments">
        ///   The function argument values, in order of the dimensions traversed
        ///   NOTE: Should be passed as an empty object array when calling this method to start the recursion
        /// </param>
        private void CompensateChainageAndResetComponents(IList<INetworkLocation> locationsOnReversedBranch,
                                                        IFunction componentsLookup, int argumentsIndexToNetworkLocations,
                                                        double branchlength, int functionDimensionNumber,
                                                        params object[] arguments)
        {
            if (functionDimensionNumber == componentsLookup.Arguments.Count)
            {
                // Stop criteria recursion: Reached last input dimension of function
                if (argumentsIndexToNetworkLocations == -1)
                    throw new InvalidOperationException(
                        String.Format(
                            "There should be network locations in function arguments defined when attempting to compensate chainages for reversal of a branch"));

                // Create copy of arguments, that become the new arguments set for the old values.
                var newArguments = arguments.Select(GetClone).ToArray();

                // Compensate Chainage
                if (locationsOnReversedBranch.Contains((NetworkLocation) newArguments[argumentsIndexToNetworkLocations]))
                {
                    double chainage = branchlength -
                                    ((NetworkLocation) newArguments[argumentsIndexToNetworkLocations]).Chainage;
                    ((NetworkLocation) newArguments[argumentsIndexToNetworkLocations]).Chainage =
                        BranchFeature.SnapChainage(branchlength, chainage);
                }

                this[newArguments] = componentsLookup.GetAllComponentValues(arguments);
            }
            else
            {
                // Determine NetworkLocations Argument dimension; argumentsIndexToNetworkLocations of '-1' -> Not set yet!
                // Assumes that the first NetworkLocations dimension is the one that needs to be compensated.
                if (argumentsIndexToNetworkLocations == -1 &&
                    componentsLookup.Arguments[functionDimensionNumber].ValueType == typeof (INetworkLocation))
                {
                    argumentsIndexToNetworkLocations = functionDimensionNumber;
                }

                // For all values in this Arguments dimension...
                foreach (var argumentValue in (argumentsIndexToNetworkLocations == functionDimensionNumber)
                                                  ? componentsLookup.Arguments[functionDimensionNumber].GetValues(
                                                      new IVariableFilter[]
                                                          {
                                                              new VariableValueFilter<INetworkLocation>(Locations,
                                                                                                        locationsOnReversedBranch)
                                                          })
                                                  : componentsLookup.Arguments[functionDimensionNumber].Values)
                {
                    // Construct function input arguments
                    var recursionArguments = new object[arguments.Length + 1];
                    for (int i = 0; i < arguments.Length; i++)
                    {
                        recursionArguments[i] = arguments[i];
                    }
                    recursionArguments[recursionArguments.Length - 1] = argumentValue;

                    CompensateChainageAndResetComponents(locationsOnReversedBranch, componentsLookup,
                                                       argumentsIndexToNetworkLocations, branchlength,
                                                       functionDimensionNumber + 1, recursionArguments);
                        // pass recursionArguments by value, not reference
                }
            }
        }

        private static object GetClone(object argumentValue)
        {
            if (argumentValue is ICloneable)
            {
                return ((ICloneable) argumentValue).Clone();
            }
            return argumentValue;
        }
        
        private void StoreBranchLength(IBranch branch)
        {
            branchLengths[branch] = branch.Length;
        }

        private void RemoveBranchLength(IBranch branch)
        {
            if (branchLengths.ContainsKey(branch))
            {
                branchLengths.Remove(branch);
            }
        }

        [EditAction]
        private void UpdateNetworkLocationsForBranch(IBranch branch, bool changedPropertyIsGeometry)
        {
            // Handle change in chainage in certain order to prevent possibility of a compensated location to be put on top and 
            // consuming an already existing location that is not yet compensated.
            var locations = branch.Length <= branchLengths[branch]
                                ? GetLocationsForBranch(branch).OrderBy(bf => bf.Chainage)
                                : GetLocationsForBranch(branch).OrderByDescending(bf => bf.Chainage);

            var needToChangeOffset = branchLengths[branch] != branch.Length &&
                          ((!branch.IsLengthCustom) && (changedPropertyIsGeometry) ||
                           (branch.IsLengthCustom) && (!changedPropertyIsGeometry));

            var factor = (branch.IsLengthCustom) ? branch.Geometry.Length / branch.Length : 1.0;

            updatingBranchGeometry = true;
            foreach (var location in locations)
            {
                if (needToChangeOffset)
                {
                    location.Chainage = BranchFeature.SnapChainage(branch.Length, location.Chainage * (branch.Length / branchLengths[branch]));
                }
                var coordinate = GeometryHelper.LineStringCoordinate((ILineString)branch.Geometry, BranchFeature.SnapChainage(branch.Geometry.Length, factor * location.Chainage));
                location.Geometry = GeometryHelper.SetCoordinate(location.Geometry, 0, coordinate);
            }
            updatingBranchGeometry = false;
        }

        //TODO:  get this out..should be handled as custom interpolator on Variable level. So that it works on individual components etc.
        private double GetInterpolatedValue(params IVariableFilter[] filters)
        {
            //Initialize();

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
                if (filters[0].Variable.ValueType != typeof (DateTime))
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

            var time = IsTimeDependent ? ((VariableValueFilter<DateTime>) (filters[timeFilterIndex])).Values[0] : (DateTime?) null;
            return InterpolateOnNetworkLocation(networkLocation, time);
        }

        private double InterpolateOnNetworkLocation(INetworkLocation networkLocation, DateTime? dateTime, IEnumerable<INetworkLocation> locations = null)
        {
            if (IsTimeDependent && dateTime == null)
            {
                throw new ArgumentNullException("No time specified for evaluating time dependent network coverage.");
            }

            if (Locations.ExtrapolationType != ExtrapolationType.Constant)
            {
                throw new NotSupportedException(
                    "Evaluation failed : currently only constant Extrapolation for locations is supported.");
            }

            double distanceDownStream;
            double distanceUpStream;
            var knownBranchLocations = locations ?? Locations.Values.Where(bf => bf.Branch == networkLocation.Branch).OrderBy(bf => bf.Chainage);
            var downstreamLocation = GetNearestLocationOnNetwork(networkLocation, true, knownBranchLocations, out distanceDownStream);
            var upstreamLocation = GetNearestLocationOnNetwork(networkLocation, false, knownBranchLocations, out distanceUpStream);

            if (downstreamLocation == null && upstreamLocation == null)
            {
                return DefaultValue;
            }

            // same value for entire branch or constant extrapolation:
            if (downstreamLocation == null)
            {
                return IsTimeDependent ? (double) this[dateTime, upstreamLocation] : (double) this[upstreamLocation];
            }
            if (upstreamLocation == null)
            {
                return IsTimeDependent ? (double) this[dateTime, downstreamLocation] : (double) this[downstreamLocation];
            }

            // interpolation:
            var downstreamValue = IsTimeDependent
                                      ? (double) this[dateTime, downstreamLocation]
                                      : (double) this[downstreamLocation];
            var upstreamValue = IsTimeDependent ? (double) this[dateTime, upstreamLocation] : (double) this[upstreamLocation];
            return InterpolateByType(-distanceUpStream, distanceDownStream, upstreamValue, downstreamValue, 0);
        }

        /// <summary>
        /// To get adjacent networklocations in the coverage.
        /// </summary>
        /// <param name="networkLocation">The networklocation for which to find adjacent points</param>
        /// <param name="downstream">When true, cross linkage nodes in the direction of the branch, and v.v. when false</param>
        /// <param name="knownBranchLocations"> </param>
        /// <param name="distance">The absolute distance between the two locations</param>
        /// <returns>The adjacent networklocation when resolved, null otherwise</returns>
        private INetworkLocation GetNearestLocationOnNetwork(INetworkLocation networkLocation, bool downstream, IEnumerable<INetworkLocation> knownBranchLocations, out double distance)
        {
            distance = -1.0;

            // first within branch
            var nextLocation = downstream
                                   ? knownBranchLocations.FirstOrDefault(b => b.Chainage > networkLocation.Chainage)
                                   : knownBranchLocations.LastOrDefault(b => b.Chainage < networkLocation.Chainage);
            if (nextLocation != null)
            {
                distance = Math.Abs(nextLocation.Chainage - networkLocation.Chainage);
                return nextLocation;
            }

            if (!interpolateAcrossNodes) return null;

            distance = downstream
                           ? networkLocation.Branch.Length - networkLocation.Chainage
                           : networkLocation.Chainage;

            // then across nodes
            var connectingNode = downstream
                                     ? networkLocation.Branch.Target
                                     : networkLocation.Branch.Source;
            var connectedBranch = GetBranchConnectedByOrderNr(networkLocation.Branch, connectingNode);
            if (connectedBranch == null) return null;

            var networkLocations = new List<INetworkLocation>(Locations.Values);
            while (connectedBranch != null)
            {
                downstream = connectedBranch.Source.Equals(connectingNode);
                var branchLocations = networkLocations.Where(bf => bf.Branch == connectedBranch).OrderBy(bf => bf.Chainage);
                nextLocation = downstream
                                   ? branchLocations.FirstOrDefault()
                                   : branchLocations.LastOrDefault();

                if (nextLocation != null)
                {
                    distance += downstream
                        ? nextLocation.Chainage
                        : connectedBranch.Length - nextLocation.Chainage;
                    return nextLocation;
                }

                // break to avoid loop:
                if (connectedBranch.Equals(networkLocation.Branch)) break;

                distance += connectedBranch.Length;
                connectingNode = downstream
                                     ? connectedBranch.Target
                                     : connectedBranch.Source;
                connectedBranch = GetBranchConnectedByOrderNr(connectedBranch, connectingNode);
            }

            return null;
        }

        private static IBranch GetBranchConnectedByOrderNr(IBranch branch, INode connectingNode)
        {
            if (branch.OrderNumber == -1) return null;
            
            var connectedBranches = connectingNode.OutgoingBranches.Concat(connectingNode.IncomingBranches);
            var branchesWithSameOrder = connectedBranches.Where(b => b.OrderNumber == branch.OrderNumber && !b.Equals(branch)).ToList();

            if (branchesWithSameOrder.Count > 1)
            {
                // More than 2 branches with identical order number connected
                // to a single node, no interpolation across node
                return null;
            }
            return branchesWithSameOrder.FirstOrDefault();
        }

        private double InterpolateByType(double leftPosition, double rightPosition, double leftValue, double rightValue,
                                   double evalPosition)
        {
            switch (Locations.InterpolationType)
            {
                case InterpolationType.Constant:
                    return InterpolateNearest(leftPosition, rightPosition, evalPosition, leftValue, rightValue);
                case InterpolationType.Linear:
                    return InterpolateLinear(leftPosition, rightPosition, evalPosition, leftValue, rightValue);
                case InterpolationType.None:
                    return DefaultValue;
                default:
                    throw new NotImplementedException();
            }
        }

        public INetworkLocation GetNearestNetworkLocation(ICoordinate coordinate)
        {
            return GetNearestNetworkLocation(coordinate, null);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="coordinate"></param>
        /// <param name="branchItMustBeOn">If a branch is given, the resulting location must lie on the branch</param>
        /// <returns></returns>
        private INetworkLocation GetNearestNetworkLocation(ICoordinate coordinate, IBranch branchItMustBeOn = null)
        {
            //TODO: speed up. this sucks when we have 100000 locations (write performance test!). 
            //TODO add a maximal distance otherwise return null
            double minDistance = double.MaxValue;
            INetworkLocation minLocation = null;
            foreach (INetworkLocation location in Locations.Values)
            {
                if (branchItMustBeOn != null && location.Branch != branchItMustBeOn)
                    continue;

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
                throw new ArgumentException("Please specify time filter to retrieve value from time related network coverage");


            return GetInterpolatedValue(new VariableValueFilter<INetworkLocation>(Locations, networkLocation));
        }

        public virtual double Evaluate(IBranchFeature branchFeature)
        {
            return Evaluate(new NetworkLocation(branchFeature.Branch, branchFeature.Chainage));
        }

        public virtual IList<double> EvaluateWithinBranch(IBranch branch, IOrderedEnumerable<double> chainageValues, IList<INetworkLocation> existingLocations)
        {
            if (!existingLocations.Any()) return chainageValues.Select(o => Evaluate(new NetworkLocation(branch, o))).ToList();

            var orderedLocations = existingLocations.OrderBy(bf => bf.Chainage).ToList();
            var minIndex = Locations.Values.IndexOf(orderedLocations.First());
            var maxIndex = minIndex + orderedLocations.Count - 1;
            var existingValues = base.GetValues<double>(new VariableIndexRangesFilter(Locations,new List<Pair<int, int>>(new[]{new Pair<int, int>(minIndex,maxIndex)})));
            
            var valueList = new List<double>();
            DateTime? time = null;
            //we might have a local filter
            if (IsTimeDependent && Filters.Count == 1 && Filters[0] is VariableValueFilter<DateTime>)
            {
                time = ((VariableValueFilter<DateTime>)Filters[0]).Values[0];
            }

            var leftmostChainage = orderedLocations.First().Chainage;
            var rightmostChainage = orderedLocations.Last().Chainage;
            int leftIndex = 0;

            foreach (var chainage in chainageValues)
            {
                // first extrapolate or interpolate across nodes
                if (chainage < leftmostChainage || chainage > rightmostChainage)
                {
                    valueList.Add(InterpolateOnNetworkLocation(new NetworkLocation(branch, chainage), time, orderedLocations));
                    continue;
                }

                // these are spot on
                if (Math.Abs(chainage - orderedLocations[leftIndex].Chainage) < BranchFeature.Epsilon)
                {
                    // match
                    valueList.Add(existingValues[leftIndex]);
                    continue;
                }

                // here we are for sure between two known locations,
                // find the index on the left:
                while (chainage > orderedLocations[leftIndex + 1].Chainage)
                {
                    if (leftIndex > orderedLocations.Count - 2)
                    {
                        // this should never happen!
                        throw new Exception("Error evaluating network coverage within a single branch");
                    }
                    ++leftIndex;
                }
                
                // being in between also means that leftIndex > 0
                valueList.Add(InterpolateByType(orderedLocations[leftIndex].Chainage,
                                                orderedLocations[leftIndex + 1].Chainage,
                                                existingValues[leftIndex], 
                                                existingValues[leftIndex + 1], 
                                                chainage));
            }

            return valueList;
        }
        
        private static double InterpolateNearest(double x0, double x1, double chainage, double y0, double y1)
        {
            double diff0 = Math.Abs(x0 - chainage);
            double diff1 = Math.Abs(x1 - chainage);

            return (diff0 < diff1 ? y0 : y1);
        }

        private static double InterpolateLinear(double x0, double x1, double x, double y0, double y1)
        {
            if (x < x0) return y0;
            if (x > x1) return y1;
            
            if (x1 > x0 + BranchFeature.Epsilon)
            {
                return y0 + (x - x0)*(y1 - y0)/(x1 - x0);
            }
            return y0;
        }

        public virtual double Evaluate(DateTime dateTime, INetworkLocation networkLocation)
        {
            if (!IsTimeDependent)
                throw new ArgumentException(
                    "Please do not specify time filter to retrieve value from time related network coverage");

            var times = Time.Values;

            if (times.Contains(dateTime))
            {
                return GetInterpolatedValue(new VariableValueFilter<DateTime>(Time, dateTime),
                                            new VariableValueFilter<INetworkLocation>(Locations, networkLocation));
            }
            else
            {
                //grab last time less than dateTime, filter, and call Evaluate on that
                var previousTime = times.LastOrDefault(t => t < dateTime);
                if (previousTime == default(DateTime))
                {
                    throw new ArgumentException("Invalid time value for network coverage evaluate: before known times");
                }

                return ((INetworkCoverage) FilterTime(previousTime)).Evaluate(networkLocation);
            }
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

            clone.Attributes = new Dictionary<string, string>();
            foreach (var attributeKey in Attributes.Keys)
            {
                clone.Attributes.Add(attributeKey, Attributes[attributeKey]);
            }

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

        /// <summary>
        /// Sets existing network locations to a new network. Only works if new network is a 'mirror' of the existing one.
        /// Replacement is done based on index.
        /// </summary>
        /// <param name="networkCoverage"></param>
        /// <param name="network"></param>
        public static void ReplaceNetworkForClone(INetwork network, INetworkCoverage networkCoverage)
        {
            if (network.Branches.Count != networkCoverage.Network.Branches.Count)
            {
                throw new InvalidOperationException("Unable to set a new network in the coverage. Number of branches differs from old network");
            }
            for (int i = 0; i < networkCoverage.Locations.Values.Count; i++)
            {
                var branchIndex = networkCoverage.Network.Branches.IndexOf(networkCoverage.Locations.Values[i].Branch);
                networkCoverage.Locations.Values[i].Branch = network.Branches[branchIndex];
            }
            //set the network into the coverage
            networkCoverage.Network = network;
        }

        protected bool InterpolateAcrossNodes 
        {
            get { return interpolateAcrossNodes; }
            set { interpolateAcrossNodes = value; }
        }
    }
}