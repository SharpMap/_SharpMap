// Copyright 2006 - Morten Nielsen (www.iter.dk)
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
using System.IO;
using System.Data.OleDb;
using DelftTools.Utils.Data;
using DelftTools.Utils.IO;
using GeoAPI.CoordinateSystems;
using GeoAPI.Extensions.Feature;
using GeoAPI.Geometries;
using SharpMap.Api;

namespace SharpMap.Data.Providers
{
	/// <summary>
	/// The OleDbPoint provider is used for rendering point data from an OleDb compatible datasource.
	/// </summary>
	/// <remarks>
	/// <para>The data source will need to have two double-type columns, xColumn and yColumn that contains the coordinates of the point,
	/// and an integer-type column containing a unique identifier for each row.</para>
	/// <para>To get good performance, make sure you have applied indexes on ID, xColumn and yColumns in your datasource table.</para>
	/// </remarks>
    public class OleDbPoint : Unique<long>, IFeatureProvider, IFileBased
	{
	    public Type FeatureType
	    {
	        get { return typeof(FeatureDataRow); }
	    }

	    public IList Features
	    {
	        get { throw new System.NotImplementedException(); }
	        set { throw new System.NotImplementedException(); }
	    }

        public bool IsReadOnly { get { return true; } }

	    /// <summary>
		/// Initializes a new instance of the OleDbPoint provider
		/// </summary>
		/// <param name="ConnectionStr"></param>
		/// <param name="tablename"></param>
		/// <param name="OID_ColumnName"></param>
		/// <param name="xColumn"></param>
		/// <param name="yColumn"></param>
		public OleDbPoint(string ConnectionStr, string tablename, string OID_ColumnName, string xColumn, string yColumn)
		{
			this.Table = tablename;
			this.XColumn = xColumn;
			this.YColumn = yColumn;
			this.ObjectIdColumn = OID_ColumnName;
			this.ConnectionString = ConnectionStr;
		}

		private string _Table;

		/// <summary>
		/// Data table name
		/// </summary>
		public string Table
		{
			get { return _Table; }
			set { _Table = value; }
		}


		private string _ObjectIdColumn;

		/// <summary>
		/// Name of column that contains the Object ID
		/// </summary>
		public string ObjectIdColumn
		{
			get { return _ObjectIdColumn; }
			set { _ObjectIdColumn = value; }
		}
		
		private string _XColumn;

		/// <summary>
		/// Name of column that contains X coordinate
		/// </summary>
		public string XColumn
		{
			get { return _XColumn; }
			set { _XColumn = value; }
		}
		
		private string _YColumn;

		/// <summary>
		/// Name of column that contains Y coordinate
		/// </summary>
		public string YColumn
		{
			get { return _YColumn; }
			set { _YColumn = value; }
		}

		private string _ConnectionString;
		/// <summary>
		/// Connectionstring
		/// </summary>
		public string ConnectionString
		{
			get { return _ConnectionString; }
			set { _ConnectionString = value;}
		}

		#region IFeatureProvider Members

	    public IFeature Add(IGeometry geometry)
	    {
	        throw new System.NotImplementedException();
	    }

	    public Func<IFeatureProvider,IGeometry,IFeature> AddNewFeatureFromGeometryDelegate { get; set; }
	    public event EventHandler FeaturesChanged;

	    /// <summary>
		/// Returns the geometry corresponding to the Object ID
		/// </summary>
		/// <param name="oid">Object ID</param>
		/// <returns>geometry</returns>
		public IGeometry GetGeometryByID(int oid)
		{			
			GeoAPI.Geometries.IGeometry geom = null;
			using (System.Data.OleDb.OleDbConnection conn = new OleDbConnection(_ConnectionString))
			{
				string strSQL = "Select " + this.XColumn + ", " + this.YColumn + " FROM " + this.Table + " WHERE " + this.ObjectIdColumn + "=" + oid.ToString();
				using (System.Data.OleDb.OleDbCommand command = new OleDbCommand(strSQL, conn))
				{
					conn.Open();
					using (System.Data.OleDb.OleDbDataReader dr = command.ExecuteReader())
					{
						if(dr.Read())
						{
							//If the read row is OK, create a point geometry from the XColumn and YColumn and return it
							if (dr[0] != DBNull.Value && dr[1] != DBNull.Value)
								geom = SharpMap.Converters.Geometries.GeometryFactory.CreatePoint((double)dr[0], (double)dr[1]);
						}
					}
					conn.Close();
				}				
			}
			return geom;
		}

