// Copyright 2005, 2006 - Morten Nielsen (www.iter.dk)
//
// This file is part of SharpMap.
// SharpMap is free software; you can redistribute it and/or modify
// it under the terms of the GNU Lesser General Public License as published by
// the Free Software Foundation; either version 2 of the License, or
// (at your option) any later version.
// 
// SharpMap is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU Lesser General Public License for more details.

// You should have received a copy of the GNU Lesser General Public License
// along with SharpMap; if not, write to the Free Software
// Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307  USA 

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Web;
using System.Web.Caching;
using DelftTools.Utils.Data;
using DelftTools.Utils.IO;
using GeoAPI.CoordinateSystems;
using GeoAPI.Extensions.Feature;
using GeoAPI.Geometries;
using log4net;
using SharpMap.Converters.Geometries;
using SharpMap.Converters.WellKnownText;
using SharpMap.Utilities.SpatialIndexing;

namespace SharpMap.Data.Providers
{
    /// <summary>
    /// Shapefile dataprovider
    /// </summary>
    /// <remarks>
    /// <para>The ShapeFile provider is used for accessing ESRI ShapeFiles. The ShapeFile should at least contain the
    /// [filename].shp, [filename].idx, and if feature-data is to be used, also [filename].dbf file.</para>
    /// <para>The first time the ShapeFile is accessed, SharpMap will automatically create a spatial index
    /// of the shp-file, and save it as [filename].shp.sidx. If you change or update the contents of the .shp file,
    /// delete the .sidx file to force SharpMap to rebuilt it. In web applications, the index will automatically
    /// be cached to memory for faster access, so to reload the index, you will need to restart the web application
    /// as well.</para>
    /// <para>
    /// M and Z values in a shapefile is ignored by SharpMap.
    /// </para>
    /// </remarks>
    /// <example>
    /// Adding a datasource to a layer:
    /// <code lang="C#">
    /// SharpMap.Layers.VectorLayer myLayer = new SharpMap.Layers.VectorLayer("My layer");
    /// myLayer.DataSource = new SharpMap.Data.Providers.ShapeFile(@"C:\data\MyShapeData.shp");
    /// </code>
    /// </example>
    public class ShapeFile : Unique<long>, IFileBasedFeatureProvider
    {
        #region Delegates

        /// <summary>
        /// Filter Delegate Method
        /// </summary>
        /// <remarks>
        /// The FilterMethod delegate is used for applying a method that filters data from the dataset.
        /// The method should return 'true' if the feature should be included and false if not.
        /// <para>See the <see cref="FilterDelegate"/> property for more info</para>
        /// </remarks>
        /// <seealso cref="FilterDelegate"/>
        /// <param name="dr"><see cref="SharpMap.Data.FeatureDataRow"/> to test on</param>
        /// <returns>true if this feature should be included, false if it should be filtered</returns>
        public delegate bool FilterMethod(FeatureDataRow dr);

        #endregion

        private static readonly ILog log = LogManager.GetLogger(typeof (ShapeFile));
        private ICoordinateSystem _CoordinateSystem;
        private bool _CoordsysReadFromFile = false;
        private IEnvelope _Envelope;
        private int _FeatureCount;
        private bool _FileBasedIndex;
        private string path;
        private FilterMethod _FilterDelegate;
        private bool _IsOpen;
        private ShapeType _ShapeType;
        private int _SRID = -1;
        private BinaryReader brShapeFile;
        private BinaryReader brShapeIndex;
        private DbaseReader dbaseFile;
        private FileStream fsShapeFile;
        private FileStream fsShapeIndex;
        
        /// <summary>
        /// Tree used for fast query of data
        /// </summary>
        private QuadTree tree;

        public ShapeFile()
        {
            _FileBasedIndex = false;
        }

        /// <summary>
        /// Initializes a ShapeFile DataProvider without a file-based spatial index.
        /// </summary>
        /// <param name="filename">Path to shape file</param>
        public ShapeFile(string filename) : this(filename, false)
        {
        }

        /// <summary>
        /// Initializes a ShapeFile DataProvider.
        /// </summary>
        /// <remarks>
        /// <para>If FileBasedIndex is true, the spatial index will be read from a local copy. If it doesn't exist,
        /// it will be generated and saved to [filename] + '.sidx'.</para>
        /// <para>Using a file-based index is especially recommended for ASP.NET applications which will speed up
        /// start-up time when the cache has been emptied.
        /// </para>
        /// </remarks>
        /// <param name="filename">Path to shape file</param>
        /// <param name="fileBasedIndex">Use file-based spatial index</param>
        public ShapeFile(string filename, bool fileBasedIndex)
        {
            Open(filename);
            _FileBasedIndex = fileBasedIndex;
        }

