using System;
using System.Collections.Generic;
using System.Linq;
using DelftTools.Functions;
using DelftTools.Functions.Filters;
using DelftTools.Functions.Generic;
using DelftTools.Utils.Collections;
using DelftTools.Utils.IO;
using DelftTools.Utils.Reflection;
using GeoAPI.Extensions.Coverages;
using GeoAPI.Geometries;
using GisSharpBlog.NetTopologySuite.Geometries;
using log4net;

namespace NetTopologySuite.Extensions.Coverages
{
    public class RegularGridCoverage : Coverage, IRegularGridCoverage
    {
        private new const string DefaultName = "regular grid";

        //this stuff is NetCdf? specific
        private const string StandardName = "standard_name";
        private const string StandardNameX = "projection_x_coordinate";
        private const string StandardNameY = "projection_y_coordinate";
        private const string CoordinateName = "coordinates";

        private static readonly ILog log = LogManager.GetLogger(typeof(RegularGridCoverage));
        private int sizeX;
        private int sizeY;
        private double deltaX;
        private double deltaY;

        //flag to indicate changes in argument collection and argument value collections
        private bool isDirty;

        private ICoordinate origin;
        private IGeometry geometry;

        public RegularGridCoverage()
            : this(DefaultName)
        {
        }

        /// <summary>
        /// Initializes gridcoverage with a default size of 1 pixel.
        /// </summary>
        /// <param name="name"></param>
        public RegularGridCoverage(string name)
            : this(0, 0, 0, 0) // makes sure that grid has always X, Y (not-null)
        {
            Name = name;
        }

        public RegularGridCoverage(IRegularGridCoverage grid)
        {
            if (grid.Components.Count > 1) { throw new NotImplementedException("Copy constructor not implemented for grids with more than one component"); }
            if (grid.Components[0].NoDataValues.Count > 1) { throw new NotImplementedException("Copy constructor not implemented for component with multiple nodata values"); }


            Name = grid.Name;
            var y = new Variable<double>("y", grid.SizeY);
            y.Attributes[StandardName] = StandardNameY;
            y.FixedSize = 0; // HACK: move it to NetCDF store
            Arguments.Add(y);

            var x = new Variable<double>("x", grid.SizeX);
            x.Attributes[StandardName] = StandardNameX;
            x.FixedSize = 0; // HACK: move it to NetCDF store
            Arguments.Add(x);


            IVariable variable = grid.Components[0].Clone();
            //clear arguments because they will be added when the component is added to the function
            variable.Arguments.Clear();
            Components.Add(variable);



            CollectionChanged += RegularGridCoverage_CollectionChanged;
            ValuesChanged += RegularGridCoverage_ValuesChanged;
            IsTimeDependent = grid.IsTimeDependent;
            // update sizes of the arguments
            Resize(grid.SizeX, grid.SizeY, grid.DeltaX, grid.DeltaY, new Coordinate(grid.Origin.X, grid.Origin.Y));
        }

        /// <param name="sizeX">Number of X values</param>
        /// <param name="sizeY">Number of Y values</param>
        /// <param name="deltaX">StepSize in X</param>
        /// <param name="deltaY">StepSize in Y</param>
        public RegularGridCoverage(int sizeX, int sizeY, double deltaX, double deltaY)
            : this(sizeX, sizeY, deltaX, deltaY, 0, 0, typeof(double))
        {
        }

        public RegularGridCoverage(int sizeX, int sizeY, double deltaX, double deltaY, Type componentValueType)
            : this(sizeX, sizeY, deltaX, deltaY, 0, 0, componentValueType)
        {
        }

        /// <param name="sizeX">Number of X values</param>
        /// <param name="sizeY">Number of Y values</param>
        /// <param name="deltaX">StepSize in X</param>
        /// <param name="deltaY">StepSize in Y</param>
        public RegularGridCoverage(int sizeX, int sizeY, double deltaX, double deltaY, double offSetX, double offSetY)
            : this(sizeX, sizeY, deltaX, deltaY, offSetX, offSetY, typeof(double))
        {
        }

