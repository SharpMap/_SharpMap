using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DelftTools.Functions;
using DelftTools.Functions.Filters;
using DelftTools.Functions.Generic;
using DelftTools.Utils.Collections;
using DelftTools.Utils.Collections.Generic;
using DelftTools.Utils.Data;
using DelftTools.Utils.IO;
using DelftTools.Utils.Reflection;
using GeoAPI.Extensions.Coverages;
using GeoAPI.Geometries;
using GisSharpBlog.NetTopologySuite.Geometries;
using log4net;
using NetTopologySuite.Extensions.Coverages;
using OSGeo.GDAL;
using SharpMap.Utilities;
using io = System.IO;

namespace SharpMap.Extensions.Data.Providers
{
    /// <summary>
    /// Store which always contains functions in a fixed order:
    /// x
    /// y
    /// values
    /// time (optional)
    /// </summary>
    public class GdalFunctionStore : Unique<long>, IFunctionStore, IFileBased
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(GdalFunctionStore));
        private Dataset gdalDataset;
        private string path;
        private IEventedList<IFunction> functions;
        private IMultiDimensionalArray xValues;
        private IMultiDimensionalArray yValues;

        private static IRegularGridCoverage emptyGrid = new RegularGridCoverage();
        private static IEventedList<IFunction> emptyGridFunctions = new EventedList<IFunction>();

        private GdalState state;

        enum GdalState
        {
            Closed,
            DefineMode,
            Initializing,
            Defined
        }

        public override string ToString()
        {
            return "Gdal function store.";
        }

        static GdalFunctionStore()
        {
            emptyGridFunctions.Add(emptyGrid);
            emptyGridFunctions.Add(emptyGrid.X);
            emptyGridFunctions.Add(emptyGrid.Y);
            emptyGridFunctions.AddRange(emptyGrid.Components.Cast<IFunction>());
        }

        public GdalFunctionStore()
        {
            state = GdalState.Closed;
            TypeConverters = new List<ITypeConverter>();
            Functions = new EventedList<IFunction>();
        }

        ~GdalFunctionStore()
        {
            Close();
        }

        private void Functions_CollectionChanging(object sender, NotifyCollectionChangingEventArgs e)
        {
            if(!AutoOpen())
            {
                return;
            }

            if ((e.Action == NotifyCollectionChangeAction.Add || e.Action == NotifyCollectionChangeAction.Replace)
                && e.Item is IRegularGridCoverage)
            {
                var grid = (IRegularGridCoverage)e.Item;

                var driverName = GdalHelper.GetDriverName(path);

                RegisterGdal();

                using (var gdalDriver = Gdal.GetDriverByName(driverName))
                {
                    var gdalType = GdalHelper.GetGdalDataType(gdalDriver, grid.Components[0].ValueType);
                    var gdalvariableValue = GdalHelper.GetVariableForDataType(gdalType);// TODO: strange logic, use supported GDAL types here (as .NET types) instead
                    var type = gdalvariableValue.ValueType;

                    if (type != grid.Components[0].ValueType)
                    {
                        throw new InvalidOperationException(string.Format("Value type {0} is not supported by GDAL driver", grid.Components[0].ValueType));
                    }

                }
            }
        }

        private static bool gdalInitialized;

        private static void RegisterGdal()
        {
            if (!gdalInitialized)
            {
                Gdal.AllRegister(); //register gdal drivers

                var maxGdalCacheSize = Gdal.GetCacheMax();
                log.DebugFormat("Adding max GDAL cache size to GC memory pressure: {0}", maxGdalCacheSize);
                GC.AddMemoryPressure(maxGdalCacheSize);
                gdalInitialized = true;
            }
        }

        private void Functions_CollectionChanged(object sender, NotifyCollectionChangingEventArgs e)
        {
            if(!AutoOpen())
            {
                return;
            }

            if (state == GdalState.Initializing) return;
            var function = (IFunction)e.Item;

            switch (e.Action)
            {
                case NotifyCollectionChangeAction.Add:
                    AddFunction(function);
                    function.Store = this;
                    break;
                case NotifyCollectionChangeAction.Remove:
                    break;

                case NotifyCollectionChangeAction.Replace:
                    throw new NotSupportedException();
            }
        }


        #region IFileBased Members

        public virtual string Path
        {
            get { return path; }
            set { path = value; }
        }

        public void CreateNew(string path)
        {
            if (IsOpen)
            {
                Close();
            }
            if (File.Exists(path))
            {
                File.Delete(path);
            }
            this.path = path;

            //Functions.Clear(); // clear grid
            functions.Clear(); // clear grid
            IsOpen = true;
        }

        public void Close()
        {
            if (gdalDataset != null)
            {
                gdalDataset.Dispose(); // dont forget to call dispose for preventing memory leaks}
                gdalDataset = null;
            }

            // Prevent calling GC.Collect() at all costs. It is very slow; closing project with many gdal stores
            //// make sure all references to gdal objects are freed to avoid lock on files
            //// GC.Collect();
            IsOpen = false;
        }

        private bool isOpening;

        public void Open(string path)
        {
            if (isOpening)
                return;

            isOpening = true;

            try
            {
                this.path = path;

                if (!File.Exists(path))
                {
                    throw new FileNotFoundException(String.Format("The following file does not exist: {0}", path));
                }

                log.DebugFormat("Opening grid using GDAL: {0}", path);

                state = GdalState.Initializing;
                OpenGdalDataset(path);
                //if (Functions.Count == 0)
                if (functions.Count == 0)
                {
                    CreateSchemaFromDataset();
                }
                state = GdalState.Defined;
                IsOpen = true;
            }
            finally // makes sure flag is reset
            {
                isOpening = false;
            }
        }

        public bool IsOpen { get; private set; }


        public void SwitchTo(string newPath)
        {
            Close();
            Open(newPath);
        }

        public void CopyTo(string newPath)
        {
            var sourceDir = io.Path.GetDirectoryName(path);
            var targetDir = io.Path.GetDirectoryName(newPath);
            var extension = io.Path.GetExtension(path).ToLower();

            File.Copy(path, newPath);

            var oldFileName = io.Path.GetFileNameWithoutExtension(path);
            var newFileName = io.Path.GetFileNameWithoutExtension(newPath);

            switch (extension)
            {
                case ".bil":
                    var hdrFileName = io.Path.Combine(sourceDir, oldFileName + ".hdr");
                    File.Copy(hdrFileName, io.Path.Combine(targetDir, newFileName + ".hdr"));
                    break;
            }
        }

        public void Delete()
        {
            if(IsOpen)
            {
                Close();
            }

            File.Delete(path);

            log.WarnFormat("TODO: delete all sattelite GDAL files (projection, headers, etc.)");
        }

        public IEnumerable<string> Paths
        {
            get
            {
                if (Path != null)
                    yield return Path;
            }
            //TODO: all sattelite GDAL files (projection, headers, etc.)
        }
        #endregion

        private void CreateSchemaFromDataset()
        {
            var grid = CreateRegularGridCoverage(gdalDataset);

            grid.Store = this;

            // add grid, grid components and arguments to the list of functions managed by the current store
            functions.Add(grid);
            functions.Add(grid.X);
            functions.Add(grid.Y);
            functions.AddRange(grid.Components.Cast<IFunction>());
        }

        private static IRegularGridCoverage CreateRegularGridCoverage(Dataset dataset)
        {
            var gridName = System.IO.Path.GetFileNameWithoutExtension(dataset.GetFileList()[0]);
            IRegularGridCoverage grid = new RegularGridCoverage { Name = gridName };

            grid.X.FixedSize = dataset.RasterXSize;
            grid.Y.FixedSize = dataset.RasterYSize;

            //set Grid geometry
            var geometryFactory = new GeometryFactory(); // TODO: add CRS!
            var extents = GdalHelper.GetExtents(dataset);
            grid.Geometry = geometryFactory.ToGeometry(extents);

            // replace grid components by the components found in GDAL dataset
            grid.Components.Clear();
            var gridComponents = GetDataSetComponentVariables(dataset);
            grid.Components.AddRange(gridComponents);

            return grid;
        }

        static Envelope emptyEnvelope = new Envelope();

        public virtual IEnvelope GetExtents()
        {
            if(!AutoOpen())
            {
                return emptyEnvelope;
            }

            return GdalHelper.GetExtents(gdalDataset);
        }


        #region IFunctionStore Members
        
        // INotifyCollectionChange
        public virtual event NotifyCollectionChangedEventHandler CollectionChanged;
        public virtual event NotifyCollectionChangingEventHandler CollectionChanging;
        
        bool INotifyCollectionChange.HasParentIsCheckedInItems { get; set; }

        public virtual bool SkipChildItemEventBubbling { get; set; }

        // ICloneable

        public object Clone()
        {
            var clone = new GdalFunctionStore { FireEvents = FireEvents };
            
            try
            {
                clone.Open(Path);

                // replace all function names in the cloned store by the current function names (GDAL store may not remember them)
                for (var i = 0; i < Functions.Count; i++)
                {
                    clone.Functions[i].Name = Functions[i].Name;
                }
            }
            catch(Exception e)
            {
                // swallow exception because we can be seen as IFeatureProvider
                log.Error("Can't open file: " + Path, e);
            }            

            return clone;
        }

        public IEventedList<IFunction> Functions
        {
            get
            {
                AutoOpen();
                return functions;
            }
            set
            {
                if (functions != null)
                {
                    functions.CollectionChanged -= Functions_CollectionChanged;
                    functions.CollectionChanging -= Functions_CollectionChanging;
                }
                functions = value;
                if (functions != null)
                {
                    functions.CollectionChanging += Functions_CollectionChanging;
                    functions.CollectionChanged += Functions_CollectionChanged;
                }
            }
        }

        /// <summary>
        /// Updates the component values for a regular grid.
        /// </summary>
        /// <param name="variable"></param>
        /// <param name="values"></param>
        /// <param name="filters"></param>
        public void SetVariableValues<T>(IVariable variable, IEnumerable<T> values, params IVariableFilter[] filters)
        {
            if(!AutoOpen())
            {
                return; // nothing to do
            }

            if ((!Grid.Components.Contains(variable)))
            {
                throw new ArgumentOutOfRangeException("unsupported function in SetValues call arguments: " + variable);
            }

            int componentIndex = Grid.Components.IndexOf(variable);
            SetGdalValues(componentIndex, values.ToArray(), filters);
        }

        /// <summary>
        /// Use netcdf driver to create datastore with the right dimensions
        /// </summary>
        public IMultiDimensionalArray GetVariableValues(IVariable variable, params IVariableFilter[] filters)
        {
            if(!AutoOpen())
            {
                return emptyGrid.Components[0].GetValues();
            }

            log.DebugFormat("Getting values for variable {0}", variable.Name);

            //Non Gdal Values
            if (variable.Name.Equals("x"))
            {
                //return xValues ?? new MultiDimensionalArray<double>();
                var geoTrans = new double[6];
                gdalDataset.GetGeoTransform(geoTrans);
                var transform = new RegularGridGeoTransform(geoTrans);
                var sizeX = gdalDataset.RasterXSize;
                var sizeY = gdalDataset.RasterYSize;
                var deltaX = transform.HorizontalPixelResolution;
                var deltaY = transform.VerticalPixelResolution;
                var origin = new Coordinate(transform.Left, transform.Top - deltaY * sizeY);

                return GetValuesArray(deltaX, sizeX, origin.X);
            }
            if (variable.Name.Equals("y"))
            {
                //return yValues ?? new MultiDimensionalArray<double>();
                var geoTrans = new double[6];
                gdalDataset.GetGeoTransform(geoTrans);
                var transform = new RegularGridGeoTransform(geoTrans);
                var sizeY = gdalDataset.RasterYSize;
                var deltaY = transform.VerticalPixelResolution;
                var origin = new Coordinate(transform.Left, transform.Top - deltaY * sizeY);

                return GetValuesArray(deltaY, sizeY, origin.Y);
            }

            if (!Grid.Components.Contains(variable))
            {
                throw new NotSupportedException("Variable not part of grid:" + variable.Name);
            }

            //TODO: switch GetVariableValues and GetVariableValues<T> so that the non-generic calls the generic version.
            switch (variable.ValueType.ToString())
            {
                case "System.Byte":
                    return GetGdalValues<byte>(Grid.Components.IndexOf(variable), filters);
                case "System.Integer":
                case "System.Int32":
                    return GetGdalValues<int>(Grid.Components.IndexOf(variable), filters);
                case "System.Int16":
                    return GetGdalValues<Int16>(Grid.Components.IndexOf(variable), filters);
                case "System.UInt32":
                    return GetGdalValues<UInt32>(Grid.Components.IndexOf(variable), filters);
                case "System.UInt16":
                    return GetGdalValues<UInt16>(Grid.Components.IndexOf(variable), filters);
                case "System.Float":
                case "System.Single":
                    return GetGdalValues<float>(Grid.Components.IndexOf(variable), filters);
                default:
                    return GetGdalValues<double>(Grid.Components.IndexOf(variable), filters);
            }
        }

        public IMultiDimensionalArray<T> GetVariableValues<T>(IVariable function, params IVariableFilter[] filters)
        {
            if(!AutoOpen())
            {
                return emptyGrid.GetValues<T>();
            }

            if (filters.Length == 0 && Grid.Components.Contains(function))
            {
                return new LazyMultiDimensionalArray<T>(
                    () => (IMultiDimensionalArray<T>)GetVariableValues(function),
                    () => GetGridComponentValuesCount(function));
            }

            return (IMultiDimensionalArray<T>)GetVariableValues(function, filters);
        }

        /// <summary>
        /// Gets the count of the component specified using a 'special' Gdal call.
        /// </summary>
        /// <param name="function"></param>
        /// <returns></returns>
        private int GetGridComponentValuesCount(IVariable function)
        {
            if(!AutoOpen())
            {
                return 0;
            }

            //does this cut it for timedepend stuff?...do we have this in Gdal?
            var count = gdalDataset.RasterXSize*gdalDataset.RasterYSize;
            return count;
            /*using (var band = gdalDataset.GetRasterBand(1))
            {
                gdalDataset.get        
            }*/
        }

        public void RemoveFunctionValues(IFunction function, params IVariableValueFilter[] filters)
        {
            throw new NotImplementedException("Write (band, x, y) values directly to file (if supported)");
        }

        public virtual bool SupportsPartialRemove { get { return false; } }

        public virtual event EventHandler<FunctionValuesChangingEventArgs> BeforeFunctionValuesChanged;
        public virtual event EventHandler<FunctionValuesChangingEventArgs> FunctionValuesChanged;
        public virtual event EventHandler<FunctionValuesChangingEventArgs> FunctionValuesChanging;

        public virtual IList<ITypeConverter> TypeConverters { get; set; }

        public virtual bool FireEvents
        {
            get;
            set;
        }

        public void UpdateVariableSize(IVariable variable)
        {
            //throw new NotImplementedException();
        }

        public T GetMaxValue<T>(IVariable variable)
        {
            if(!AutoOpen())
            {
                return default(T);
            }

            using (var band = gdalDataset.GetRasterBand(1))
            {
                double maximum;
                int hasValue;
                band.GetMaximum(out maximum, out hasValue);
                if (hasValue == 0)
                {
                    double[] values = new double[2];
                    band.ComputeRasterMinMax(values, 0);
                    maximum = values[1];
                }

                return (T)Convert.ChangeType(maximum, typeof(T));
            }
        }

        public T GetMinValue<T>(IVariable variable)
        {
            if (!AutoOpen())
            {
                return default(T);
            }

            using (var band = gdalDataset.GetRasterBand(1))
            {
                double minimum;
                int hasValue;
                band.GetMinimum(out minimum, out hasValue);
                if (hasValue == 0)
                {
                    double[] values = new double[2];
                    band.ComputeRasterMinMax(values, 0);
                    minimum = values[0];
                }
                return (T)Convert.ChangeType(minimum, typeof(T));

            }
        }

        public virtual void CacheVariable(IVariable variable)
        {
            //throw new NotImplementedException();
        }

        public virtual bool DisableCaching { get; set; }

        #endregion

        private bool AutoOpen()
        {
            try
            {
                if (!IsOpen && !string.IsNullOrEmpty(Path))
                {
                    Open(Path);
                }
            }
            catch (Exception e)
            {
                log.Error("Can't open raster file (GDAL): " + Path, e);
                return false;
            }

            return true;
        }

        private IRegularGridCoverage grid;

        public IRegularGridCoverage Grid
        {
            get
            {
                if(!AutoOpen())
                {
                    return emptyGrid;
                }

                if (Functions.Count == 0)
                {
                    return grid ?? (grid = new RegularGridCoverage());
                }

                return (IRegularGridCoverage)Functions[0];
            }
        }

        struct RasterBoundaries
        {
            public int StartX;
            public int StartY;
            public int WidthX;
            public int WidthY;
        }

        public Dataset GdalDataset
        {
            get { return gdalDataset; }
        }


        public virtual void AddIndependendVariableValues<T>(IVariable variable, IEnumerable<T> values)
        {
            //GDAL is not intereset in x and y.
            //SetVariableValues(variable,values);
        }

        private void OpenGdalDataset(string path)
        {
            if (IsOpen)
            {
                Close();
            }

            RegisterGdal();

            CheckAndFixHeaders(path);

            gdalDataset = Gdal.Open(path.ToLower(), Access.GA_Update);

            SetDriver(path);

/*
            var geoTrans = new double[6];
            gdalDataset.GetGeoTransform(geoTrans);
            var transform = new RegularGridGeoTransform(geoTrans);

            int sizeX = gdalDataset.RasterXSize;
            int sizeY = gdalDataset.RasterYSize;

            //  double deltaX = sizeX/gdalDataset.RasterCount;
            double deltaX = transform.HorizontalPixelResolution;
            double deltaY = transform.VerticalPixelResolution;


            var origin = new Coordinate(transform.Left, transform.Top - deltaY * sizeY);
            /yValues = GetValuesArray(deltaY, sizeY, origin.Y);
            /xValues = GetValuesArray(deltaX, sizeX, origin.X);
*/

        }

        private void SetDriver(string path)
        {
            using (var driver = gdalDataset.GetDriver())
            {
                if (!GdalHelper.IsCreateSupported(driver))
                {
                    log.WarnFormat("Datasource is readonly: {0}", path);
                    return;
                }
                //cannot directly create dataset. check if dataset can be created by copying from another set
                if (GdalHelper.IsCreateCopySupported(driver))
                {
                    //TODO: fix this criteria
                    if (GetDatasetSize() < 1000000)
                    {
                        log.Debug("Using in-memory driver to facilitate writing to datasource");
                        //log.WarnFormat("Using in memory-driver with large dataset can yield unexpected results.");
                        using (var gdalDriver = Gdal.GetDriverByName("MEM"))
                        {
                            var oldGdalDataSet = gdalDataset;
                            gdalDataset = gdalDriver.CreateCopy(path, gdalDataset, 0, new string[] { }, null, null);
                            oldGdalDataSet.Dispose();
                        }    
                    }
                    
                }
            }
        }

        public long GetDatasetSize()
        {
            //TODO: fix this so it works OK..so a 4 bytes per cell per band assumption is here
            long rasterSize = gdalDataset.RasterXSize*gdalDataset.RasterYSize;
            return gdalDataset.RasterCount*rasterSize*4L;
        }

        private static IEnumerable<IVariable> GetDataSetComponentVariables(Dataset gdalDataset)
        {
            IList<IVariable> components = new List<IVariable>();

            for (var i = 1; i <= gdalDataset.RasterCount; i++)
            {
                using (var band = gdalDataset.GetRasterBand(i))
                {
                    var dataType = band.DataType;
                    var componentVariableName = "Raster" + i;
                    var componentVariable = GdalHelper.GetVariableForDataType(dataType);
                    componentVariable.Name = componentVariableName;
                    int hasNoDataValue;
                    double noDataValue;
                    band.GetNoDataValue(out noDataValue, out hasNoDataValue);

                    //ToDo check this logic versus specs of hdr definition for bil files.
                    if (noDataValue < 0 && (dataType == DataType.GDT_UInt32 || dataType == DataType.GDT_UInt16))
                    {
                        noDataValue = Math.Abs(noDataValue);
                        log.DebugFormat("Nodata value changed from a negative to a positive value");
                    }

                    if (hasNoDataValue > 0)
                    {
                        componentVariable.NoDataValues = new[] { noDataValue };
                    }

                    componentVariable.FixedSize = gdalDataset.RasterXSize * gdalDataset.RasterYSize;

                    components.Add(componentVariable);
                }
            }

            return components;
        }

        private IMultiDimensionalArray<T> GetGdalValues<T>(int componentIndex, params IVariableFilter[] variableFilters)
        {
            bool scale = false;
            int startX = 0, startY = 0, widthX = Grid.SizeX, widthY = Grid.SizeY;
            int rasterBandIndex = componentIndex + 1; //1 based index

            var sizeX = Grid.SizeX;
            var sizeY = Grid.SizeY;

            if (variableFilters.Length > 0)
            {
                foreach (IVariableFilter filter in variableFilters)
                {
                    if (filter is VariableAggregationFilter)
                    {
                        var sampleFilter = filter as VariableAggregationFilter;
                        if (sampleFilter.Variable == Grid.X)
                        {

                            startX = sampleFilter.MinIndex;
                            widthX = sampleFilter.Count;
                            sizeX = sampleFilter.MaxIndex - sampleFilter.MinIndex + 1;
                        }
                        if (sampleFilter.Variable == Grid.Y)
                        {
                            startY = Grid.SizeY - sampleFilter.MaxIndex - 1;
                            widthY = sampleFilter.Count;
                            sizeY = sampleFilter.MaxIndex - sampleFilter.MinIndex + 1;
                        }
                        scale = true;
                        continue;
                    }

                    if (filter is IVariableValueFilter)
                    {
                        var variableValueFilter = filter as IVariableValueFilter;
                        if (variableValueFilter.Values.Count > 1)
                        {
                            throw new NotSupportedException(
                                "Multiple values for VariableValueFilter not supported by GDalFunctionStore");
                        }

                        if (filter.Variable == Grid.X)
                        {
                            startX = Grid.X.Values.IndexOf(variableValueFilter.Values[0]);
                            widthX = 1;
                        }
                        if (filter.Variable == Grid.Y)
                        {
                            //origin of our system is lower left corner, origin of gdal is upper left corner.
                            startY = Grid.SizeY - Grid.Y.Values.IndexOf(variableValueFilter.Values[0]) - 1;
                            widthY = 1;
                        }
                        continue;
                    }

                    if (filter is VariableIndexRangesFilter)
                    {
                        var rangesFilter = ((VariableIndexRangesFilter)filter);
                        if (filter.Variable == Grid.X)
                        {
                            startX = rangesFilter.IndexRanges[0].First;
                            widthX = rangesFilter.IndexRanges[0].Second - startX + 1;
                        }
                        if (filter.Variable == Grid.Y)
                        {
                            // rangesFilter.IndexRanges[0].First;
                            startY = Grid.SizeY - rangesFilter.IndexRanges[0].Second - 1;
                            widthY = Grid.SizeY - startY - rangesFilter.IndexRanges[0].First;
                        }
                        continue;
                    }

                    if (filter is VariableIndexRangeFilter)
                    {
                        var variableIndexRangeFilter = filter as VariableIndexRangeFilter;
                        if (filter.Variable == Grid.X)
                        {
                            startX = variableIndexRangeFilter.MinIndex;
                            widthX = 1 + variableIndexRangeFilter.MaxIndex - variableIndexRangeFilter.MinIndex;
                        }
                        if (filter.Variable == Grid.Y)
                        {

                            startY = Grid.SizeY - variableIndexRangeFilter.MaxIndex - 1;
                            widthY = Grid.SizeY - startY - variableIndexRangeFilter.MinIndex;
                        }
                        continue;
                    }
                }
            }
            
            //create a generic MDA of [xSize,ySize] with values of dataset.
            var values = scale
                             ? GdalHelper.GetValuesForBand<T>(gdalDataset, rasterBandIndex, startX, startY, sizeX, sizeY, widthX, widthY)
                             : GdalHelper.GetValuesForBand<T>(gdalDataset, rasterBandIndex, startX, startY, widthX, widthY);

            var array = (MultiDimensionalArray<T>)TypeUtils.CreateGeneric( typeof(MultiDimensionalArray<>),
                                                                           typeof(T), true, true,
                                                                           Grid.Components[componentIndex].DefaultValue,
                                                                           new List<T>(),
                                                                           new []{0}
                                                                           );
            //backdoor for performance (prevent creating clone):
            array.Shape = new[] {widthX, widthY};
            array.SetValues(values);  
            
            return array;
        }

        private void SetGdalValues<T>(int componentIndex, T[] values, params IVariableFilter[] variableFilters)
        {
            var rasterBoundaries = GetRasterBoundaries(variableFilters);

            var rasterBandIndex = componentIndex + 1; //1 based index

            GdalHelper.SetValuesForBand(gdalDataset, rasterBandIndex, rasterBoundaries.StartX, rasterBoundaries.StartY, rasterBoundaries.WidthX, rasterBoundaries.WidthY, values);
            gdalDataset.FlushCache();

            //update dataset on filesystem by copying in-memory dataset
            var inMemoryDriver = gdalDataset.GetDriver();
            if (!GdalHelper.IsInMemoryDriver(inMemoryDriver))
            {
                return;
            }
            Driver targetDatasetDriver = GetTargetDatasetDriver(path);
            Dataset outputDataset = GetOutputDataset(gdalDataset, inMemoryDriver, targetDatasetDriver, Functions.OfType<IRegularGridCoverage>().FirstOrDefault(), rasterBandIndex, rasterBoundaries, values);

            WriteGdalDatasetToFile(path, outputDataset);
        }

        private static void WriteGdalDatasetToFile(string path, Dataset gdalDataset)
        {
            log.Debug("Recreating file and copying data from in-memory datasource.");

            Driver targetDatasetDriver = GetTargetDatasetDriver(path);
            using (targetDatasetDriver)
            {
                if (GdalHelper.IsCreateCopySupported(targetDatasetDriver))
                {
                    targetDatasetDriver.CreateCopy(path, gdalDataset, 0, new string[] { }, null, null)
                        .Dispose();
                }
            }
            //GC.Collect();
        }

        private static Dataset GetOutputDataset<T>(Dataset gdalDataset, Driver gdalDriver, Driver targetDatasetDriver, IRegularGridCoverage gridCoverage, int rasterBandIndex, RasterBoundaries rasterBoundaries, T[] values)
        {
            Dataset outputDataset = gdalDataset;
            if (targetDatasetDriver.ShortName == "PCRaster")
            {
                // Convert the in mem dataset to a pc rasterBoundaries compatible one...

                Type valueType = gridCoverage.Components[0].ValueType;

                DataType dataType =
                    GdalHelper.GetGdalDataType(targetDatasetDriver, valueType);

                outputDataset = gdalDriver.Create(gridCoverage.Name, gdalDataset.RasterXSize,
                                                  gdalDataset.RasterYSize,
                                                  gridCoverage.Components.Count, dataType, new string[] { });

                GdalHelper.SetValuesForBand(outputDataset, rasterBandIndex, rasterBoundaries.StartX, rasterBoundaries.StartY, rasterBoundaries.WidthX, rasterBoundaries.WidthY, values);
            }
            return outputDataset;
        }

        private static Driver GetTargetDatasetDriver(string path)
        {
            var driverName = GdalHelper.GetDriverName(path);
            RegisterGdal();
            return Gdal.GetDriverByName(driverName);
        }

        private RasterBoundaries GetRasterBoundaries(IEnumerable<IVariableFilter> variableFilters)
        {
            RasterBoundaries rasterBoundaries = new RasterBoundaries { WidthX = Grid.SizeX, WidthY = Grid.SizeY };

            foreach (var filter in variableFilters)
            {
                var variableValueFilter = filter as IVariableValueFilter;

                if (variableValueFilter != null)
                {
                    if (filter.Variable == Grid.X)
                    {

                        rasterBoundaries.StartX = Grid.X.Values.IndexOf(variableValueFilter.Values[0]);
                        rasterBoundaries.WidthX = 1;
                    }
                    if (filter.Variable == Grid.Y)
                    {

                        rasterBoundaries.StartY = Grid.Y.Values.IndexOf(variableValueFilter.Values[0]);
                        rasterBoundaries.WidthY = 1;
                    }
                }

                var variableIndexRangeFilter = filter as VariableIndexRangeFilter;

                if (variableIndexRangeFilter != null)
                {
                    if (filter.Variable == Grid.X)
                    {

                        rasterBoundaries.StartX = variableIndexRangeFilter.MinIndex;
                        rasterBoundaries.WidthX = 1 + variableIndexRangeFilter.MaxIndex - variableIndexRangeFilter.MinIndex;
                    }
                    if (filter.Variable == Grid.Y)
                    {
                        rasterBoundaries.StartY = variableIndexRangeFilter.MinIndex;
                        rasterBoundaries.WidthY = 1 + variableIndexRangeFilter.MaxIndex - variableIndexRangeFilter.MinIndex;
                    }
                }
            }

            return rasterBoundaries;
        }

        /// <summary>
        /// Use store to copy values from function to datasource and connect store to function.
        /// </summary>
        /// <param name="function"></param>
        private void AddFunction(IFunction function)
        {
            if (!AutoOpen())
            {
                return;
            }

            if (!(function is IRegularGridCoverage)) return;

            if (state == GdalState.Initializing)
            {
                return;
            }

            var addedCoverage = (IRegularGridCoverage)function;

            //xValues = addedCoverage.X.Values;
            //yValues = addedCoverage.Y.Values;

            //Close(); //clean up resources used by current dataset if any.

            var driverName = GdalHelper.GetDriverName(path);

            RegisterGdal();

            using (Driver gdalDriver = Gdal.GetDriverByName(driverName))
            {
                VerifyDriverIsValid(gdalDriver);

                if (addedCoverage.Store is GdalFunctionStore)
                {
                    //CopyCurrentDatasetFromAddedCoverage((GdalFunctionStore)addedCoverage.Store, gdalDriver);
                    var store = (GdalFunctionStore)addedCoverage.Store;
                    CopyCurrentDatasetFromAddedCoverage(store, gdalDriver);
                    return;
                }

                //verify if all components are of the same type
                VerifyComponentTypesAreSame(addedCoverage);

                //substitute driver by in-memory driver in case driver is read-only
                SubstituteDriverByInMemIfReadonly(addedCoverage, gdalDriver);


                if (gdalDataset == null)
                {
                    throw new IOException(String.Format("Cannot open file: {0}", path));
                }
                {
                    var transform = new RegularGridGeoTransform(addedCoverage.Origin.X,
                                                                addedCoverage.Origin.Y + addedCoverage.DeltaY * addedCoverage.SizeY,
                                                                addedCoverage.DeltaX,
                                                                addedCoverage.DeltaY);


                    //todo check: lowerleft corner or upperleft corner. (currently using upperleft corner as reference point)
                    gdalDataset.SetGeoTransform(transform.TransForm);

                    // add agruments
                    functions.Add(addedCoverage.X);
                    functions.Add(addedCoverage.Y);

                    if(addedCoverage.X.Parent != null)
                    {
                        // reset parent, since function will have this store as a parent
                        addedCoverage.X.Parent = null;
                        addedCoverage.Y.Parent = null;
                    }

                    //copy components to this store
                    for (int i = 0; i < addedCoverage.Components.Count; i++)
                    {
                        IMultiDimensionalArray componentValues = addedCoverage.Components[i].Values;

                        // reset parent, since function will have this store as a parent
                        if(addedCoverage.Components[i].Parent != null)
                        {
                            addedCoverage.Components[i].Parent = null;
                        }

                        functions.Add(addedCoverage.Components[i]);
                        addedCoverage.Components[i].SetValues(componentValues);
                    }
                    gdalDataset.FlushCache();
                }


                if (gdalDataset == null)
                {
                    log.ErrorFormat("No GdalDataset available to write/read {0}.", path);
                }
            }
        }

        private void SubstituteDriverByInMemIfReadonly(IRegularGridCoverage addedCoverage, Driver gdalDriver)
        {
            var canCreate = gdalDriver.GetMetadataItem("DCAP_CREATE", null) == "YES";
            //cannot create use mem driver.
            if (!canCreate)
            {
                using (var inMemoryDriver = Gdal.GetDriverByName("MEM"))
                {
                    DataType dataType =
                        GdalHelper.GetGdalDataType(inMemoryDriver, addedCoverage.Components[0].ValueType);

                    if (dataType.Equals(DataType.GDT_Unknown))
                    {
                        throw new NotSupportedException(
                            String.Format("The datatype {0} cannot be saved to this kind of file: {1}",
                                          addedCoverage.Components[0].ValueType, System.IO.Path.GetExtension(path)));
                    }

                    gdalDataset = inMemoryDriver.Create(path, addedCoverage.SizeX, addedCoverage.SizeY,
                                                        addedCoverage.Components.Count, dataType,
                                                        new string[] { });

                }
            }
            else
            {
                DataType dataType =
                    GdalHelper.GetGdalDataType(gdalDriver, addedCoverage.Components[0].ValueType);

                if (dataType.Equals(DataType.GDT_Unknown))
                {
                    throw new NotSupportedException(
                        String.Format("The datatype {0} cannot be saved to this kind of file: {1}",
                                      addedCoverage.Components[0].ValueType, System.IO.Path.GetExtension(path)));
                }

                gdalDataset = gdalDriver.Create(path, addedCoverage.SizeX, addedCoverage.SizeY,
                                                addedCoverage.Components.Count, dataType,
                                                new string[] { });

            }
        }

        private void CopyCurrentDatasetFromAddedCoverage(GdalFunctionStore gdalFunctionStore, Driver gdalDriver)
        {
            gdalDataset = gdalDriver.CreateCopy(path, gdalFunctionStore.gdalDataset, 0, new string[] { }, null, "Copy");
            //substitute driver by in-memory driver in case driver is read-only
            if (!GdalHelper.IsCreateSupported(gdalDriver))
            {
                using (var inMemoryDriver = Gdal.GetDriverByName("MEM"))
                {
                    var oldGdalDataSet = gdalDataset;
                    //create in-memory dataset with a copy of the original data.
                    gdalDataset = inMemoryDriver.CreateCopy(path, gdalFunctionStore.gdalDataset, 0, new string[] { },
                                                            null, null);
                    oldGdalDataSet.Dispose();
                }
            }
        }

        private void VerifyDriverIsValid(Driver gdalDriver)
        {
            if (gdalDriver == null)
            {
                throw new IOException(String.Format("Cannot find suitable driver to write to this kind of output file: '{0}'", path));
            }

            if (String.Compare(gdalDriver.GetMetadataItem("DCAP_CREATE", null), "YES") != 0)
            {

                //verify if driver supports creation by copying from another dataset.
                if (String.Compare("YES", gdalDriver.GetMetadataItem("DCAP_CREATECOPY", null)) != 0)
                {
                    //driver does not support writing nor create copy methods to write to this file
                    throw new IOException(String.Format("Cannot find suitable driver to write to this kind of output file: '{0}'", path));
                }
            }
        }

        private static IMultiDimensionalArray GetValuesArray(double delta, int size, double offset)
        {
            IMultiDimensionalArray result = new MultiDimensionalArray<double>(size);
            for (var j = 0; j < size; j++)
            {
                result[j] = j * delta + offset;
            }
            return result;
        }

        private static void CheckAndFixHeaders(string path)
        {
            if (path.EndsWith(".bil", StringComparison.OrdinalIgnoreCase))
            {
                GdalHelper.CheckBilHdrFileForPixelTypeField(path);
            }
            if (path.EndsWith(".asc", StringComparison.OrdinalIgnoreCase))
            {
                GdalHelper.CheckAndFixAscHeader(path);
            }
        }

        private static void VerifyComponentTypesAreSame(IFunction sourceGridCoverage)
        {
            var distinctValueTypes = sourceGridCoverage.Components
                .Select(c => c.ValueType).Distinct();

            if (distinctValueTypes.Count() != 1)
            {
                throw new NotImplementedException(
                    "All of the components in the dataset should be of the same type");
            }
        }
    }
}