		/// <summary>
		/// Throws NotSupportedException. 
		/// </summary>
		/// <param name="geom"></param>
        public IEnumerable<IFeature> GetFeatures(GeoAPI.Geometries.IGeometry geom)
		{
			throw new NotSupportedException("GetFeatures(Geometry) is not supported by the OleDbPointProvider.");
			//When relation model has been implemented the following will complete the query
			/*
			GetFeatures(geom.GetBoundingBox(), ds);
			if (ds.Tables.Count > 0)
			{
				for(int i=ds.Tables[0].Count-1;i>=0;i--)
				{
					if (!geom.Intersects(ds.Tables[0][i].Geometry))
						ds.Tables.RemoveAt(i);
				}
			}
			*/
		}

	    /// <summary>
	    /// Returns all features with the view box
	    /// </summary>
	    /// <param name="bbox">view box</param>
	    /// <param name="d"></param>
	    /// <param name="ds">FeatureDataSet to fill data into</param>
	    public IEnumerable<IFeature> GetFeatures(IEnvelope bbox)
		{
			//List<Geometries.Geometry> features = new List<SharpMap.Geometries.Geometry>();
			using (System.Data.OleDb.OleDbConnection conn = new OleDbConnection(_ConnectionString))
			{
				string strSQL = "Select * FROM " + this.Table + " WHERE ";
				if (!String.IsNullOrEmpty(_defintionQuery)) //If a definition query has been specified, add this as a filter on the query
					strSQL += _defintionQuery + " AND ";
				//Limit to the points within the boundingbox
				strSQL += this.XColumn + " BETWEEN " + bbox.MinX.ToString(SharpMap.Map.numberFormat_EnUS) + " AND " + bbox.MaxX.ToString(SharpMap.Map.numberFormat_EnUS) + " AND " + this.YColumn +
					" BETWEEN " + bbox.MaxY.ToString(SharpMap.Map.numberFormat_EnUS) + " AND " + bbox.MinY.ToString(SharpMap.Map.numberFormat_EnUS);

				using (System.Data.OleDb.OleDbDataAdapter adapter = new OleDbDataAdapter(strSQL, conn))
				{
					conn.Open();
					System.Data.DataSet ds2 = new System.Data.DataSet();
					adapter.Fill(ds2);
					conn.Close();
					if (ds2.Tables.Count > 0)
					{
						FeatureDataTable fdt = new FeatureDataTable(ds2.Tables[0]);
						foreach (System.Data.DataColumn col in ds2.Tables[0].Columns)
							fdt.Columns.Add(col.ColumnName, col.DataType, col.Expression);
						foreach (System.Data.DataRow dr in ds2.Tables[0].Rows)
						{
							SharpMap.Data.FeatureDataRow fdr = fdt.NewRow();
							foreach (System.Data.DataColumn col in ds2.Tables[0].Columns)
								fdr[col.ColumnName] = dr[col];
							if (dr[this.XColumn] != DBNull.Value && dr[this.YColumn] != DBNull.Value)
								fdr.Geometry = SharpMap.Converters.Geometries.GeometryFactory.CreatePoint((double)dr[this.XColumn], (double)dr[this.YColumn]);
							fdt.AddRow(fdr);
						}
						return fdt;
					}
				}
			}

		    return null;
		}

		/// <summary>
		/// Returns the number of features in the dataset
		/// </summary>
		/// <returns>Total number of features</returns>
		public int GetFeatureCount()
		{
			int count = 0;
			using (System.Data.OleDb.OleDbConnection conn = new OleDbConnection(_ConnectionString))
			{
				string strSQL = "Select Count(*) FROM " + this.Table;
				if (!String.IsNullOrEmpty(_defintionQuery)) //If a definition query has been specified, add this as a filter on the query
					strSQL += " WHERE " + _defintionQuery;

				using (System.Data.OleDb.OleDbCommand command = new OleDbCommand(strSQL, conn))
				{
					conn.Open();
					count = (int)command.ExecuteScalar();
					conn.Close();
				}				
			}
			return count;
		}

		private string _defintionQuery;

		/// <summary>
		/// Definition query used for limiting dataset
		/// </summary>
		public string DefinitionQuery
		{
			get { return _defintionQuery; }
			set { _defintionQuery = value; }
		}