        /// <param name="sizeX">Number of X values</param>
        /// <param name="sizeY">Number of Y values</param>
        /// <param name="deltaX">StepSize in X</param>
        /// <param name="deltaY">StepSize in Y</param>
        public RegularGridCoverage(int sizeX, int sizeY, double deltaX, double deltaY, double offSetX, double offSetY, Type componentValueType)
        {
            this.Name = "Regular_Grid";
            var y = new Variable<double>("y", sizeY);
            y.Attributes[StandardName] = StandardNameY;
            y.FixedSize = 0; // HACK: move it to NetCDF store
            Arguments.Add(y);

            var x = new Variable<double>("x", sizeX);
            x.Attributes[StandardName] = StandardNameX;
            x.FixedSize = 0; // HACK: move it to NetCDF store
            Arguments.Add(x);


            //Arguments.CollectionChanged += RegularGrid_Arguments_CollectionChanged;

            //Components.CollectionChanged += RegularGrid_Components_CollectionChanged;

            var component = (IVariable)TypeUtils.CreateGeneric(typeof (Variable<>), componentValueType, "value");
            Components.Add(component);

            CollectionChanged += RegularGridCoverage_CollectionChanged;
            ValuesChanged += RegularGridCoverage_ValuesChanged;



            // update sizes of the arguments
            Resize(sizeX, sizeY, deltaX, deltaY, new Coordinate(offSetX, offSetY));
        }

        private void RegularGrid_Arguments_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            isDirty = true;
        }


        /// <summary>
        /// Make sure data is loaded before it is accessed.
        /// IFileBased
        /// </summary>
        private void SynchronizeStore()
        {
            if (!(Store is IFileBased)) 
                return;
            IFileBased fileBased = (IFileBased)Store;
            if ((!fileBased.IsOpen) && (fileBased.Path != null))
            {
                fileBased.Open();
            }
        }

        public override DelftTools.Utils.Collections.Generic.IEventedList<IVariable> Arguments
        {
            get
            {
                SynchronizeStore();
                return base.Arguments;
            }
            set
            {
                if (Arguments != null)
                {
                    Arguments.CollectionChanged -= RegularGrid_Arguments_CollectionChanged;
                }
                base.Arguments = value;
                if (Arguments != null)
                {
                    Arguments.CollectionChanged += RegularGrid_Arguments_CollectionChanged;
                }
            }
        }

        public override DelftTools.Utils.Collections.Generic.IEventedList<IVariable> Components
        {
            get
            {
                SynchronizeStore();
                return base.Components;
            }
            set
            {
                if (Components != null)
                {
                    Components.CollectionChanged -= RegularGrid_Components_CollectionChanged;
                }
                base.Components = value;
                if (Components != null)
                {
                    Components.CollectionChanged += RegularGrid_Components_CollectionChanged;
                }
            }
        }

        private void RegularGridCoverage_ValuesChanged(object sender, FunctionValuesChangedEventArgs e)
        {
            isDirty = true;
        }

