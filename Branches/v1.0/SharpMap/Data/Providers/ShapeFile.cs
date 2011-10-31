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
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Web;
using System.Web.Caching;
#if !DotSpatialProjections
using GeoAPI.Geometries;
using ProjNet.Converters.WellKnownText;
using ProjNet.CoordinateSystems;
#else
using DotSpatial.Projections;
#endif
using SharpMap.Geometries;
using SharpMap.Utilities.SpatialIndexing;

namespace SharpMap.Data.Providers
{
	/// <summary>
	/// Shapefile geometry type.
	/// </summary>
	public enum ShapeType
	{
		/// <summary>
		/// Null shape with no geometric data
		/// </summary>
		Null = 0,
		/// <summary>
		/// A point consists of a pair of double-precision coordinates.
		/// SharpMap interpretes this as <see cref="SharpMap.Geometries.Point"/>
		/// </summary>
		Point = 1,
		/// <summary>
		/// PolyLine is an ordered set of vertices that consists of one or more parts. A part is a
		/// connected sequence of two or more points. Parts may or may not be connected to one
		///	another. Parts may or may not intersect one another.
		/// SharpMap interpretes this as either <see cref="SharpMap.Geometries.LineString"/> or <see cref="SharpMap.Geometries.MultiLineString"/>
		/// </summary>
		PolyLine = 3,
		/// <summary>
		/// A polygon consists of one or more rings. A ring is a connected sequence of four or more
		/// points that form a closed, non-self-intersecting loop. A polygon may contain multiple
		/// outer rings. The order of vertices or orientation for a ring indicates which side of the ring
		/// is the interior of the polygon. The neighborhood to the right of an observer walking along
		/// the ring in vertex order is the neighborhood inside the polygon. Vertices of rings defining
		/// holes in polygons are in a counterclockwise direction. Vertices for a single, ringed
		/// polygon are, therefore, always in clockwise order. The rings of a polygon are referred to
		/// as its parts.
		/// SharpMap interpretes this as either <see cref="SharpMap.Geometries.Polygon"/> or <see cref="SharpMap.Geometries.MultiPolygon"/>
		/// </summary>
		Polygon = 5,
		/// <summary>
		/// A MultiPoint represents a set of points.
		/// SharpMap interpretes this as <see cref="SharpMap.Geometries.MultiPoint"/>
		/// </summary>
		Multipoint = 8,
		/// <summary>
		/// A PointZ consists of a triplet of double-precision coordinates plus a measure.
		/// SharpMap interpretes this as <see cref="SharpMap.Geometries.Point"/>
		/// </summary>
		PointZ = 11,
		/// <summary>
		/// A PolyLineZ consists of one or more parts. A part is a connected sequence of two or
		/// more points. Parts may or may not be connected to one another. Parts may or may not
		/// intersect one another.
		/// SharpMap interpretes this as <see cref="SharpMap.Geometries.LineString"/> or <see cref="SharpMap.Geometries.MultiLineString"/>
		/// </summary>
		PolyLineZ = 13,
		/// <summary>
		/// A PolygonZ consists of a number of rings. A ring is a closed, non-self-intersecting loop.
		/// A PolygonZ may contain multiple outer rings. The rings of a PolygonZ are referred to as
		/// its parts.
		/// SharpMap interpretes this as either <see cref="SharpMap.Geometries.Polygon"/> or <see cref="SharpMap.Geometries.MultiPolygon"/>
		/// </summary>
		PolygonZ = 15,
		/// <summary>
		/// A MultiPointZ represents a set of <see cref="PointZ"/>s.
		/// SharpMap interpretes this as <see cref="SharpMap.Geometries.MultiPoint"/>
		/// </summary>
		MultiPointZ = 18,
		/// <summary>
		/// A PointM consists of a pair of double-precision coordinates in the order X, Y, plus a measure M.
		/// SharpMap interpretes this as <see cref="SharpMap.Geometries.Point"/>
		/// </summary>
		PointM = 21,
		/// <summary>
		/// A shapefile PolyLineM consists of one or more parts. A part is a connected sequence of
		/// two or more points. Parts may or may not be connected to one another. Parts may or may
		/// not intersect one another.
		/// SharpMap interpretes this as <see cref="SharpMap.Geometries.LineString"/> or <see cref="SharpMap.Geometries.MultiLineString"/>
		/// </summary>
		PolyLineM = 23,
		/// <summary>
		/// A PolygonM consists of a number of rings. A ring is a closed, non-self-intersecting loop.
		/// SharpMap interpretes this as either <see cref="SharpMap.Geometries.Polygon"/> or <see cref="SharpMap.Geometries.MultiPolygon"/>
		/// </summary>
		PolygonM = 25,
		/// <summary>
		/// A MultiPointM represents a set of <see cref="PointM"/>s.
		/// SharpMap interpretes this as <see cref="SharpMap.Geometries.MultiPoint"/>
		/// </summary>
		MultiPointM = 28,
		/// <summary>
		/// A MultiPatch consists of a number of surface patches. Each surface patch describes a
		/// surface. The surface patches of a MultiPatch are referred to as its parts, and the type of
		/// part controls how the order of vertices of an MultiPatch part is interpreted.
		/// SharpMap doesn't support this feature type.
		/// </summary>
		MultiPatch = 31
	} ;

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
	public class ShapeFile : IProvider
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

#if !DotSpatialProjections
		private ICoordinateSystem _coordinateSystem;
#else
		private ProjectionInfo _coordinateSystem;
#endif
		private bool _coordsysReadFromFile;