		/// <summary>
		/// Returns a datarow based on a RowID
		/// </summary>
		/// <param name="index"></param>
		/// <returns>datarow</returns>
		public IFeature GetFeature(int index)
		{
			using (System.Data.OleDb.OleDbConnection conn = new OleDbConnection(_ConnectionString))
			{
				string strSQL = "select * from " + this.Table + " WHERE " + this.ObjectIdColumn + "=" + index.ToString();
				
				using (System.Data.OleDb.OleDbDataAdapter adapter = new OleDbDataAdapter(strSQL, conn))
				{
					conn.Open();
					System.Data.DataSet ds = new System.Data.DataSet();
					adapter.Fill(ds);
					conn.Close();
					if (ds.Tables.Count > 0)
					{
						FeatureDataTable fdt = new FeatureDataTable(ds.Tables[0]);
						foreach (System.Data.DataColumn col in ds.Tables[0].Columns)
							fdt.Columns.Add(col.ColumnName, col.DataType, col.Expression);
						if (ds.Tables[0].Rows.Count > 0)
						{
							System.Data.DataRow dr = ds.Tables[0].Rows[0];
							SharpMap.Data.FeatureDataRow fdr = fdt.NewRow();
							foreach (System.Data.DataColumn col in ds.Tables[0].Columns)
								fdr[col.ColumnName] = dr[col];
							if (dr[this.XColumn] != DBNull.Value && dr[this.YColumn] != DBNull.Value)
								fdr.Geometry = SharpMap.Converters.Geometries.GeometryFactory.CreatePoint((double)dr[this.XColumn], (double)dr[this.YColumn]);
							return fdr;
						}
						else
							return null;
					}
					else
						return null;
				}
			}
		}

        public virtual bool Contains(IFeature feature)
	    {
	        throw new System.NotImplementedException();
	    }

        public virtual int IndexOf(IFeature feature)
        {
            throw new NotImplementedException();
        }

	    /// <summary>
		/// Boundingbox of dataset
		/// </summary>
		/// <returns>boundingbox</returns>
		public GeoAPI.Geometries.IEnvelope GetExtents()
		{
			GeoAPI.Geometries.IEnvelope box = null;
			using (System.Data.OleDb.OleDbConnection conn = new OleDbConnection(_ConnectionString))
			{
				string strSQL = "Select Min(" + this.XColumn + ") as MinX, Min(" + this.YColumn + ") As MinY, " +
									   "Max(" + this.XColumn + ") As MaxX, Max(" + this.YColumn + ") As MaxY FROM " + this.Table;
				if (!String.IsNullOrEmpty(_defintionQuery)) //If a definition query has been specified, add this as a filter on the query
					strSQL += " WHERE " + _defintionQuery;

				using (System.Data.OleDb.OleDbCommand command = new OleDbCommand(strSQL, conn))
				{
					conn.Open();
					using (System.Data.OleDb.OleDbDataReader dr = command.ExecuteReader())
					{
						if(dr.Read())
						{
							//If the read row is OK, create a point geometry from the XColumn and YColumn and return it
							if (dr[0] != DBNull.Value && dr[1] != DBNull.Value && dr[2] != DBNull.Value && dr[3] != DBNull.Value)
								box = SharpMap.Converters.Geometries.GeometryFactory.CreateEnvelope((double)dr[0], (double)dr[1], (double)dr[2], (double)dr[3]);
						}
					}
					conn.Close();
				}
			}
			return box;
		}

	    public string Path { get { return path; } set { Open(value); } }

	    public void CreateNew(string path)
	    {
	        throw new NotImplementedException();
	    }

	    /// <summary>
		/// Closes the datasource
		/// </summary>
		public void Close()
		{
			//Don't really do anything. OleDb's ConnectionPooling takes over here
			_IsOpen = false;
		}

	    public void Open(string path)
	    {
	        
	    }

	    private bool _IsOpen;
        
	    /// <summary>
		/// Returns true if the datasource is currently open
		/// </summary>
		public bool IsOpen
		{
			get { return _IsOpen; }
		}

	    public void ReConnect()
	    {
	        
	    }

        public virtual void Delete()
	    {
            File.Delete(Path);
	    }

        public IEnumerable<string> Paths
        {
            get
            {
                if (Path != null)
                    yield return Path;
            }
        }
	    public void SwitchTo(string newPath)
        {
            throw new NotImplementedException();
        }

        public void CopyTo(string newPath)
        {
            throw new NotImplementedException();
        }

	    private string srsWkt;
		
        /// <summary>
		/// The spatial reference in WKT format
		/// </summary>
		public string SrsWkt
		{
			get { return srsWkt; }
			set { srsWkt = value; }
		}

	    public IEnvelope GetBounds(int recordIndex)
	    {
	        return GetFeature(recordIndex).Geometry.EnvelopeInternal;
	    }

	    public ICoordinateSystem CoordinateSystem { get; set; }

	    #endregion

		#region Disposers and finalizers
		private bool disposed = false;
	    private string path;

	    /// <summary>
		/// Disposes the object
		/// </summary>
		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		internal void Dispose(bool disposing)
		{
			if (!disposed)
			{
				if (disposing)
				{
				}
				disposed = true;
			}
		}

		/// <summary>
		/// Finalizer
		/// </summary>
		~OleDbPoint()
		{
			Dispose();
		}
		#endregion

    }
}