        /// <summary>
        /// Gets or sets the coordinate system of the ShapeFile. If a shapefile has 
        /// a corresponding [filename].prj file containing a Well-Known Text 
        /// description of the coordinate system this will automatically be read.
        /// If this is not the case, the coordinate system will default to null.
        /// </summary>
        /// <exception cref="ApplicationException">An exception is thrown if the coordinate system is read from file.</exception>
        public virtual ICoordinateSystem CoordinateSystem
        {
            get { return _CoordinateSystem; }
            set
            {
                if (_CoordsysReadFromFile)
                    throw new ApplicationException("Coordinate system is specified in projection file and is read only");
                _CoordinateSystem = value;
            }
        }


        /// <summary>
        /// Gets the <see cref="SharpMap.Data.Providers.ShapeType">shape geometry type</see> in this shapefile.
        /// </summary>
        /// <remarks>
        /// The property isn't set until the first time the datasource has been opened,
        /// and will throw an exception if this property has been called since initialization. 
        /// <para>All the non-Null shapes in a shapefile are required to be of the same shape
        /// type.</para>
        /// </remarks>
        public virtual ShapeType ShapeType
        {
            get { return _ShapeType; }
        }

        /// <summary>
        /// Gets or sets the filename of the shapefile
        /// </summary>
        /// <remarks>If the filename changes, indexes will be rebuilt</remarks>
        public virtual string Path
        {
            get { return path; }
            set 
            {
                path = value; 
                Open(path);
            }
        }

        /// <summary>
        /// Gets or sets the encoding used for parsing strings from the DBase DBF file.
        /// </summary>
        /// <remarks>
        /// The DBase default encoding is <see cref="System.Text.Encoding.UTF7"/>.
        /// </remarks>
        public virtual Encoding Encoding
        {
            get { return dbaseFile.Encoding; }
            set { dbaseFile.Encoding = value; }
        }

        /// <summary>
        /// Filter Delegate Method for limiting the datasource
        /// </summary>
        /// <remarks>
        /// <example>
        /// Using an anonymous method for filtering all features where the NAME column starts with S:
        /// <code lang="C#">
        /// myShapeDataSource.FilterDelegate = new SharpMap.Data.Providers.ShapeFile.FilterMethod(delegate(SharpMap.Data.FeatureDataRow row) { return (!row["NAME"].ToString().StartsWith("S")); });
        /// </code>
        /// </example>
        /// <example>
        /// Declaring a delegate method for filtering (multi)polygon-features whose area is larger than 5.
        /// <code>
        /// myShapeDataSource.FilterDelegate = CountryFilter;
        /// [...]
        /// public static bool CountryFilter(SharpMap.Data.FeatureDataRow row)
        /// {
        ///		if(row.Geometry.GetType()==typeof(SharpMap.Geometries.Polygon))
        ///			return ((row.Geometry as SharpMap.Geometries.Polygon).Area>5);
        ///		if (row.Geometry.GetType() == typeof(SharpMap.Geometries.MultiPolygon))
        ///			return ((row.Geometry as SharpMap.Geometries.MultiPolygon).Area > 5);
        ///		else return true;
        /// }
        /// </code>
        /// </example>
        /// </remarks>
        /// <seealso cref="FilterMethod"/>
        public virtual FilterMethod FilterDelegate
        {
            get { return _FilterDelegate; }
            set { _FilterDelegate = value; }
        }
        public virtual void ReConnect()
        {

        }

        public virtual void Delete()
        {
            File.Delete(Path);
            var dbaseFilePath = GetDbaseFilePath();
            if(dbaseFilePath != null)
                File.Delete(dbaseFilePath);
            var indexFilePath = GetIndexFilePath();
            if(indexFilePath != null)
                File.Delete(indexFilePath);
        }

        #region Disposers and finalizers

        private bool disposed = false;