        private int _fileSize;
		private GeoAPI.Geometries.Envelope _envelope;
		private int _featureCount;
		private bool _fileBasedIndex;
	    private readonly bool _fileBasedIndexWanted;
		private string _filename;
		private FilterMethod _filterDelegate;
		private bool _isOpen;
		private ShapeType _shapeType;
		private int _srid = -1;
		private BinaryReader _brShapeFile;
		private BinaryReader _brShapeIndex;
		private DbaseReader _dbaseFile;
		private FileStream _fsShapeFile;
		private FileStream _fsShapeIndex;
		private readonly bool _useMemoryCache;
		private DateTime _lastCleanTimestamp = DateTime.Now;
		private readonly TimeSpan _cacheExpireTimeout = TimeSpan.FromMinutes(1);
		private readonly Dictionary <uint,FeatureDataRow> _cacheDataTable = new Dictionary<uint,FeatureDataRow>();

        private int[] _offsetOfRecord;

		/// <summary>
		/// Tree used for fast query of data
		/// </summary>
		private QuadTree _tree;

		/// <summary>
		/// Initializes a ShapeFile DataProvider without a file-based spatial index.
		/// </summary>
		/// <param name="filename">Path to shape file</param>
		public ShapeFile(string filename) : this(filename, false)
		{
		}