        /// <summary>
        /// Listen to changes in collections of components and arguments.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void RegularGridCoverage_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            isDirty = true;
        }


        private void RegularGrid_Components_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Add)
            {
                var component = (IVariable)e.Item;
                component.Attributes[CoordinateName] = IsTimeDependent ? "time y x" : "y x";
            }
            isDirty = true;
        }

        public double Rotation { get; set; }


        public ICoordinate Origin
        {
            get
            {
                if (isDirty) UpdateGridGeometryAttributes();
                return origin;
            }
        }

        /// <summary>
        /// Gridcell size in x- or horizontal direction
        /// </summary>
        public double DeltaX
        {
            get
            {
                if (isDirty) UpdateGridGeometryAttributes();
                return deltaX;
            }
        }

        /// <summary>
        /// Updates grid size, stepsize, origin and geometry based on argument values (x and y).
        /// </summary>
        private void UpdateGridGeometryAttributes()
        {
            SynchronizeStore();
            sizeX = X.Values.Count;
            sizeY = Y.Values.Count;


            deltaY = sizeY > 1 ? Y.Values[1] - Y.Values[0] : 0;
            deltaX = sizeX > 1 ? X.Values[1] - X.Values[0] : 0;
            origin = sizeX > 0 && sizeY > 0 ? new Coordinate(X.Values[0], Y.Values[0]) : new Coordinate();

            var coordinates = new ICoordinate[5];

            coordinates[0] = new Coordinate(origin.X, origin.Y + deltaY * sizeY);
            coordinates[1] = new Coordinate(origin.X + deltaX * sizeX, origin.Y + deltaY * sizeY);
            coordinates[2] = new Coordinate(origin.X + deltaX * sizeX, origin.Y);
            coordinates[3] = new Coordinate(origin.X, origin.Y);
            coordinates[4] = coordinates[0];

            geometry = new Polygon(new LinearRing(coordinates));
            isDirty = false;
        }

        /// <summary>
        /// Gridcell size in y- or vertical direction
        /// </summary>
        public double DeltaY
        {
            get
            {
                if (isDirty) UpdateGridGeometryAttributes();
                return deltaY;
            }
        }

        /// <summary>
        /// Number of gridcells in x- or horizontal direction
        /// </summary>
        public int SizeX
        {
            get
            {
                if (isDirty) UpdateGridGeometryAttributes();
                return sizeX;
            }
        }

        /// <summary>
        /// Number of gridcells in y- or vertical direction
        /// </summary>
        public int SizeY
        {
            get
            {
                if (isDirty) UpdateGridGeometryAttributes();
                return sizeY;
            }
        }


        public override object Evaluate(ICoordinate coordinate)
        {
            return GetApproximatedValue(
                new VariableValueFilter<double>(X, coordinate.X),
                new VariableValueFilter<double>(Y, coordinate.Y)
                );
        }

        /// <summary>
        /// Evaluates value of coverage at coordinate.
        /// </summary>
        /// <param name="coordinate"></param>
        /// <returns></returns>
        public override T Evaluate<T>(ICoordinate coordinate)
        {
            return GetApproximatedValue<T>(
                new VariableValueFilter<double>(X, coordinate.X),
                new VariableValueFilter<double>(Y, coordinate.Y)
                );
        }

        /// <summary>
        /// Evaluates value of coverage at coordinate.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public override T Evaluate<T>(double x, double y)
        {
            return GetApproximatedValue<T>(
                new VariableValueFilter<double>(X, x),
                new VariableValueFilter<double>(Y, y)
                );
        }

        private object GetApproximatedValue(params IVariableFilter[] filters)
        {
            if (filters.Length != 2)
            {
                throw new ArgumentOutOfRangeException("filters", "Please specify x and y values");
            }

            var doubleValueFilters = filters.OfType<VariableValueFilter<double>>();
            var x = doubleValueFilters.First(f => f.Variable.Name.Equals("x")).Values[0];
            var y = doubleValueFilters.First(f => f.Variable.Name.Equals("y")).Values[0];

            // select nearest value
            if (!Geometry.EnvelopeInternal.Intersects(x, y))
            {
                return Components[0].DefaultValue;
            }

            var indexX = (int)Math.Floor((x - Origin.X) / DeltaX);

            var indexY = (int)Math.Floor((y - Origin.Y) / DeltaY);

            return Store.GetVariableValues(Components[0],
                                              new VariableValueFilter<double>(X, X.Values[indexX]),
                                              new VariableValueFilter<double>(Y, Y.Values[indexY])
                )[0];
        }

        /// <summary>
        /// Returns a function with all values at a location
        /// Currently no support for interpolation or extrapolation.
        /// </summary>
        /// <param name="coordinate"></param>
        /// <returns></returns>
        public override IFunction GetTimeSeries(ICoordinate coordinate)
        {
            // note Evaluate(coordinate) will only give first value
            if (!X.Values.Contains(coordinate.X))
            {
                throw new ArgumentOutOfRangeException("coordinate",
                                                      string.Format("No values available for {0} at x = {1}", Name,
                                                                    coordinate.X));
            }
            if (!Y.Values.Contains(coordinate.Y))
            {
                throw new ArgumentOutOfRangeException("coordinate",
                                                      string.Format("No values available for {0} at y = {1}", Name,
                                                                    coordinate.X));
            }
            IFunction filteredFunction = Filter(
                new VariableValueFilter<double>(X, new[] { coordinate.X }),
                new VariableValueFilter<double>(Y, new[] { coordinate.Y }),
                new VariableReduceFilter(X),
                new VariableReduceFilter(Y));
            filteredFunction.Name = string.Format("{0} at {1}, {2}", Name, coordinate.X, coordinate.Y);
            return filteredFunction;
        }

        private T GetApproximatedValue<T>(params IVariableFilter[] filters) where T : IComparable

        {
            if (filters.Length != 2)
            {
                throw new ArgumentOutOfRangeException("filters", "Please specify x and y values");
            }

            var x = filters.OfType<VariableValueFilter<double>>().First(f => f.Variable.Name.Equals("x")).Values[0];
            var y = filters.OfType<VariableValueFilter<double>>().First(f => f.Variable.Name.Equals("y")).Values[0];

            // select nearest value
            if (!Geometry.EnvelopeInternal.Intersects(x, y))
            {
                return (T)Convert.ChangeType(Components[0].DefaultValue, typeof(T));
            }

            var indexX = (int)((x - Origin.X) / DeltaX);

            var indexY = (int)((y - Origin.Y) / DeltaY);
            //TODO: use component filters here??
            return Store.GetVariableValues<T>(Components[0],
                                              new VariableValueFilter<double>(X, X.Values[indexX]),
                                              new VariableValueFilter<double>(Y, Y.Values[indexY])
                )[new int[Arguments.Count]];
        }

        public override IMultiDimensionalArray<T> GetValues<T>(params IVariableFilter[] filters)
        {
            //check we need to interpolate in time
            if (IsTimeDependent)
            {
                var timeFilter = (IVariableValueFilter) filters.FirstOrDefault(f => f is IVariableValueFilter && f.Variable == Time);
                if (timeFilter == null)
                    return base.GetValues<T>(filters);

                var timeArgumentValue = (DateTime)timeFilter.Values[0];
                //exact value match return Base.GetValues()
                if (Time.Values.Contains(timeArgumentValue))
                    return base.GetValues<T>(filters);

                if (filters.Count() > 1)
                {
                    throw new ArgumentException("Time-interpolation is not possible with more than 1 filter.");
                }
                
                //check extrapolation
                var needToExtrapolate = timeArgumentValue < Time.MinValue || timeArgumentValue > Time.MaxValue;
                if (needToExtrapolate)
                {
                    return GetTimeExtrapolatedValues<T>(timeArgumentValue, filters);
                }

                //check interpolation
                var needToInterpolate = Time.MinValue < timeArgumentValue && timeArgumentValue < Time.MaxValue;
                if (needToInterpolate)
                {
                    return GetTimeInterpolatedValues<T>(timeArgumentValue, filters);
                }
            }

            return base.GetValues<T>(filters);
        }

        private IMultiDimensionalArray<T> GetTimeInterpolatedValues<T>(DateTime timeArgumentValue, IVariableFilter[] filters) where T : IComparable
        {
           
            if (Time.InterpolationType == ApproximationType.None)
            {
                throw new ArgumentOutOfRangeException("Interpolation is disabled");
            }
            
            if (Time.InterpolationType == ApproximationType.Constant)
            {               
                //get the nearest t smaller than the t given t;
                var time = (DateTime) FunctionHelper.GetLastValueSmallerThan(timeArgumentValue, Time.Values);
                var newFilters = (IVariableFilter[]) filters.Clone();
                
                ReplaceTimeFilter(time, newFilters);

                return base.GetValues<T>(newFilters);
            }
            throw new NotImplementedException();
        }


        private IMultiDimensionalArray<T> GetTimeExtrapolatedValues<T>(DateTime timeArgumentValue, IVariableFilter[] filters) where T : IComparable
        {
            if (Time.ExtrapolationType == ApproximationType.None)
            {
                throw new ArgumentOutOfRangeException("Extrapolation is disabled");
            }

            if (Time.ExtrapolationType == ApproximationType.Constant)
            {
                var time = (timeArgumentValue > Time.MaxValue) ? Time.MaxValue : Time.MinValue;
                var newFilters = (IVariableFilter[])filters.Clone();
                
                ReplaceTimeFilter(time, newFilters);

                return base.GetValues<T>(newFilters); 
            }

            throw new NotImplementedException();
        }

        private void ReplaceTimeFilter(DateTime time, IVariableFilter[] filters)
        {
            var timeFilter = (IVariableValueFilter)filters.FirstOrDefault(f => f is IVariableValueFilter && f.Variable == Time);
            var timeFilterIndex = filters.ToList().IndexOf(timeFilter);

            filters[timeFilterIndex] = new VariableValueFilter<DateTime>(Time, time);
        }

        /// <summary>
        /// RealWorld X Raster values
        /// </summary>
        public IVariable<double> X
        {
            get { return (IVariable<double>)Arguments[IsTimeDependent ? 2 : 1]; }
        }
        public override void Clear()
        {
            isDirty = true;
            sizeX = 0;
            sizeY = 0;

            base.Clear();
        }
        /// <summary>
        /// RealWorld Y Raster values
        /// </summary>
        public IVariable<double> Y
        {
            get { return (IVariable<double>)Arguments[IsTimeDependent ? 1 : 0]; }
        }


        /// <summary>
        /// Rectangle representing boundary of the grid.
        /// </summary>
        public override IGeometry Geometry
        {
            get
            {
                if (isDirty) UpdateGridGeometryAttributes();

                return geometry;
            }
            set
            {
                log.WarnFormat("Cannot set geometry for regular gridcoverage since it is defined by x and y values",
                               "value");
            }
        }

        public IRegularGridCoverage FilterAsRegularGridCoverage(params IVariableFilter[] filters)
        {
            return (IRegularGridCoverage)base.Filter(filters);
        }

        /// <summary>
        /// Resizes and recreates the grid
        /// </summary>
        /// <param name="sizeX">Number of X values</param>
        /// <param name="sizeY">Number of Y values</param>
        /// <param name="deltaX">StepSize in X</param>
        /// <param name="deltaY">StepSize in Y</param>
        public void Resize(int sizeX, int sizeY, double deltaX, double deltaY)
        {
            Resize(sizeX, sizeY, deltaX, deltaY, new Coordinate(0, 0));
        }

        public void Resize(int sizeX, int sizeY, double deltaX, double deltaY, ICoordinate origin)
        {
            Resize(sizeX, sizeY, deltaX, deltaY, origin, true);
        }
        /// <summary>
        /// Resizes and recreates the grid
        /// </summary>
        /// <param name="sizeX">Number of X values</param>
        /// <param name="sizeY">Number of Y values</param>
        /// <param name="deltaX">StepSize in X</param>
        /// <param name="deltaY">StepSize in Y</param>
        /// <param name="origin">Offset in real world coordinates </param>
        public void Resize(int sizeX, int sizeY, double deltaX, double deltaY, ICoordinate origin, bool setDefaultComponentValues)
        {
            //clear values from arguments and components
            Clear();
            Store.FireEvents = false;
            //detach the components from argument changes
            foreach (IVariable component in Components)
            {
                component.Detach();
            }

            var yValues = new double[sizeY];
            for (var j = 0; j < sizeY; j++)
            {
                yValues[j] = j * deltaY + origin.Y;
            }

            var xValues = new double[sizeX];
            for (var i = 0; i < sizeX; i++)
            {
                xValues[i] = i * deltaX + origin.X;
            }

            X.FixedSize = sizeX;
            Y.FixedSize = sizeY;

            foreach (IVariable component in Components)
            {
                component.FixedSize = sizeX * sizeY;
            }

            Y.SetValues(yValues);
            X.SetValues(xValues);


            //reattach components to arguments
            foreach (IVariable component in Components)
            {
                component.Attach();
            }

            // HACK: set values..MOVE to the store..
            if (setDefaultComponentValues)
            {
                foreach (IVariable component in Components)
                {
                    IEnumerable<object> enumerable = Enumerable.Repeat(component.DefaultValue, component.FixedSize);
                    component.SetValues(enumerable);
                }
            }
            Store.FireEvents = true;
            isDirty = true;
        }
        /// <summary>
        /// Aggregate along given variable.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="TAccumulate"></typeparam>
        /// <param name="v"></param>
        /// <param name="startValue"></param>
        /// <param name="func"></param>
        /// <returns></returns>
        public override IFunction Aggregate<T, TAccumulate>(IVariable v, TAccumulate startValue, Func<TAccumulate, T, TAccumulate> func)
        {
            IRegularGridCoverage resultCoverage = new RegularGridCoverage(SizeX, SizeY, DeltaX, DeltaY);
            resultCoverage.Components.Clear();

            var variable = (IVariable)TypeUtils.CreateGeneric(typeof(Variable<>), typeof(TAccumulate));
            resultCoverage.Components.Add(variable);

            var values = resultCoverage.Components[0].Values;

            //integrate values over time
            int i = 0, j = 0;
            foreach (var x in X.Values)
            {
                foreach (var y in Y.Values)
                {
                    var values1D = (IList<T>)Components[0].GetValues(
                        new VariableValueFilter<double>(X, new[] { x }),
                        new VariableValueFilter<double>(Y, new[] { y }),
                        new VariableReduceFilter(X),
                        new VariableReduceFilter(Y)
                        );

                    TAccumulate value = values1D.Aggregate(startValue, func);

                    values[j, i] = value;
                    j++;
                }
                i++;
                j = 0;
            }

            return resultCoverage;


            // if (v is DateTime > .....)
        }
        
        /// <summary>
        /// Returns the RegularGridCoverageCell x, y is in.
        /// eg.       x=0  x=1
        /// y=1
        /// y=0
        /// GetRegularGridCoverageCellAtPosition( 0.0,  0.0) will return cell 0, 0
        /// GetRegularGridCoverageCellAtPosition( 0.5,  0.0) will return cell 0, 0
        /// GetRegularGridCoverageCellAtPosition( 0.0,  0.5) will return cell 0, 0
        /// GetRegularGridCoverageCellAtPosition( 1.0,  1.0) will return cell 1, 1
        /// 
        /// GetRegularGridCoverageCellAtPosition( 1.5,  0.0) will return cell 1, 0
        /// GetRegularGridCoverageCellAtPosition( 2.0,  0.0) will throw an OutOfRangeException
        /// GetRegularGridCoverageCellAtPosition(-1.0,  0.0) will throw an OutOfRangeException
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public IRegularGridCoverageCell GetRegularGridCoverageCellAtPosition(double x, double y)
        {
            if (x < Origin.X)
            {
                throw new ArgumentOutOfRangeException("x", string.Format("Regular grid coverage {0}; no data available at x={1}", Name, x));
            }
            if (y < Origin.Y)
            {
                throw new ArgumentOutOfRangeException("y", string.Format("Regular grid coverage {0}; no data available at y={1}", Name, y));
            }
            if (x >= Origin.X + (SizeX * DeltaX))
            {
                throw new ArgumentOutOfRangeException("x", string.Format("Regular grid coverage {0}; no data available at x={1}", Name, x));
            }
            if (y >= Origin.Y + (SizeY * DeltaY))
            {
                throw new ArgumentOutOfRangeException("y", string.Format("Regular grid coverage {0}; no data available at y={1}", Name, y));
            }

            double newX = X.Values[(int)Math.Floor((x - Origin.X) / DeltaX)];
            double newY = Y.Values[(int)Math.Floor((y - Origin.Y) / DeltaY)];

            return new RegularGridCoverageCell
            {
                X = newX,
                Y = newY,
                RegularGridCoverage = this
            };
        }
    }
}