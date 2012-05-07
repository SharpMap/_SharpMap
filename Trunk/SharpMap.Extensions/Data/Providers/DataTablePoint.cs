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
using System.Collections.ObjectModel;
using System.Data;
using GeoAPI.Geometries;
using Geometry = GeoAPI.Geometries.IGeometry;
using BoundingBox = GeoAPI.Geometries.Envelope;

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
    public class DataTablePoint : PreparedGeometryProvider, IDisposable
    {
        private string _ConnectionString;
        private string _defintionQuery;
        private string _ObjectIdColumn;
        private DataTable _Table;
        private string _XColumn;
        private string _YColumn;

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
            Table = dataTable;
            XColumn = xColumn;
            YColumn = yColumn;
            ObjectIdColumn = oidColumnName;
        }

        /// <summary>
        /// Data table used as the data source.
        /// </summary>
        public DataTable Table
        {
            get { return _Table; }
            set { _Table = value; }
        }


        /// <summary>
        /// Name of column that contains the Object ID
        /// </summary>
        public string ObjectIdColumn
        {
            get { return _ObjectIdColumn; }
            set { _ObjectIdColumn = value; }
        }

        /// <summary>
        /// Name of column that contains X coordinate
        /// </summary>
        public string XColumn
        {
            get { return _XColumn; }
            set { _XColumn = value; }
        }

        /// <summary>
        /// Name of column that contains Y coordinate
        /// </summary>
        public string YColumn
        {
            get { return _YColumn; }
            set { _YColumn = value; }
        }

        /// <summary>
        /// Connectionstring
        /// </summary>
        public string ConnectionString
        {
            get { return _ConnectionString; }
            set { _ConnectionString = value; }
        }

        /// <summary>
        /// Definition query used for limiting dataset
        /// </summary>
        public string DefinitionQuery
        {
            get { return _defintionQuery; }
            set { _defintionQuery = value; }
        }

        #region IProvider Members

        /// <summary>
        /// Returns geometries within the specified bounding box
        /// </summary>
        /// <param name="bbox"></param>
        /// <returns></returns>
        public override Collection<IGeometry> GetGeometriesInView(Envelope bbox)
        {
            DataRow[] drow;
            var features = new Collection<IGeometry>();

            if (Table.Rows.Count == 0)
            {
                return null;
            }

            string strSQL = XColumn + " > " + bbox.Left().ToString(Map.NumberFormatEnUs) + " AND " +
                            XColumn + " < " + bbox.Right().ToString(Map.NumberFormatEnUs) + " AND " +
                            YColumn + " > " + bbox.Bottom().ToString(Map.NumberFormatEnUs) + " AND " +
                            YColumn + " < " + bbox.Top().ToString(Map.NumberFormatEnUs);

            drow = Table.Select(strSQL);

            foreach (DataRow dr in drow)
            {
                features.Add(Factory.CreatePoint(new Coordinate((double) dr[XColumn], (double) dr[YColumn])));
            }

            return features;
        }

        /// <summary>
        /// Returns geometry Object IDs whose bounding box intersects 'bbox'
        /// </summary>
        /// <param name="bbox"></param>
        /// <returns></returns>
        public override Collection<uint> GetObjectIDsInView(BoundingBox bbox)
        {
            DataRow[] drow;
            Collection<uint> objectlist = new Collection<uint>();

            if (Table.Rows.Count == 0)
            {
                return null;
            }

            string strSQL = XColumn + " > " + bbox.Left().ToString(Map.NumberFormatEnUs) + " AND " +
                            XColumn + " < " + bbox.Right().ToString(Map.NumberFormatEnUs) + " AND " +
                            YColumn + " > " + bbox.Bottom().ToString(Map.NumberFormatEnUs) + " AND " +
                            YColumn + " < " + bbox.Top().ToString(Map.NumberFormatEnUs);

            drow = Table.Select(strSQL);

            foreach (DataRow dr in drow)
            {
                objectlist.Add((uint) (int) dr[0]);
            }

            return objectlist;
        }

        /// <summary>
        /// Returns the geometry corresponding to the Object ID
        /// </summary>
        /// <param name="oid">Object ID</param>
        /// <returns>geometry</returns>
        public override Geometry GetGeometryByID(uint oid)
        {
            DataRow[] rows;
            Geometry geom = null;

            if (Table.Rows.Count == 0)
            {
                return null;
            }

            string selectStatement = ObjectIdColumn + " = " + oid;

            rows = Table.Select(selectStatement);

            foreach (DataRow dr in rows)
            {
                geom = Factory.CreatePoint(new Coordinate((double) dr[XColumn], (double) dr[YColumn]));
            }

            return geom;
        }

        /// <summary>
        /// Retrieves all features within the given BoundingBox.
        /// </summary>
        /// <param name="bbox">Bounds of the region to search.</param>
        /// <param name="ds">FeatureDataSet to fill data into</param>
        public override void ExecuteIntersectionQuery(BoundingBox bbox, FeatureDataSet ds)
        {
            DataRow[] rows;

            if (Table.Rows.Count == 0)
            {
                return;
            }

            string statement = XColumn + " > " + bbox.Left().ToString(Map.NumberFormatEnUs) + " AND " +
                               XColumn + " < " + bbox.Right().ToString(Map.NumberFormatEnUs) + " AND " +
                               YColumn + " > " + bbox.Bottom().ToString(Map.NumberFormatEnUs) + " AND " +
                               YColumn + " < " + bbox.Top().ToString(Map.NumberFormatEnUs);

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
                fdr.Geometry = Factory.CreatePoint(new Coordinate((double) dr[XColumn], (double) dr[YColumn]));
            }

            ds.Tables.Add(fdt);
        }

        /// <summary>
        /// Returns the number of features in the dataset
        /// </summary>
        /// <returns>Total number of features</returns>
        public override int GetFeatureCount()
        {
            return Table.Rows.Count;
        }

        /// <summary>
        /// Returns a datarow based on a RowID
        /// </summary>
        /// <param name="rowId"></param>
        /// <returns>datarow</returns>
        public override FeatureDataRow GetFeature(uint rowId)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Computes the full extents of the data source as a 
        /// <see cref="BoundingBox"/>.
        /// </summary>
        /// <returns>
        /// A BoundingBox instance which minimally bounds all the features
        /// available in this data source.
        /// </returns>
        public override BoundingBox GetExtents()
        {
            if (Table.Rows.Count == 0)
            {
                return null;
            }

            BoundingBox box;

            double minX = Double.PositiveInfinity,
                   minY = Double.PositiveInfinity,
                   maxX = Double.NegativeInfinity,
                   maxY = Double.NegativeInfinity;

            foreach (DataRowView dr in Table.DefaultView)
            {
                if (minX > (double) dr[XColumn]) minX = (double) dr[XColumn];
                if (maxX < (double) dr[XColumn]) maxX = (double) dr[XColumn];
                if (minY > (double) dr[YColumn]) minY = (double) dr[YColumn];
                if (maxY < (double) dr[YColumn]) maxY = (double) dr[YColumn];
            }

            box = new BoundingBox(minX, maxX, minY, maxY);

            return box;
        }

        #endregion
    }
}