		/// <summary>
		/// Cleans the internal memory cached, expurging the objects that are not in the viewarea anymore
		/// </summary>
		/// <param name="objectlist">OID of the objects in the current viewarea</param>
		private void CleanInternalCache(IList<uint> objectlist)
		{
			//Only execute this if the memorycache is active and the expiretimespan has timed out
			if (_useMemoryCache && 
				DateTime.Now.Subtract(_lastCleanTimestamp) > _cacheExpireTimeout)
			{
				var notIntersectOid = new Collection<uint>();
				
                //identify the not intersected oid
				foreach (uint oid in _cacheDataTable.Keys)
				{
					if (!objectlist.Contains(oid))
					{
						notIntersectOid.Add(oid);
					}
				}
				
                //Clean the cache
				foreach (uint oid in notIntersectOid)
				{
					_cacheDataTable.Remove(oid);
				}

				//Reset the lastclean timestamp
				_lastCleanTimestamp = DateTime.Now;
			}
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
			_filename = filename;
		    _fileBasedIndexWanted = fileBasedIndex;
			_fileBasedIndex = (fileBasedIndex) && File.Exists(Path.ChangeExtension(filename, ".shx"));

			//Initialize DBF
			string dbffile = Path.ChangeExtension(filename, ".dbf");
			if (File.Exists(dbffile))
				_dbaseFile = new DbaseReader(dbffile);

			//By Default disable _MemoryCache
			_useMemoryCache = false;
			//Parse shape header
			ParseHeader();
			//Read projection file
			ParseProjection();
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
	    /// <param name="useMemoryCache">Use the memory cache. BEWARE in case of large shapefiles</param>
	    public ShapeFile(string filename, bool fileBasedIndex, bool useMemoryCache) : this(filename, fileBasedIndex)
		{
			_useMemoryCache = useMemoryCache;
		}

		/// <summary>
		/// Gets or sets the coordinate system of the ShapeFile. If a shapefile has 
		/// a corresponding [filename].prj file containing a Well-Known Text 
		/// description of the coordinate system this will automatically be read.
		/// If this is not the case, the coordinate system will default to null.
		/// </summary>
		/// <exception cref="ApplicationException">An exception is thrown if the coordinate system is read from file.</exception>
#if !DotSpatialProjections
		public ICoordinateSystem CoordinateSystem
#else
		public ProjectionInfo CoordinateSystem
#endif
		{
			get { return _coordinateSystem; }
			set
			{
				if (_coordsysReadFromFile)
					throw new ApplicationException("Coordinate system is specified in projection file and is read only");
				_coordinateSystem = value;
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
		public ShapeType ShapeType
		{
			get { return _shapeType; }
		}


		/// <summary>
		/// Gets or sets the filename of the shapefile
		/// </summary>
		/// <remarks>If the filename changes, indexes will be rebuilt</remarks>
		public string Filename
		{
			get { return _filename; }
			set
			{
				if (value != _filename)
				{
                    if (IsOpen)
						throw new ApplicationException("Cannot change filename while datasource is open");

                    _filename = value;
                    _fileBasedIndex = (_fileBasedIndexWanted) && File.Exists(Path.ChangeExtension(value, ".shx"));

                    var dbffile = Path.ChangeExtension(value, ".dbf");
                    if (File.Exists(dbffile))
                        _dbaseFile = new DbaseReader(dbffile);

					ParseHeader();
					ParseProjection();
					_tree = null;
				}
			}
		}

		/// <summary>
		/// Gets or sets the encoding used for parsing strings from the DBase DBF file.
		/// </summary>
		/// <remarks>
		/// The DBase default encoding is <see cref="System.Text.Encoding.UTF7"/>.
		/// </remarks>
		public Encoding Encoding
		{
			get { return _dbaseFile.Encoding; }
			set { _dbaseFile.Encoding = value; }
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
		public FilterMethod FilterDelegate
		{
			get { return _filterDelegate; }
			set { _filterDelegate = value; }
		}

		#region Disposers and finalizers

		private bool _disposed;

		/// <summary>
		/// Disposes the object
		/// </summary>
		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		private void Dispose(bool disposing)
		{
			if (!_disposed)
			{
				if (disposing)
				{
					Close();
					_envelope = null;
					_tree = null;
				}
				_disposed = true;
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

		#region IProvider Members

		/// <summary>
		/// Opens the datasource
		/// </summary>
		public void Open()
		{
			// TODO:
			// Get a Connector.  The connector returned is guaranteed to be connected and ready to go.
			// Pooling.Connector connector = Pooling.ConnectorPool.ConnectorPoolManager.RequestConnector(this,true);

			if (!_isOpen )
			{
			    string shxFile = Path.ChangeExtension(_filename, "shx");
                if (File.Exists(shxFile))
                {
					_fsShapeIndex = new FileStream(shxFile, FileMode.Open, FileAccess.Read);
					_brShapeIndex = new BinaryReader(_fsShapeIndex, Encoding.Unicode);
                }
				_fsShapeFile = new FileStream(_filename, FileMode.Open, FileAccess.Read);
				_brShapeFile = new BinaryReader(_fsShapeFile);

                // Create array to hold the index array for this open session
                _offsetOfRecord = new int[_featureCount];
                PopulateIndexes();
				InitializeShape(_filename, _fileBasedIndex);
				if (_dbaseFile != null)
					_dbaseFile.Open();
				_isOpen = true;

			}
		}

		/// <summary>
		/// Closes the datasource
		/// </summary>
		public void Close()
		{
			if (!_disposed)
			{
				//TODO: (ConnectionPooling)
				/*	if (connector != null)
					{ Pooling.ConnectorPool.ConnectorPoolManager.Release...()
				}*/
				if (_isOpen)
				{
					_brShapeFile.Close();
					_fsShapeFile.Close();
                    if (_brShapeIndex != null)
                    {
                        _brShapeIndex.Close();
                        _fsShapeIndex.Close();
                    }

                    // Give back the memory from the index array.
                    _offsetOfRecord = null;

					if (_dbaseFile != null)
						_dbaseFile.Close();
					_isOpen = false;
				}
			}
		}

		/// <summary>
		/// Returns true if the datasource is currently open
		/// </summary>		
		public bool IsOpen
		{
			get { return _isOpen; }
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
		public Collection<IGeometry> GetGeometriesInView(Envelope bbox)
		{
			//Use the spatial index to get a list of features whose boundingbox intersects bbox
			Collection<uint> objectlist = GetObjectIDsInView(bbox);
			if (objectlist.Count == 0) //no features found. Return an empty set
				return new Collection<IGeometry>();

			var geometries = new Collection<IGeometry>();

			for (int i = 0; i < objectlist.Count; i++)
			{
				var g = GetGeometryByID(objectlist[i]);
				if (g != null)
					geometries.Add(g);
			}

			CleanInternalCache(objectlist);
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
		/// <param name="ds"></param>
		/// <returns></returns>
		public void ExecuteIntersectionQuery(Envelope bbox, FeatureDataSet ds)
		{
			//Use the spatial index to get a list of features whose boundingbox intersects bbox
			var objectlist = GetObjectIDsInView(bbox);
			var dt = _dbaseFile.NewTable;

			for (int i = 0; i < objectlist.Count; i++)
			{
				var fdr = GetFeature(objectlist[i], dt);
				if ( fdr != null ) dt.AddRow(fdr);

				/*
				//This is triple effort since 
				//- Bounding Boxes are checked by GetObjectIdsInView,
				//- FilterDelegate is evaluated in GetFeature
				FeatureDataRow fdr = dbaseFile.GetFeature(objectlist[i], dt);
				fdr.Geometry = ReadGeometry(objectlist[i]);
				if (fdr.Geometry != null)
					if (fdr.Geometry.GetBoundingBox().Intersects(bbox))
						if (FilterDelegate == null || FilterDelegate(fdr))
							dt.AddRow(fdr);
				 */
			}
			ds.Tables.Add(dt);

			CleanInternalCache(objectlist);

		}

		/// <summary>
		/// Returns geometry Object IDs whose bounding box intersects 'bbox'
		/// </summary>
		/// <param name="bbox"></param>
		/// <returns></returns>
		public Collection<uint> GetObjectIDsInView(Envelope bbox)
		{
			if (!IsOpen)
				throw (new ApplicationException("An attempt was made to read from a closed datasource"));
			//Use the spatial index to get a list of features whose boundingbox intersects bbox
			return _tree.Search(bbox);
		}

		/// <summary>
		/// Returns the geometry corresponding to the Object ID
		/// </summary>
		/// <param name="oid">Object ID</param>
		/// <returns>geometry</returns>
		public IGeometry GetGeometryByID(uint oid)
		{
			if (FilterDelegate != null) //Apply filtering
			{
				var fdr = GetFeature(oid);
				if (fdr != null)
					return fdr.Geometry;
				return null;
			}

			if (_useMemoryCache)
			{
				FeatureDataRow fdr;
				_cacheDataTable.TryGetValue(oid, out fdr);
				if (fdr == null)
				{
                    fdr = GetFeature(oid);
                    _cacheDataTable.Add(oid, fdr);
                }

			    return fdr.Geometry;
			}

		    return ReadGeometry(oid);
		}

		/// <summary>
		/// Returns the data associated with all the geometries that are intersected by 'geom'.
		/// Please note that the ShapeFile provider currently doesn't fully support geometryintersection
		/// and thus only GeoAPI.Geometries.Envelope/GeoAPI.Geometries.Envelope querying are performed. The results are NOT
		/// guaranteed to lie withing 'geom'.
		/// </summary>
		/// <param name="geom"></param>
		/// <param name="ds">FeatureDataSet to fill data into</param>
		public void ExecuteIntersectionQuery(IGeometry geom, FeatureDataSet ds)
		{
			var bbox = geom.EnvelopeInternal;

			//currently we are only checking against the bounding box,
			//so we can safely call ExecuteIntersectionQuery(GeoAPI.Geometries.Envelope, FeatureDataSet)
			ExecuteIntersectionQuery(bbox, ds);
			/*
			//Get candidates by intersecting the spatial index tree
			Collection<uint> objectlist = tree.Search(bbox);

			if (objectlist.Count == 0)
				return;

			for (int j = 0; j < objectlist.Count; j++)
			{
				FeatureDataRow fdr = GetFeature(objectlist[j], dt);
				//if (fdr == null) continue;

				if (fdr.Geometry != null)
					if (fdr.Geometry.GetBoundingBox().Intersects(bbox))
						//replace above line with this:  if(fdr.Geometry.Intersects(bbox))  when relation model is complete
						if (FilterDelegate == null || FilterDelegate(fdr))
							dt.AddRow(fdr);
			}
			ds.Tables.Add(dt);
			 */
		}


		/// <summary>
		/// Returns the total number of features in the datasource (without any filter applied)
		/// </summary>
		/// <returns></returns>
		public int GetFeatureCount()
		{
			return _featureCount;
		}

		/// <summary>
		/// Gets a datarow from the datasource at the specified index
		/// </summary>
		/// <param name="rowId"></param>
		/// <returns></returns>
		public FeatureDataRow GetFeature(uint rowId)
		{
			return GetFeature(rowId, _dbaseFile.NewTable);
		}

		/// <summary>
		/// Returns the extents of the datasource
		/// </summary>
		/// <returns></returns>
		public GeoAPI.Geometries.Envelope GetExtents()
		{
			if (_tree == null)
				throw new ApplicationException(
					"File hasn't been spatially indexed. Try opening the datasource before retriving extents");
			return _tree.Box;
		}

		/// <summary>
		/// Gets the connection ID of the datasource
		/// </summary>
		/// <remarks>
		/// The connection ID of a shapefile is its filename
		/// </remarks>
		public string ConnectionID
		{
			get { return _filename; }
		}

		/// <summary>
		/// Gets or sets the spatial reference ID (CRS)
		/// </summary>
		public int SRID
		{
			get { return _srid; }
			set { _srid = value; }
		}

		#endregion

		private void InitializeShape(string filename, bool fileBasedIndex)
		{
			if (!File.Exists(filename))
				throw new FileNotFoundException(String.Format("Could not find file \"{0}\"", filename));
			if (!filename.ToLower().EndsWith(".shp"))
				throw (new Exception("Invalid shapefile filename: " + filename));

			LoadSpatialIndex(fileBasedIndex); //Load spatial index			
		}

		/// <summary>
		/// Reads and parses the header of the .shp index file
		/// </summary>
		private void ParseHeader()
		{
            _fsShapeFile = new FileStream(_filename, FileMode.Open, FileAccess.Read);
            _brShapeFile = new BinaryReader(_fsShapeFile, Encoding.Unicode);

            _brShapeFile.BaseStream.Seek(0, 0);
			//Check file header
            if (_brShapeFile.ReadInt32() != 170328064)
				//File Code is actually 9994, but in Little Endian Byte Order this is '170328064'
				throw (new ApplicationException("Invalid Shapefile (.shp)"));

            //Read filelength as big-endian. The length is based on 16bit words
            _brShapeFile.BaseStream.Seek(24, 0); //seek to File Length
            _fileSize = 2 * SwapByteOrder(_brShapeFile.ReadInt32());
				
            _brShapeFile.BaseStream.Seek(32, 0); //seek to ShapeType
            _shapeType = (ShapeType)_brShapeFile.ReadInt32();

			//Read the spatial bounding box of the contents
            _brShapeFile.BaseStream.Seek(36, 0); //seek to box
            _envelope = new GeoAPI.Geometries.Envelope(_brShapeFile.ReadDouble(), _brShapeFile.ReadDouble(), _brShapeFile.ReadDouble(),
                                        _brShapeFile.ReadDouble());

            // Work out the numberof features, if we have an index file use that
            if (File.Exists(Path.ChangeExtension(_filename, ".shx")))
            //if (brShapeIndex != null)
            {
                _fsShapeIndex = new FileStream(Path.ChangeExtension(_filename, ".shx"), FileMode.Open, FileAccess.Read);
                _brShapeIndex = new BinaryReader(_fsShapeIndex, Encoding.Unicode);

                _brShapeIndex.BaseStream.Seek(24, 0); //seek to File Length
                var indexFileSize = SwapByteOrder(_brShapeIndex.ReadInt32()); //Read filelength as big-endian. The length is based on 16bit words
                _featureCount = (2 * indexFileSize - 100) / 8; //Calculate FeatureCount. Each feature takes up 8 bytes. The header is 100 bytes

                _brShapeIndex.Close();
                _fsShapeIndex.Close();
            }
            else
            {
                // Move to the start of the data
                _brShapeFile.BaseStream.Seek(100, 0); //Skip content length
                long offset = 100; // Start of the data records

                // Loop through the data to extablish the number of features contained within the data file
                while (offset < _fileSize)
                {
                    ++_featureCount;

                    _brShapeFile.BaseStream.Seek(offset + 4, 0); //Skip content length
                    var dataLength = 2 * SwapByteOrder(_brShapeFile.ReadInt32());

                    // This is to cover the chance when the data is corupt
                    // as seen with the sample counties file, in this example the index file
                    // has been adjusted to cover the problem.
                    if ((offset + dataLength) > _fileSize)
                    {
                        --_featureCount;
                    }

                    offset += dataLength; // Add Record data length
                    offset += 8; //  Plus add the record header size
                }
            }
            _brShapeFile.Close();
            _fsShapeFile.Close();
		}

		/// <summary>
		/// Reads and parses the projection if a projection file exists
		/// </summary>
		private void ParseProjection()
		{
			string projfile = Path.GetDirectoryName(Filename) + "\\" + Path.GetFileNameWithoutExtension(Filename) +
							  ".prj";
			if (File.Exists(projfile))
			{
				try
				{
					string wkt = File.ReadAllText(projfile);
#if !DotSpatialProjections
					_coordinateSystem = (ICoordinateSystem) CoordinateSystemWktReader.Parse(wkt);
#else
					_coordinateSystem = new ProjectionInfo();
					_coordinateSystem.ReadEsriString(wkt);
#endif
					_coordsysReadFromFile = true;
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
		/// If an index file is present (.shx) it reads the record offsets from the .shx index file and returns the information in an array.
        /// IfF an indexd array is not present it works out the indexes from the data file, by going through the record headers, finding the
        /// data lengths and workingout the offsets. Which ever method is used a array of index is popuated to be use by the other methods.
        /// This array is created when the open method is called, and removed when the close method called.
		/// </summary>
        private void PopulateIndexes()
		{
            if (_brShapeIndex != null)
            {
                _brShapeIndex.BaseStream.Seek(100, 0);  //skip the header

                for (int x = 0; x < _featureCount; ++x)
                {
                    _offsetOfRecord[x] = 2 * SwapByteOrder(_brShapeIndex.ReadInt32()); //Read shape data position // ibuffer);
                    _brShapeIndex.BaseStream.Seek(_brShapeIndex.BaseStream.Position + 4, 0); //Skip content length
                }
            }
            else  
            {
                // we need to create an index from the shape file

                // Record the current position pointer for later
                var oldPosition = _brShapeFile.BaseStream.Position;
  
                // Move to the start of the data
                _brShapeFile.BaseStream.Seek(100, 0); //Skip content length
                long offset = 100; // Start of the data records
                
                for (int x = 0; x < _featureCount; ++x)
                {
                   _offsetOfRecord[x] = (int)offset; 
                   
                    _brShapeFile.BaseStream.Seek(offset + 4, 0); //Skip content length
                    int dataLength = 2 * SwapByteOrder(_brShapeFile.ReadInt32());
                    offset += dataLength; // Add Record data length
                    offset += 8; //  Plus add the record header size
                }

                // Return the position pointer
                _brShapeFile.BaseStream.Seek(oldPosition, 0);
            }
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
				    var sw = new Stopwatch();
                    sw.Start();
                    var tree = QuadTree.FromFile(filename + ".sidx");
                    sw.Stop();
                    Debug.WriteLine(string.Format("Linear creation of QuadTree took {0}ms", sw.ElapsedMilliseconds));
				    return tree;
				}
				catch (QuadTree.ObsoleteFileFormatException)
				{
					File.Delete(filename + ".sidx");
				    CreateSpatialIndexFromFile(filename);
				}
				catch (Exception ex)
				{
					throw ex;
				}
			}

            // Need to create the spatial index from scratch
            switch (SpatialIndexCreationOption)
            {
                case SpatialIndexCreation.Linear:
                    return CreateSpatialIndexLinear(filename);
                default:
                    return CreateSpatialIndexRecursive(filename);
            }
            //tree.SaveIndex(filename + ".sidx");
		    //return tree;
		}

        static ShapeFile()
        {
            SpatialIndexCreationOption = SpatialIndexCreation.Recursive;
        }

        /// <summary>
		/// Generates a spatial index for a specified shape file.
		/// </summary>
		/// <param name="filename"></param>
		private QuadTree CreateSpatialIndexLinear(string filename)
        {
            var extent = _envelope;
            var sw = new Stopwatch();
            sw.Start();
            var root = QuadTree.CreateRootNode(extent);
            var h = new Heuristic
                        {
                            maxdepth = (int) Math.Ceiling(Math.Log(GetFeatureCount(), 2)),
                            // These are not used for this approach
                            minerror = 10,
                            tartricnt = 5,
                            mintricnt = 2
                        };

            uint i = 0;
            foreach (var box in GetAllFeatureBoundingBoxes())
            {
                //is the box valid?
                if (!box.IsValid()) continue;

                //create box object and add to root.
                var g = new QuadTree.BoxObjects {Box = box, ID = i};
                root.AddNode(g, h);
                i++;
            }

            sw.Stop();
            Debug.WriteLine(string.Format( "Linear creation of QuadTree took {0}ms", sw.ElapsedMilliseconds));

            if (_fileBasedIndexWanted && !string.IsNullOrEmpty(filename))
                root.SaveIndex(filename + ".sidx");
            return root;


        }
        /// <summary>
		/// Generates a spatial index for a specified shape file.
		/// </summary>
		/// <param name="filename">The filename</param>
		private QuadTree CreateSpatialIndexRecursive(string filename)
		{
            var sw = new Stopwatch();
            sw.Start();

            var objList = new List<QuadTree.BoxObjects>();
			//Convert all the geometries to boundingboxes 
			uint i = 0;
			foreach (var box in GetAllFeatureBoundingBoxes())
			{
				if (!box.IsValid()) continue;

                var g = new QuadTree.BoxObjects {Box = box, ID = i};
				objList.Add(g);
				i++;
			}

			Heuristic heur;
			heur.maxdepth = (int) Math.Ceiling(Math.Log(GetFeatureCount(), 2));
			heur.minerror = 10;
			heur.tartricnt = 5;
			heur.mintricnt = 2;
            var root =  new QuadTree(objList, 0, heur);

            sw.Stop();
            Debug.WriteLine(string.Format("Linear creation of QuadTree took {0}ms", sw.ElapsedMilliseconds));

            if (_fileBasedIndexWanted && !String.IsNullOrEmpty(filename))
                root.SaveIndex(filename + ".sidx");

            return root;
		}

        //private void LoadSpatialIndex()
        //{
        //    LoadSpatialIndex(false, false);
        //}

		private void LoadSpatialIndex(bool loadFromFile)
		{
			LoadSpatialIndex(false, loadFromFile);
		}

        /// <summary>
        /// Options to create the <see cref="QuadTree"/> spatial index
        /// </summary>
        public enum SpatialIndexCreation
        {
            /// <summary>
            /// Loads all the bounding boxes in builds the QuadTree from the list of nodes.
            /// This is memory expensive!
            /// </summary>
            Recursive = 0,

            /// <summary>
            /// Creates a root node by the bounds of the ShapeFile and adds each node one-by-one-
            /// </summary>
            Linear,
        }

        /// <summary>
        /// The Spatial index create
        /// </summary>
        public static SpatialIndexCreation SpatialIndexCreationOption { get; set; }


		private void LoadSpatialIndex(bool forceRebuild, bool loadFromFile)
		{
			//Only load the tree if we haven't already loaded it, or if we want to force a rebuild
			if (_tree == null || forceRebuild)
			{
			    Func<string, QuadTree> createSpatialIndex;
                if (SpatialIndexCreationOption == SpatialIndexCreation.Recursive)
                    createSpatialIndex = CreateSpatialIndexRecursive;
                else
                    createSpatialIndex = CreateSpatialIndexLinear;
                
                // Is this a web application? If so lets store the index in the cache so we don't
				// need to rebuild it for each request
				if (HttpContext.Current != null)
				{
					//Check if the tree exists in the cache
					if (HttpContext.Current.Cache[_filename] != null)
						_tree = (QuadTree) HttpContext.Current.Cache[_filename];
					else
					{
						if (!loadFromFile)
							_tree = createSpatialIndex(_filename);
						else
							_tree = CreateSpatialIndexFromFile(_filename);
						//Store the tree in the web cache
						//TODO: Remove this when connection pooling is implemented
						HttpContext.Current.Cache.Insert(_filename, _tree, null, Cache.NoAbsoluteExpiration,
														 TimeSpan.FromDays(1));
					}
				}
				else if (!loadFromFile)
					_tree = createSpatialIndex(_filename);
				else
					_tree = CreateSpatialIndexFromFile(_filename);
			}
		}

		/// <summary>
		/// Forces a rebuild of the spatial index. If the instance of the ShapeFile provider
		/// uses a file-based index the file is rewritten to disk.
		/// </summary>
		public void RebuildSpatialIndex()
		{
			if (_fileBasedIndex)
			{
				if (File.Exists(_filename + ".sidx"))
					File.Delete(_filename + ".sidx");
				_tree = CreateSpatialIndexFromFile(_filename);
			}
			else
			{
			    switch(SpatialIndexCreationOption)
			    {
                    case SpatialIndexCreation.Linear:
                        _tree = CreateSpatialIndexLinear(_filename);
                        break;
                    default:
                        _tree = CreateSpatialIndexRecursive(_filename);
                        break;
			    }
			}
			if (HttpContext.Current != null)
				//TODO: Remove this when connection pooling is implemented:
				HttpContext.Current.Cache.Insert(_filename, _tree, null, Cache.NoAbsoluteExpiration, TimeSpan.FromDays(1));
		}

        /*
	    private delegate bool RecordDeletedFunction(uint oid);
        private static bool NoRecordDeleted(uint oid)
        {
            return false;
        }
         */

		/// <summary>
		/// Reads all boundingboxes of features in the shapefile. This is used for spatial indexing.
		/// </summary>
		/// <returns></returns>
		private IEnumerable<GeoAPI.Geometries.Envelope> GetAllFeatureBoundingBoxes()
		{
			//List<GeoAPI.Geometries.Envelope> boxes = new List<GeoAPI.Geometries.Envelope>();

            /*
		    RecordDeletedFunction recDel = dbaseFile != null
		                                       ? (RecordDeletedFunction) dbaseFile.RecordDeleted
		                                       : NoRecordDeleted;
             */
			if (_shapeType == ShapeType.Point)
			{
				for (int a = 0; a < _featureCount; ++a)
				{
					//if (recDel((uint)a)) continue;

                    _fsShapeFile.Seek(_offsetOfRecord[a] + 8, 0); //skip record number and content length
					if ((ShapeType) _brShapeFile.ReadInt32() != ShapeType.Null)
					{
						double x = _brShapeFile.ReadDouble();
						double y = _brShapeFile.ReadDouble();
						//boxes.Add(new GeoAPI.Geometries.Envelope(x, y, x, y));
						yield return new GeoAPI.Geometries.Envelope(x, y, x, y);
					}
				}
			}
			else
			{
				for (int a = 0; a < _featureCount; ++a)
				{
                    //if (recDel((uint)a)) continue;
                    _fsShapeFile.Seek(_offsetOfRecord[a] + 8, 0); //skip record number and content length
					if ((ShapeType)_brShapeFile.ReadInt32() != ShapeType.Null)
						yield return new GeoAPI.Geometries.Envelope(_brShapeFile.ReadDouble(), _brShapeFile.ReadDouble(),
													 _brShapeFile.ReadDouble(), _brShapeFile.ReadDouble());
						//boxes.Add(new GeoAPI.Geometries.Envelope(brShapeFile.ReadDouble(), brShapeFile.ReadDouble(),
						//                          brShapeFile.ReadDouble(), brShapeFile.ReadDouble()));
				}
			}
			//return boxes;
		}

		/// <summary>
		/// Reads and parses the geometry with ID 'oid' from the ShapeFile
		/// </summary>
		/// <remarks><see cref="FilterDelegate">Filtering</see> is not applied to this method</remarks>
		/// <param name="oid">Object ID</param>
		/// <returns>geometry</returns>
		private IGeometry ReadGeometry(uint oid)
		{
            _brShapeFile.BaseStream.Seek(_offsetOfRecord[oid] + 8, 0); //Skip record number and content length
			ShapeType type = (ShapeType) _brShapeFile.ReadInt32(); //Shape type
			if (type == ShapeType.Null)
				return null;
			switch (_shapeType)
			{
			    case ShapeType.PointZ:
			    case ShapeType.PointM:
			    case ShapeType.Point:
			        return new Point(_brShapeFile.ReadDouble(), _brShapeFile.ReadDouble());
			    
                case ShapeType.MultiPointZ:
			    case ShapeType.MultiPointM:
			    case ShapeType.Multipoint:
			        {
			            _brShapeFile.BaseStream.Seek(32 + _brShapeFile.BaseStream.Position, 0); //skip min/max box
			            var feature = new MultiPoint();
			            var nPoints = _brShapeFile.ReadInt32(); // get the number of points
			            if (nPoints == 0)
			                return null;
			            for (var i = 0; i < nPoints; i++)
			                feature.Points.Add(new Point(_brShapeFile.ReadDouble(), _brShapeFile.ReadDouble()));

			            return feature;
			        }
			    case ShapeType.PolygonZ:
			    case ShapeType.PolyLineZ:
			    case ShapeType.PolygonM:
			    case ShapeType.PolyLineM:
			    case ShapeType.Polygon:
			    case ShapeType.PolyLine:
			        {
			            _brShapeFile.BaseStream.Seek(32 + _brShapeFile.BaseStream.Position, 0); //skip min/max box

			            var nParts = _brShapeFile.ReadInt32(); // get number of parts (segments)
			            if (nParts <= 0)
			                return null;
			            var nPoints = _brShapeFile.ReadInt32(); // get number of points

			            var segments = new int[nParts + 1];
			            
                        //Read in the segment indexes
			            for (var b = 0; b < nParts; b++)
			                segments[b] = _brShapeFile.ReadInt32();
			            
                        //add end point
			            segments[nParts] = nPoints;

			            if ((int) _shapeType%10 == 3)
			            {
			                var mline = new MultiLineString();
			                for (var lineID = 0; lineID < nParts; lineID++)
			                {
			                    var line = new LineString();
			                    for (var i = segments[lineID]; i < segments[lineID + 1]; i++)
			                        line.Vertices.Add(new Coordinate(_brShapeFile.ReadDouble(), _brShapeFile.ReadDouble()));
			                    mline.LineStrings.Add(line);
			                }
			                if (mline.LineStrings.Count == 1)
			                    return mline[0];
			                return mline;
			            }

			            //First read all the rings
			            var rings = new List<LinearRing>();
			            for (var ringID = 0; ringID < nParts; ringID++)
			            {
			                var ring = new LinearRing();
			                for (var i = segments[ringID]; i < segments[ringID + 1]; i++)
			                    ring.Vertices.Add(new Coordinate(_brShapeFile.ReadDouble(), _brShapeFile.ReadDouble()));
			                rings.Add(ring);
			            }
			            var isCounterClockWise = new bool[rings.Count];
                        var polygonCount = 0;
			            for (var i = 0; i < rings.Count; i++)
			            {
			                isCounterClockWise[i] = rings[i].IsCCW();
			                if (!isCounterClockWise[i])
			                    polygonCount++;
			            }
			            if (polygonCount == 1) //We only have one polygon
			            {
			                var poly = new Polygon();
			                poly.ExteriorRing = rings[0];
			                if (rings.Count > 1)
			                    for (int i = 1; i < rings.Count; i++)
			                        poly.InteriorRings.Add(rings[i]);
			                return poly;
			            }
			            else
			            {
			                var mpoly = new MultiPolygon();
			                var poly = new Polygon();
			                poly.ExteriorRing = rings[0];
			                for (int i = 1; i < rings.Count; i++)
			                {
			                    if (!isCounterClockWise[i])
			                    {
			                        mpoly.Polygons.Add(poly);
			                        poly = new Polygon(rings[i]);
			                    }
			                    else
			                        poly.InteriorRings.Add(rings[i]);
			                }
			                mpoly.Polygons.Add(poly);
			                return mpoly;
			            }
			        }
			    default:
			        throw (new ApplicationException("Shapefile type " + _shapeType + " not supported"));
			}
		}

		/// <summary>
		/// Gets a datarow from the datasource at the specified index belonging to the specified datatable
		/// </summary>
		/// <param name="rowId"></param>
		/// <param name="dt">Datatable to feature should belong to.</param>
		/// <returns></returns>
		public FeatureDataRow GetFeature(uint rowId, FeatureDataTable dt)
		{
			Debug.Assert(dt != null);
			if (_dbaseFile != null)
			{
				//MemoryCache
				if (_useMemoryCache)
				{
					FeatureDataRow dr2;
					_cacheDataTable.TryGetValue(rowId, out dr2);
					if (dr2 == null)
					{
						dr2 = _dbaseFile.GetFeature(rowId, dt);
						dr2.Geometry = ReadGeometry(rowId);
						_cacheDataTable.Add(rowId, dr2);
					}

					//Make a copy to return
					FeatureDataRow drNew = dt.NewRow();
					for (int i = 0; i < dr2.Table.Columns.Count; i++)
					{
						drNew[i] = dr2[i];
					}
					drNew.Geometry = dr2.Geometry;
					return drNew;
				}

			    //FeatureDataRow dr = (FeatureDataRow)dbaseFile.GetFeature(RowID, (dt == null) ? dbaseFile.NewTable : dt);
			    FeatureDataRow dr = _dbaseFile.GetFeature(rowId, dt);
			    dr.Geometry = ReadGeometry(rowId);
			    if (FilterDelegate == null || FilterDelegate(dr))
			        return dr;
			    
                return null;
			}
            
		    throw (new ApplicationException(
		        "An attempt was made to read DBase data from a shapefile without a valid .DBF file"));
		}
	}
}