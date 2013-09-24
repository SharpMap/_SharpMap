// Copyright 2007 - Dan and Joel
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
using System.Data;
using DelftTools.Utils.Data;
using GeoAPI.CoordinateSystems;
using GeoAPI.Extensions.Feature;
using GeoAPI.Geometries;
using GisSharpBlog.NetTopologySuite.Geometries;
using SharpMap.Api;

namespace SharpMap.Data.Providers
{
    /// <summary>
    /// The DataTablePoint provider is used for rendering point data 
    /// from a System.Data.DataTable.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The data source will need to have two double-type columns, 
    /// xColumn and yColumn that contains the coordinates of the point,
    /// and an integer-type column containing a unique identifier for each row.
    /// </para>
    /// </remarks>
    public class DataTablePoint : Unique<long>, IFeatureProvider, IDisposable
    {
        public Type FeatureType
        {
            get { return typeof (FeatureDataRow); }
        }

        public IList Features
        {
            get { return _Table; }
            set { throw new System.NotImplementedException(); }
        }

        public bool IsReadOnly { get { return true; } } 

        /// <summary>
        /// Initializes a new instance of the DataTablePoint provider
        /// </summary>
        /// <param name="dataTable">
        /// Instance of <see cref="DataTable"/> to use as data source.
        /// </param>
        /// <param name="oidColumnName">
        /// Name of the OID column.
        /// </param>
        /// <param name="xColumn">
        /// Name of column where point's X value is stored.
        /// </param>
        /// <param name="yColumn">
        /// Name of column where point's Y value is stored.
        /// </param>
        public DataTablePoint(DataTable dataTable, string oidColumnName,
                              string xColumn, string yColumn)
        {
            this.Table = dataTable;
            this.XColumn = xColumn;
            this.YColumn = yColumn;
            this.ObjectIdColumn = oidColumnName;
        }

        private FeatureDataTable _Table;

        /// <summary>
        /// Data table used as the data source.
        /// </summary>
        public DataTable Table
        {
            get { return _Table; }
            set { _Table = (FeatureDataTable) value; }
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
            set { _ConnectionString = value; }
        }

        #region IFeatureProvider Members

        public IFeature Add(IGeometry geometry)
        {
            if (geometry is IPoint)
            {
                IPoint point = (IPoint) geometry;
                DataRow newRow = Table.NewRow();
                newRow[XColumn] = point.X;
                newRow[YColumn] = point.Y;
                Table.Rows.Add(newRow);

                FeatureDataTable fdt = new FeatureDataTable(Table);

                foreach (DataColumn col in Table.Columns)
                {
                    fdt.Columns.Add(col.ColumnName, col.DataType, col.Expression);
                }

                FeatureDataRow featureRow = fdt.NewRow();
                featureRow[XColumn] = point.X;
                featureRow[YColumn] = point.Y;

                return featureRow;
            }
            throw new ArgumentOutOfRangeException();
        }

        public Func<IFeatureProvider, IGeometry, IFeature> AddNewFeatureFromGeometryDelegate { get; set; }
        public event EventHandler FeaturesChanged;

        /// <summary>
        /// Returns the geometry corresponding to the Object ID
        /// </summary>
        /// <param name="oid">Object ID</param>
        /// <returns>geometry</returns>
        public IGeometry GetGeometryByID(int oid)
        {
            DataRow[] rows;
            IGeometry geom = null;

            if (Table.Rows.Count == 0)
            {
                return null;
            }

            string selectStatement = ObjectIdColumn + " = " + oid;

            rows = Table.Select(selectStatement);

            foreach (DataRow dr in rows)
            {
                geom = SharpMap.Converters.Geometries.GeometryFactory.CreatePoint((double) dr[XColumn],
                                                                                  (double) dr[YColumn]);
            }

            return geom;
        }

        /// <summary>
        /// Retrieves all features within the given BoundingBox.
        /// </summary>
        /// <param name="bbox">Bounds of the region to search.</param>
        /// <param name="d"></param>
        public IEnumerable<IFeature> GetFeatures(IEnvelope bbox)
        {
            DataRow[] rows;

            if (Table.Rows.Count == 0)
            {
                return null;
            }

            string statement = XColumn + " >= " + bbox.MinX.ToString(Map.numberFormat_EnUS) + " AND " +
                               XColumn + " <= " + bbox.MaxX.ToString(Map.numberFormat_EnUS) + " AND " +
                               YColumn + " >= " + bbox.MinY.ToString(Map.numberFormat_EnUS) + " AND " +
                               YColumn + " <= " + bbox.MaxY.ToString(Map.numberFormat_EnUS);

            rows = Table.Select(statement);

            FeatureDataTable fdt = new FeatureDataTable(Table);

            foreach (DataColumn col in Table.Columns)
            {
                fdt.Columns.Add(col.ColumnName, col.DataType, col.Expression);
            }

            foreach (DataRow dr in rows)
            {
                fdt.ImportRow(dr);
                FeatureDataRow fdr = fdt.Rows[fdt.Rows.Count - 1] as FeatureDataRow;
                fdr.Geometry = SharpMap.Converters.Geometries.GeometryFactory.CreatePoint((double) dr[XColumn],
                                                                                          (double) dr[YColumn]);
            }

            return fdt;
        }

        /// <summary>
        /// Returns the number of features in the dataset
        /// </summary>
        /// <returns>Total number of features</returns>
        public int GetFeatureCount()
        {
            return Table.Rows.Count;
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

            //hack assume index is rowindex
            var rows = Table.Rows[index];

            var featureDataTable = new FeatureDataTable(Table);

            foreach (DataColumn col in Table.Columns)
            {
                featureDataTable.Columns.Add(col.ColumnName, col.DataType, col.Expression);
            }


            featureDataTable.ImportRow(rows);
            var featureDataRow = featureDataTable.Rows[0] as FeatureDataRow;
            featureDataRow.Geometry = SharpMap.Converters.Geometries.GeometryFactory.CreatePoint((double) rows[XColumn],
                                                                                      (double) rows[YColumn]);


            return featureDataRow;
        }

        public virtual bool Contains(IFeature feature)
        {
            throw new NotSupportedException();
        }

        public virtual int IndexOf(IFeature feature)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Computes the full extents of the data source as a 
        /// <see cref="IEnvelope"/>.
        /// </summary>
        /// <returns>
        /// An Envelope instance which minimally bounds all the features
        /// available in this data source.
        /// </returns>
        public IEnvelope GetExtents()
        {
            if (Table.DefaultView.Count == 0) return null;

            IEnvelope envelope = new Envelope();

            foreach (DataRowView row in Table.DefaultView)
            {
                var coordinate = new Coordinate((double) row[XColumn], (double) row[YColumn]);

                if (!envelope.Contains(coordinate))
                {
                    envelope.ExpandToInclude(coordinate);
                }
            }

            return envelope;
        }

        public string Path { get; set; }

        public void CreateNew(string path)
        {
        }

        /// <summary>
        /// Closes the datasource.
        /// </summary>
        public void Close()
        {
        }

        public void Open(string path)
        {
        }

        /// <summary>
        /// Gets true if the datasource is currently open.
        /// </summary>
        public bool IsOpen
        {
            get { return true; }
        }

        private string srsWkt;

        /// <summary>
        /// The spatial reference ID (CRS)
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
        ~DataTablePoint()
        {
            Dispose();
        }

        #endregion
    }
}