        /// <summary>
        /// Disposes the object
        /// </summary>
        public virtual void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    Close();
                    _Envelope = null;
                    tree = null;
                }
                disposed = true;
            }
        }

        /// <summary>
        /// Finalizes the object
        /// </summary>
        ~ShapeFile()
        {
            Dispose();
        }

        #endregion

        #region IFeatureProvider Members
        
        public virtual Type FeatureType
        {
            get { return typeof(FeatureDataRow); }
        }

        public virtual IList Features
        {
            get
            { 
                //problem with reuse 
                return GetFeatures(GetExtents()); 
            }
            set { throw new NotImplementedException(); }
        }

        public virtual IFeature Add(IGeometry geometry)
        {
            throw new NotImplementedException();
        }

        public virtual Func<IFeatureProvider, IGeometry, IFeature> AddNewFeatureFromGeometryDelegate { get; set; }

        public virtual ICollection<IGeometry> GetGeometriesInView(IEnvelope bbox)
        {
            return GetGeometriesInView(bbox, -1);
        }

        /// <summary>
        /// Returns geometries whose bounding box intersects 'bbox'
        /// </summary>
        /// <remarks>
        /// <para>Please note that this method doesn't guarantee that the geometries returned actually intersect 'bbox', but only
        /// that their boundingbox intersects 'bbox'.</para>
        /// <para>This method is much faster than the QueryFeatures method, because intersection tests
        /// are performed on objects simplifed by their boundingbox, and using the Spatial Index.</para>
        /// </remarks>
        /// <param name="bbox"></param>
        /// <returns></returns>
        public virtual ICollection<IGeometry> GetGeometriesInView(IEnvelope bbox, double tolerance)
        {
            if(!IsOpen)
            {
                Open(Path
                    );
            }
            DateTime startReadingTime = DateTime.Now;

            //Use the spatial index to get a list of features whose boundingbox intersects bbox
            var objectlist = GetObjectIDsInView(bbox);
            if (objectlist.Count == 0) //no features found. Return an empty set
            {
                return new Collection<IGeometry>();
            }

            var geometries = new Collection<IGeometry>();

            foreach (var o in objectlist)
            {
                var g = GetGeometryByID(o);
                
                if (g == null)
                {
                    continue;
                }
                
                geometries.Add(g);
            }

            long dt = (DateTime.Now - startReadingTime).Milliseconds;

            return geometries;
        }

        /// <summary>
        /// Returns all objects whose boundingbox intersects bbox.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Please note that this method doesn't guarantee that the geometries returned actually intersect 'bbox', but only
        /// that their boundingbox intersects 'bbox'.
        /// </para>
        /// </remarks>
        /// <param name="bbox"></param>
        /// <returns></returns>
        public virtual IList GetFeatures(IEnvelope bbox)
        {
            Open(path);

            if (!_IsOpen) return new List<IFeature>(); //return empty list in case there is no connection

            long coordinateCount = 0;
            DateTime startReadingTime = DateTime.Now;

            if (dbaseFile == null)
                return new ArrayList();

            //Use the spatial index to get a list of features whose boundingbox intersects bbox
            var objectlist = GetObjectIDsInView(bbox);
            var table = dbaseFile.NewTable;

            foreach (var o in objectlist)
            {
                var fdr = dbaseFile.GetFeature(o, table);
                fdr.Geometry = ReadGeometry(o);
                if (fdr.Geometry != null)
                {
                    if (fdr.Geometry.EnvelopeInternal.Intersects(bbox))
                    {
                        if (FilterDelegate == null || FilterDelegate(fdr))
                        {
                            table.AddRow(fdr);
                        }
                    }
                    coordinateCount += fdr.Geometry.Coordinates.Length;
                }
            }

            return table;

            long dt = (DateTime.Now - startReadingTime).Milliseconds;
            //log.DebugFormat("Selected {0} features + attributes with total {1} coordinates in {2} ms", table.Count, coordinateCount, dt);
        }

        /// <summary>
        /// Returns geometry Object IDs whose bounding box intersects 'envelope'
        /// </summary>
        /// <param name="envelope"></param>
        /// <returns></returns>
        public virtual ICollection<int> GetObjectIDsInView(IEnvelope envelope)
        {
            if (!IsOpen)
                throw (new ApplicationException("An attempt was made to read from a closed datasource"));
            //Use the spatial index to get a list of features whose boundingbox intersects envelope
            return tree.Search(envelope);
        }

        /// <summary>
        /// Returns the geometry corresponding to the Object ID
        /// </summary>
        /// <param name="oid">Object ID</param>
        /// <returns>geometry</returns>
        public virtual IGeometry GetGeometryByID(int oid)
        {
            if (FilterDelegate != null) //Apply filtering
            {
                // TODO: this should work as IFeature
                var fdr = (FeatureDataRow) GetFeature(oid);
                if (fdr != null)
                    return fdr.Geometry;
                else
                    return null;
            }
            else return ReadGeometry(oid);
        }

        /// <summary>
        /// Returns the data associated with all the geometries that are intersected by 'geom'.
        /// Please note that the ShapeFile provider currently doesn't fully support geometryintersection
        /// and thus only BoundingBox/BoundingBox querying are performed. The results are NOT
        /// guaranteed to lie withing 'geom'.
        /// </summary>
        /// <param name="geom"></param>
        public virtual IList GetFeatures(IGeometry geom)
        {
            var dt = dbaseFile.NewTable;
            IEnvelope bbox = geom.EnvelopeInternal;
            //Get candidates by intersecting the spatial index tree
            Collection<int> objectlist = tree.Search(bbox);

            if (objectlist.Count == 0)
                return dt;

            for (int j = 0; j < objectlist.Count; j++)
            {
                // JIRA TOOLS-1307 uint (dt.Rows.Count - 1) = (uint)(-1) causer rather lengthy loop.
                // some SharpMap refactoring?
                //for (uint i = (uint) dt.Rows.Count - 1; i >= 0; i--)
                //{
                    FeatureDataRow fdr = GetFeature(objectlist[j], dt);
                    if (fdr.Geometry != null)
                        if (fdr.Geometry.Intersects(geom))
                            if (FilterDelegate == null || FilterDelegate(fdr))
                                dt.AddRow(fdr);
                //}
            }

            return dt;
        }


        /// <summary>
        /// Returns the total number of features in the datasource (without any filter applied)
        /// </summary>
        /// <returns></returns>
        public virtual int GetFeatureCount()
        {
            return _FeatureCount;
        }

        /*
        /// <summary>
        /// Returns a colleciton of columns from the datasource [NOT IMPLEMENTED]
        /// </summary>
        public System.Data.DataColumnCollection Columns
        {
            get {
                if (dbaseFile != null)
                {
                    System.Data.DataTable dt = dbaseFile.DataTable;
                    return dt.Columns;
                }
                else
                    throw (new FileNotFoundException("An attempt was made to read DBase data from a shapefile without a valid .DBF file"));
            }
        }*/

        /// <summary>
        /// Gets a datarow from the datasource at the specified index
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public virtual IFeature GetFeature(int index)
        {
            return GetFeature(index, null);
        }

        public virtual bool Contains(IFeature feature)
        {
            if (feature is FeatureDataRow)
            {
                return dbaseFile.NewTable.Rows.Contains(feature);
            }

            return false;
        }

        public virtual int IndexOf(IFeature feature)
        {
            if (feature is FeatureDataRow)
            {
                return dbaseFile.NewTable.Rows.IndexOf((FeatureDataRow) feature);
            }
            return -1;
        }

        /// <summary>
        /// Returns the extents of the datasource
        /// </summary>
        /// <returns></returns>
        public virtual IEnvelope GetExtents()
        {
            if(!IsOpen)
            {
                Open(path);
            }

            if (tree == null)
            {
                log.Error(path + ": File hasn't been spatially indexed. Try opening the datasource before retriving extents");
                return null;
            }

            IEnvelope envelope = tree.Box;

            return envelope;
        }

        /// <summary>
        /// Gets or sets the spatial reference ID (CRS)
        /// </summary>
        public virtual int SRID
        {
            get { return _SRID; }
            set { _SRID = value; }
        }

        #endregion

        #region IFileBased Members

        /// <summary>
        /// Opens the datasource
        /// </summary>
        public virtual void Open(string path)
        {
            // Get a Connector.  The connector returned is guaranteed to be connected and ready to go.
            // Pooling.Connector connector = Pooling.ConnectorPool.ConnectorPoolManager.RequestConnector(this,true);

            if (!File.Exists(path))
            {
                log.Error("Could not find " + path);
                return;
            }

            if (!_IsOpen || this.path != path)
            {
                try
                {
                    this.path = path;
                    tree = null;

                    //Initialize DBF
                    string dbffile = GetDbaseFilePath();
                    if (File.Exists(dbffile))
                        dbaseFile = new DbaseReader(dbffile);
                    //Parse shape header
                    ParseHeader();
                    //Read projection file
                    ParseProjection();

                    fsShapeIndex = new FileStream(GetIndexFilePath(), FileMode.Open,
                                                  FileAccess.Read);
                    brShapeIndex = new BinaryReader(fsShapeIndex, Encoding.Unicode);
                    fsShapeFile = new FileStream(this.path, FileMode.Open, FileAccess.Read);
                    brShapeFile = new BinaryReader(fsShapeFile);
                    InitializeShape(this.path, _FileBasedIndex);
                    if (dbaseFile != null)
                        dbaseFile.Open();
                    _IsOpen = true;
                }
                catch (IOException e)
                {
                    log.Error(e.Message);
                    _IsOpen = false;
                }
            }
        }

        private string GetIndexFilePath()
        {
            if (path == null)
            {
                return null;
            }

            return path.Remove(path.Length - 4, 4) + ".shx";
        }

        private string GetDbaseFilePath()
        {
            if(path == null)
            {
                return null;
            }

            return path.Substring(0, path.LastIndexOf(".")) + ".dbf";
        }

        public virtual void CreateNew(string path)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Closes the datasource
        /// </summary>
        public virtual void Close()
        {
            if (!disposed)
            {
                //TODO: (ConnectionPooling)
                /*	if (connector != null)
					{ Pooling.ConnectorPool.ConnectorPoolManager.Release...()
				}*/
                if (_IsOpen)
                {
                    brShapeFile.Close();
                    fsShapeFile.Close();
                    brShapeIndex.Close();
                    fsShapeIndex.Close();
                    if (dbaseFile != null)
                        dbaseFile.Close();
                    _IsOpen = false;
                }
            }
        }

        /// <summary>
        /// Returns true if the datasource is currently open
        /// </summary>		
        public virtual bool IsOpen
        {
            get { return _IsOpen; }
        }

        public virtual void SwitchTo(string newPath)
        {
            Close();
            Open(newPath);
        }

        public virtual void CopyTo(string newPath)
        {
            throw new NotImplementedException();
        }

        #endregion

        private void InitializeShape(string filename, bool FileBasedIndex)
        {
            if (!File.Exists(filename))
                throw new FileNotFoundException(String.Format("Could not find file \"{0}\"", filename));
            if (!filename.ToLower().EndsWith(".shp"))
                throw (new Exception("Invalid shapefile filename: " + filename));

            LoadSpatialIndex(FileBasedIndex); //Load spatial index			
        }

        /// <summary>
        /// Reads and parses the header of the .shx index file
        /// </summary>
        private void ParseHeader()
        {
            fsShapeIndex = new FileStream(System.IO.Path.ChangeExtension(path, ".shx"), FileMode.Open, FileAccess.Read);
            brShapeIndex = new BinaryReader(fsShapeIndex, Encoding.Unicode);

            brShapeIndex.BaseStream.Seek(0, 0);
            //Check file header
            if (brShapeIndex.ReadInt32() != 170328064)
                //File Code is actually 9994, but in Little Endian Byte Order this is '170328064'
                throw (new ApplicationException("Invalid Shapefile Index (.shx)"));

            brShapeIndex.BaseStream.Seek(24, 0); //seek to File Length
            int IndexFileSize = SwapByteOrder(brShapeIndex.ReadInt32());
                //Read filelength as big-endian. The length is based on 16bit words
            _FeatureCount = (2*IndexFileSize - 100)/8;
                //Calculate FeatureCount. Each feature takes up 8 bytes. The header is 100 bytes

            brShapeIndex.BaseStream.Seek(32, 0); //seek to ShapeType
            _ShapeType = (ShapeType) brShapeIndex.ReadInt32();

            //Read the spatial bounding box of the contents
            brShapeIndex.BaseStream.Seek(36, 0); //seek to box


            double x1, x2, y1, y2;
            x1 = brShapeIndex.ReadDouble();
            y1 = brShapeIndex.ReadDouble();
            x2 = brShapeIndex.ReadDouble();
            y2 = brShapeIndex.ReadDouble();

            _Envelope = GeometryFactory.CreateEnvelope(x1, x2, y1, y2);

            brShapeIndex.Close();
            fsShapeIndex.Close();
        }

        /// <summary>
        /// Reads and parses the projection if a projection file exists
        /// </summary>
        private void ParseProjection()
        {
            string projfile = System.IO.Path.GetDirectoryName(Path) + "\\" + System.IO.Path.GetFileNameWithoutExtension(Path) + ".prj";
            if (File.Exists(projfile))
            {
                try
                {
                    string wkt = File.ReadAllText(projfile);
                    _CoordinateSystem = (ICoordinateSystem) CoordinateSystemWktReader.Parse(wkt);
                    _CoordsysReadFromFile = true;
                }
                catch (Exception ex)
                {
                    Trace.TraceWarning("Coordinate system file '" + projfile +
                                       "' found, but could not be parsed. WKT parser returned:" + ex.Message);
                    throw (ex);
                }
            }
        }

        /// <summary>
        /// Reads the record offsets from the .shx index file and returns the information in an array
        /// </summary>
        private int[] ReadIndex()
        {
            var OffsetOfRecord = new int[_FeatureCount];
            brShapeIndex.BaseStream.Seek(100, 0); //skip the header

            for (int x = 0; x < _FeatureCount; ++x)
            {
                OffsetOfRecord[x] = 2*SwapByteOrder(brShapeIndex.ReadInt32()); //Read shape data position // ibuffer);
                brShapeIndex.BaseStream.Seek(brShapeIndex.BaseStream.Position + 4, 0); //Skip content length
            }
            return OffsetOfRecord;
        }

        /// <summary>
        /// Gets the file position of the n'th shape
        /// </summary>
        /// <param name="n">Shape ID</param>
        /// <returns></returns>
        private int GetShapeIndex(int n)
        {
            brShapeIndex.BaseStream.Seek(100 + n*8, 0); //seek to the position of the index
            return 2*SwapByteOrder(brShapeIndex.ReadInt32()); //Read shape data position
        }

        ///<summary>
        ///Swaps the byte order of an int32
        ///</summary>
        /// <param name="i">Integer to swap</param>
        /// <returns>Byte Order swapped int32</returns>
        private int SwapByteOrder(int i)
        {
            byte[] buffer = BitConverter.GetBytes(i);
            Array.Reverse(buffer, 0, buffer.Length);
            return BitConverter.ToInt32(buffer, 0);
        }

        /// <summary>
        /// Loads a spatial index from a file. If it doesn't exist, one is created and saved
        /// </summary>
        /// <param name="filename"></param>
        /// <returns>QuadTree index</returns>
        private QuadTree CreateSpatialIndexFromFile(string filename)
        {
            if (File.Exists(filename + ".sidx"))
            {
                try
                {
                    return QuadTree.FromFile(filename + ".sidx");
                }
                catch (QuadTree.ObsoleteFileFormatException)
                {
                    File.Delete(filename + ".sidx");
                    return CreateSpatialIndexFromFile(filename);
                }
                catch (Exception ex)
                {
                    throw ex;
                }
            }
            else
            {
                QuadTree tree = CreateSpatialIndex(path);
                tree.SaveIndex(filename + ".sidx");
                return tree;
            }
        }

        /// <summary>
        /// Generates a spatial index for a specified shape file.
        /// </summary>
        /// <param name="filename"></param>
        private QuadTree CreateSpatialIndex(string filename)
        {
            var objList = new List<QuadTree.BoxObjects>();
            //Convert all the geometries to boundingboxes 
            uint i = 0;
            foreach (IEnvelope box in GetAllFeatureBoundingBoxes())
            {
                if (!double.IsNaN(box.MinX) && !double.IsNaN(box.MaxX) && !double.IsNaN(box.MaxY) &&
                    !double.IsNaN(box.MinY))
                {
                    var g = new QuadTree.BoxObjects();
                    g.box = box;
                    g.ID = i;
                    objList.Add(g);
                    i++;
                }
            }

            Heuristic heur;
            heur.maxdepth = (int) Math.Ceiling(Math.Log(GetFeatureCount(), 2));
            heur.minerror = 10;
            heur.tartricnt = 5;
            heur.mintricnt = 2;
            return new QuadTree(objList, 0, heur);
        }

        //private void LoadSpatialIndex() { LoadSpatialIndex(false,false); }
        private void LoadSpatialIndex(bool LoadFromFile)
        {
            LoadSpatialIndex(false, LoadFromFile);
        }

        private void LoadSpatialIndex(bool ForceRebuild, bool LoadFromFile)
        {
            //Only load the tree if we haven't already loaded it, or if we want to force a rebuild
            if (tree == null || ForceRebuild)
            {
                // Is this a web application? If so lets store the index in the cache so we don't
                // need to rebuild it for each request
                if (HttpContext.Current != null)
                {
                    //Check if the tree exists in the cache
                    if (HttpContext.Current.Cache[path] != null)
                        tree = (QuadTree) HttpContext.Current.Cache[path];
                    else
                    {
                        if (!LoadFromFile)
                            tree = CreateSpatialIndex(path);
                        else
                            tree = CreateSpatialIndexFromFile(path);
                        //Store the tree in the web cache
                        //TODO: Remove this when connection pooling is implemented
                        HttpContext.Current.Cache.Insert(path, tree, null, Cache.NoAbsoluteExpiration,
                                                         TimeSpan.FromDays(1));
                    }
                }
                else if (!LoadFromFile)
                    tree = CreateSpatialIndex(path);
                else
                    tree = CreateSpatialIndexFromFile(path);
            }
        }

        /// <summary>
        /// Forces a rebuild of the spatial index. If the instance of the ShapeFile provider
        /// uses a file-based index the file is rewritten to disk.
        /// </summary>
        public virtual void RebuildSpatialIndex()
        {
            if (_FileBasedIndex)
            {
                if (File.Exists(path + ".sidx"))
                    File.Delete(path + ".sidx");
                tree = CreateSpatialIndexFromFile(path);
            }
            else
                tree = CreateSpatialIndex(path);
            if (HttpContext.Current != null)
                //TODO: Remove this when connection pooling is implemented:
                HttpContext.Current.Cache.Insert(path, tree, null, Cache.NoAbsoluteExpiration, TimeSpan.FromDays(1));
        }

        /// <summary>
        /// Reads all boundingboxes of features in the shapefile. This is used for spatial indexing.
        /// </summary>
        /// <returns></returns>
        private List<IEnvelope> GetAllFeatureBoundingBoxes()
        {
            int[] offsetOfRecord = ReadIndex(); //Read the whole .idx file

            var boxes = new List<IEnvelope>();

            if (_ShapeType == ShapeType.Point)
            {
                for (int a = 0; a < _FeatureCount; ++a)
                {
                    fsShapeFile.Seek(offsetOfRecord[a] + 8, 0); //skip record number and content length
                    if ((ShapeType) brShapeFile.ReadInt32() != ShapeType.Null)
                    {
                        double x = brShapeFile.ReadDouble();
                        double y = brShapeFile.ReadDouble();
                        boxes.Add(GeometryFactory.CreateEnvelope(x, x, y, y));
                    }
                }
            }
            else
            {
                for (int a = 0; a < _FeatureCount; ++a)
                {
                    fsShapeFile.Seek(offsetOfRecord[a] + 8, 0); //skip record number and content length
                    if ((ShapeType) brShapeFile.ReadInt32() != ShapeType.Null)
                    {
                        double x1, x2, y1, y2;
                        x1 = brShapeFile.ReadDouble();
                        y1 = brShapeFile.ReadDouble();
                        x2 = brShapeFile.ReadDouble();
                        y2 = brShapeFile.ReadDouble();

                        boxes.Add(GeometryFactory.CreateEnvelope(x1, x2, y1, y2));
                    }
                }
            }
            return boxes;
        }

        /// <summary>
        /// Reads and parses the geometry with ID 'oid' from the ShapeFile
        /// </summary>
        /// <remarks><see cref="FilterDelegate">Filtering</see> is not applied to this method</remarks>
        /// <param name="oid">Object ID</param>
        /// <returns>geometry</returns>
        private IGeometry ReadGeometry(int oid)
        {
            brShapeFile.BaseStream.Seek(GetShapeIndex(oid) + 8, 0); //Skip record number and content length
            var type = (ShapeType) brShapeFile.ReadInt32(); //Shape type
            if (type == ShapeType.Null)
                return null;
            if (_ShapeType == ShapeType.Point || _ShapeType == ShapeType.PointM || _ShapeType == ShapeType.PointZ)
            {
                //SharpMap.Geometries.Point tempFeature = new SharpMap.Geometries.Point();
                return GeometryFactory.CreatePoint(brShapeFile.ReadDouble(), brShapeFile.ReadDouble());
            }
            else if (_ShapeType == ShapeType.Multipoint || _ShapeType == ShapeType.MultiPointM ||
                     _ShapeType == ShapeType.MultiPointZ)
            {
                brShapeFile.BaseStream.Seek(32 + brShapeFile.BaseStream.Position, 0); //skip min/max box
                var feature = new List<IPoint>();
                //SharpMap.Geometries.MultiPoint feature = new SharpMap.Geometries.MultiPoint();
                int nPoints = brShapeFile.ReadInt32(); // get the number of points
                if (nPoints == 0)
                    return null;
                for (int i = 0; i < nPoints; i++)
                    feature.Add(GeometryFactory.CreatePoint(brShapeFile.ReadDouble(), brShapeFile.ReadDouble()));


                return GeometryFactory.CreateMultiPoint(feature.ToArray());
            }
            else if (_ShapeType == ShapeType.PolyLine || _ShapeType == ShapeType.Polygon ||
                     _ShapeType == ShapeType.PolyLineM || _ShapeType == ShapeType.PolygonM ||
                     _ShapeType == ShapeType.PolyLineZ || _ShapeType == ShapeType.PolygonZ)
            {
                brShapeFile.BaseStream.Seek(32 + brShapeFile.BaseStream.Position, 0); //skip min/max box

                int nParts = brShapeFile.ReadInt32(); // get number of parts (segments)
                if (nParts == 0)
                    return null;
                int nPoints = brShapeFile.ReadInt32(); // get number of points

                var segments = new int[nParts + 1];
                //Read in the segment indexes
                for (int b = 0; b < nParts; b++)
                    segments[b] = brShapeFile.ReadInt32();
                //add end point
                segments[nParts] = nPoints;

                if ((int) _ShapeType%10 == 3)
                {
                    //SharpMap.Geometries.MultiLineString mline = new SharpMap.Geometries.MultiLineString();
                    var mline = new List<ILineString>();
                    for (int LineID = 0; LineID < nParts; LineID++)
                    {
                        //SharpMap.Geometries.LineString line = new SharpMap.Geometries.LineString();
                        var line = new List<ICoordinate>();
                        for (int i = segments[LineID]; i < segments[LineID + 1]; i++)
                            line.Add(GeometryFactory.CreateCoordinate(brShapeFile.ReadDouble(),
                                                                      brShapeFile.ReadDouble()));
                        //line.Vertices.Add(new SharpMap.Geometries.Point(
                        mline.Add(GeometryFactory.CreateLineString(line.ToArray()));
                    }
                    if (mline.Count == 1)
                        return mline[0];
                    return GeometryFactory.CreateMultiLineString(mline.ToArray());
                }
                else //(_ShapeType == ShapeType.Polygon etc...)
                {
                    //First read all the rings
                    //List<SharpMap.Geometries.LinearRing> rings = new List<SharpMap.Geometries.LinearRing>();
                    var rings = new List<ILinearRing>();
                    for (int RingID = 0; RingID < nParts; RingID++)
                    {
                        //SharpMap.Geometries.LinearRing ring = new SharpMap.Geometries.LinearRing();
                        var ring = new List<ICoordinate>();
                        for (int i = segments[RingID]; i < segments[RingID + 1]; i++)
                            ring.Add(GeometryFactory.CreateCoordinate(brShapeFile.ReadDouble(),
                                                                      brShapeFile.ReadDouble()));

                        //ring.Vertices.Add(new SharpMap.Geometries.Point
                        rings.Add(GeometryFactory.CreateLinearRing(ring.ToArray()));
                    }
                    var IsCounterClockWise = new bool[rings.Count];
                    int PolygonCount = 0;
                    for (int i = 0; i < rings.Count; i++)
                    {
                        IsCounterClockWise[i] = GeometryFactory.IsCCW(rings[i].Coordinates);
                        if (!IsCounterClockWise[i])
                            PolygonCount++;
                    }
                    if (PolygonCount == 1) //We only have one polygon
                    {
                        ILinearRing shell = rings[0];
                        var holes = new List<ILinearRing>();
                        if (rings.Count > 1)
                            for (int i = 1; i < rings.Count; i++)
                                holes.Add(rings[i]);
                        return GeometryFactory.CreatePolygon(shell, holes.ToArray());
                    }
                    else
                    {
                        var polys = new List<IPolygon>();
                        ILinearRing shell = rings[0];
                        var holes = new List<ILinearRing>();
                        for (int i = 1; i < rings.Count; i++)
                        {
                            if (!IsCounterClockWise[i])
                            {
                                polys.Add(GeometryFactory.CreatePolygon(shell, null));
                                shell = rings[i];
                            }
                            else
                                holes.Add(rings[i]);
                        }
                        polys.Add(GeometryFactory.CreatePolygon(shell, holes.ToArray()));
                        return GeometryFactory.CreateMultiPolygon(polys.ToArray());
                    }
                }
            }
            else
                throw (new ApplicationException("Shapefile type " + _ShapeType.ToString() + " not supported"));
        }

        /// <summary>
        /// Gets a datarow from the datasource at the specified index belonging to the specified datatable
        /// </summary>
        /// <param name="RowID"></param>
        /// <param name="dt">Datatable to feature should belong to.</param>
        /// <returns></returns>
        public virtual FeatureDataRow GetFeature(int RowID, FeatureDataTable dt)
        {
            if (dbaseFile != null)
            {
                Open(Path);
                var dr = (FeatureDataRow) dbaseFile.GetFeature(RowID, dt ?? dbaseFile.NewTable);
                dr.Geometry = ReadGeometry(RowID);

                if (FilterDelegate == null || FilterDelegate(dr))
                    return dr;
                else
                    return null;
            }
            else
                throw (new FileNotFoundException(
                    "An attempt was made to read DBase data from a shapefile without a valid .DBF file"));
        }

        public virtual string FileFilter
        {
            get { return "Shape file (*.shp)|*.shp"; }
        }

        public virtual bool IsRelationalDataBase
        {
            get { return false; }
        }
    }
}