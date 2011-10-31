using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Data.Common;
using System.Diagnostics;
using System.Linq;
using System.Text;
using GeoAPI.Geometries;
using SharpMap.Geometries;

namespace SharpMap.Data.Providers
{
    public abstract class DbBase<TOid, TConnection, TGeometry> : IUpdateableProvider
        where TOid : struct, IComparable<TOid>
        where TConnection : DbConnection, new()
        where TGeometry : GeoAPI.Geometries.IGeometry
    {
        private readonly string _connectionString;
        private readonly Dictionary<TOid, uint> _oidDictionary;
        private readonly DbGeometryConverter<TGeometry> _dbGeometryConverter;

        protected int Srid;

        protected readonly DataTable SchemaTable;

        /// <summary>
        /// Gets the name of the Schema
        /// </summary>
        public string Schema { get; protected set; }
        
        /// <summary>
        /// Gets the name of the Table
        /// </summary>
        public string Table { get; protected set; }
        
        /// <summary>
        /// Gets the name of the object identifier column
        /// </summary>
        public DataColumn OidColumn { get; protected set; }

        /// <summary>
        /// Gets the name of the geometry column
        /// </summary>
        public string GeometryColumn { get; protected set; }
        
        /// <summary>
        /// Gets the names of the columns of interest
        /// </summary>
        public DataColumn[] Columns { get; protected set; }

        /// <summary>
        /// Gets or sets the As alias
        /// </summary>
        public string As { get; protected set; }

        /// <summary>
        /// Gets the qualified table name, made up from <see cref="Schema"/> and <see cref="Table"/>. If <see cref="As"/> has a valid value, the
        /// </summary>
        protected string QualifiedTable
        {
            get
            {
                var sb = new StringBuilder();
                sb.Append((string.IsNullOrEmpty(Schema)) ?
                    string.Format(EntityDecorator, Table) :
                    DecorateEntities(EntityDecorator, new[] {Schema, Table}));

                if (!string.IsNullOrEmpty(As))
                    sb.AppendFormat(" AS {0}", As);
                
                return sb.ToString();
            }
        }

        protected string QualifiedColumn(string column)
        {
            var sb = new StringBuilder();
            AddQualifiedColumn(column, sb);
            return sb.ToString();
        }

        private void AddQualifiedColumn(string column, StringBuilder sb)
        {
            if (!String.IsNullOrEmpty(As))
                sb.AppendFormat("{0}.", As);
            sb.AppendFormat(EntityDecorator, column);
        }

        protected string QualifiedColumns(IList<string> columns)
        {
            Debug.Assert(columns != null, "columns != null");
            Debug.Assert(columns.Count > 0);
            
            var sb = new StringBuilder();
            AddQualifiedColumn(columns[0], sb);
            for (var i = 1; i < columns.Count; i++)
            {
                sb.Append(", ");
                AddQualifiedColumn(columns[i], sb);
            }
            return sb.ToString();
        }

        protected DbBase(string connectionString, string table)
            :this(connectionString, string.Empty, table)
        {}

        protected DbBase(string connectionString, string schema, string table)
        {
            _connectionString = connectionString;
            Schema = schema;
            Table = table;

            DataColumn oidColumn;
            string geometryColumn;
            GetOidAndGeometryColumn(out oidColumn, out geometryColumn);
            OidColumn = oidColumn;
            GeometryColumn = geometryColumn;
        }

        private void GetOidAndGeometryColumn(out DataColumn oidColumn, out string geometryColumn)
        {
            oidColumn = null;
            geometryColumn = null;
        }

        protected DbBase(string connectionString, string table, string oidColumn, string geometryColumn)
            :this(connectionString, string.Empty, table, oidColumn, geometryColumn)
        {}
        
        /// <summary>
        /// Creates an instance of this class
        /// </summary>
        /// <param name="connectionString"></param>
        /// <param name="schema"></param>
        /// <param name="table">The table/view name</param>
        /// <param name="oidColumn">The name of the identifier column</param>
        /// <param name="geometryColumn">The name of the geometry column</param>
        protected DbBase(string connectionString, string schema, string table, string oidColumn, string geometryColumn)
        {
            _connectionString = connectionString;
            Schema = schema;
            Table = table;
            Initialize(oidColumn, geometryColumn);
        }

        private void Initialize(string oidColumn, string geometryColumn)
        {
            SchemaTable = GetSchemaTable();
            

            if (Columns == null)
                Columns = GetOtherColumns();

        }




        private void GetSchemaTable()
        {
            using (var conn = GetOpenConnection())
            {
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = GenerateFullSelect(1);
                    cmd.ExecuteScalar()
                }
            }
        }

        private string[] GetOtherColumns()
        {
            

        }

        private string _parameterDecorator = "@P{0}";
        protected string ParameterDecorator
        {
            get { return _parameterDecorator; }
            set
            {
                if (string.IsNullOrEmpty(value) || !(value.Contains("{0}") || value=="?"))
                    throw new ArgumentException("A parameter decorator must contain \"{0}\", or be \"?\"");
                _parameterDecorator = value;
            }
        }

        private string _entityDecorator = "\"{0}\"";
        protected string EntityDecorator
        {
            get { return _entityDecorator; }
            set
            {
                if (string.IsNullOrEmpty(value) || !value.Contains("{0}"))
                    throw new ArgumentException("An entity decorator must contain \"{0}\"");
                _entityDecorator = value;
            }
        }

        private string _geometryDecoration;
        protected string GeometryDecoration
        {
            get { return _geometryDecoration; }
            set
            {
                if (string.IsNullOrEmpty(value) || !value.Contains("{0}"))
                    throw new ArgumentException("An entity decorator must contain \"{0}\"");
                _geometryDecoration = value;
            }
        }

        protected static string DecorateEntities(string entityDecorator, IList<string> entities)
        {
            var sb = new StringBuilder(string.Format(entityDecorator, entities[0]));
            for (var i = 1; i < entities.Count; i++ )
            {
                sb.AppendFormat(entityDecorator, entities[i]);
            }
            return entities.ToString();
        }

        protected abstract string SpatialRelationClause(SpatialRelations relation);


        #region Implementation of IDisposable

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (disposing)
            {
                OnDispose();
            }
            OnFinalize();
        }

        protected virtual void OnDispose()
        {
        }

        protected virtual void OnFinalize()
        {
        }

        #endregion

        #region Implementation of IProvider

        public string ConnectionID
        {
            get { return _connectionString; }
        }

        public bool IsOpen
        {
            get { return false; }
        }

        public virtual int SRID
        {
            get { return Srid; }
            set { Srid = value; }
        }

        protected abstract TConnection GetOpenConnection();

        public Collection<Geometry> GetGeometriesInView(Envelope bbox)
        {
            var res = new Collection<Geometry>();
            using (var cn = GetOpenConnection())
            {

            }
        }

        public Collection<uint> GetObjectIDsInView(Envelope bbox)
        {
            throw new NotImplementedException();
        }

        protected abstract object GetEnvelope(Envelope bbox);

        public Geometry GetGeometryByID(uint oid)
        {
            
            throw new NotImplementedException();
        }

        public void ExecuteIntersectionQuery(Geometry geom, FeatureDataSet ds)
        {
            ExecuteSpatialPredicateQuery(SpatialPredicate.Intersects, geom, ds);
        }

        public void ExecuteIntersectionQuery(Envelope box, FeatureDataSet ds)
        {
            ExecuteSpatialPredicateQuery(SpatialPredicate.Intersects, box, ds);
        }

        public void ExecuteSpatialPredicateQuery(SpatialPredicate spatialRelation, Geometries.Geometry geometry, FeatureDataSet dataSet)
        {
        }

        public void ExecuteSpatialPredicateQuery(SpatialPredicate spatialRelation, Envelope geometry, FeatureDataSet dataSet)
        {
            FeatureDataTable table = 
        }

        public int GetFeatureCount()
        {
            throw new NotImplementedException();
        }

        public FeatureDataRow GetFeature(uint rowId)
        {
        }

        public FeatureDataRow GetFeature(TOid oid)
        {
            return null;
        }

        private Envelope _cachedExtent;
        public Envelope GetExtents()
        {
            return _cachedExtent ?? (_cachedExtent = GetExtentsInternal());
        }

        protected abstract Envelope GetExtentsInternal();

        public void Open()
        {
            // nothing to do
        }

        public void Close()
        {
            //nothing to do
        }

        #endregion

        #region Implementation of IUpdateableProvider

        public void Update(FeatureDataTable table)
        {
            var deleted = table.GetChanges(DataRowState.Deleted);
            Delete(deleted);
            
            var added = table.GetChanges(DataRowState.Added);
            Insert(added);
            
            var modified = table.GetChanges(DataRowState.Modified);
            Update(modified);

            table.AcceptChanges();
        }

        public void Insert(FeatureDataRow featureDataRow)
        {
            throw new NotImplementedException();
        }

        public void Insert(DataTable featureDataRows)
        {
            if (featureDataRows == null || featureDataRows.Rows.Count > 0)
                return;
        }

        public void Update(FeatureDataRow featureDataRow)
        {
            throw new NotImplementedException();
        }

        public void Update(DataTable featureDatatable)
        {
            if (featureDatatable == null || featureDatatable.Rows.Count > 0)
                return;

            var featureDataRows = featureDatatable.Rows;

            using (var conn = GetOpenConnection())
            {
                var trans = conn.BeginTransaction();
                try
                {
                    using (var cmd = GetUpdateCommand(conn))
                    {
                        var pars = cmd.Parameters;
                        foreach (FeatureDataRow featureDataRow in featureDataRows)
                        {
                            SetParameters(pars, featureDataRow);
                            cmd.ExecuteNonQuery();
                        }
                    }
                    trans.Commit();
                }
                catch (Exception)
                {
                    trans.Rollback();
                    throw;
                }
            }
        }

        private void SetParameters(DbParameterCollection pars, FeatureDataRow featureDataRow)
        {
            pars[0].Value = featureDataRow[OidColumn.ColumnName, DataRowVersion.Current];
            pars[1].Value = ToStoreGeometry(featureDataRow.Geometry);
            for (var i = 0; i < Columns.Length; i++)
                pars[i + 2].Value = featureDataRow[Columns[i].ColumnName];
        }

        public void Delete(FeatureDataRow featureDataRow)
        {
            throw new NotImplementedException();
        }

        public void Delete(DataTable featureDataTable)
        {
            if (featureDataTable == null || featureDataTable.Rows.Count > 0)
                return;

            var featureDataRows = featureDataTable.Rows;

            using (var conn = GetOpenConnection())
            {
                var trans = conn.BeginTransaction();
                try
                {
                    using (var cmd = GetDeleteCommand(conn))
                    {
                        foreach (DataRow featureDataRow in featureDataRows)
                        {
                            cmd.Parameters[0].Value = featureDataRow[OidColumn];
                            cmd.ExecuteNonQuery();
                        }
                    }
                    trans.Commit();
                }
                catch (Exception)
                {
                    trans.Rollback();
                }
            }
        }

        protected static DbParameter CreateParameter(DbCommand cmd, string name, DbType type, object value)
        {
            var res = CreateParameter(cmd, name, type);
            res.Value = value;
            return res;
        }

        protected DbParameter CreateParameter(DbCommand cmd, DataColumn column)
        {
            var res = cmd.CreateParameter();
            res.ParameterName = column.ColumnName;
            res.DbType = ToDbType(column.DataType);
            return res;
        }

        protected virtual DbType ToDbType(Type dataType)
        {
            var typeCode = Type.GetTypeCode(dataType);
            switch (typeCode)
            {
                case TypeCode.Boolean:
                    return DbType.Boolean;
                
                case TypeCode.Byte:
                    return DbType.Byte;
                case TypeCode.SByte:
                    return DbType.SByte;
                
                case TypeCode.DateTime:
                    return DbType.DateTime;
                
                case TypeCode.Int16:
                    return DbType.Int16;
                case TypeCode.Int32:
                    return DbType.Int32;
                case TypeCode.Int64:
                    return DbType.Int64;

                case TypeCode.Object:
                    return DbType.Binary;

                case TypeCode.Single:
                    return DbType.Single;
                case TypeCode.Double:
                    return DbType.Double;
                case TypeCode.Decimal:
                    return DbType.Decimal;
                
                case TypeCode.String:
                    return DbType.String;

                case TypeCode.UInt16:
                    return DbType.UInt64;
                case TypeCode.UInt32:
                    return DbType.UInt32;
                case TypeCode.UInt64:
                    return DbType.UInt64;

// ReSharper disable RedundantCaseLabel
                case TypeCode.DBNull:
                case TypeCode.Empty:
                default:
                    throw new ArgumentException();
                    //return DbType.Object;
// ReSharper restore RedundantCaseLabel
            }
        }

        protected static DbParameter CreateParameter(DbCommand cmd, string name, DbType type)
        {
            var res = cmd.CreateParameter();
            res.ParameterName = name;
            res.DbType = type;
            return res;
        }

        private DbCommand GetDeleteCommand(TConnection conn)
        {
            var res = conn.CreateCommand();
            res.CommandText = string.Format("DELETE FROM {0} WHERE {1}=@POid;", QualifiedTable, QualifiedColumn(OidColumn.ColumnName));
            res.Parameters.Add(CreateParameter(res, "POid", DbType.UInt32));
            return res;
        }

        private DbCommand GetInsertCommand(DbConnection conn)
        {
            var alias = As;
            As = String.Empty;
            var res = conn.CreateCommand();
            res.CommandText = string.Format("INSERT INTO {0} ({1}) VALUES ({2});", 
                QualifiedTable, AllColumns(), AllParameters(res, false));
            As = alias;
            return res;
        }

        private DbCommand GetUpdateCommand(TConnection conn)
        {
            var alias = As;
            As = String.Empty;
            var res = conn.CreateCommand();
            res.CommandText = string.Format("UPDATE {0} {1} WHERE {2}=@P_Where_;",
                QualifiedTable, AllSet(), QualifiedColumn(OidColumn.ColumnName));
            res.Parameters.Add(CreateParameter(res, "_Where_", DbType.UInt32));
            As = alias;
            return res;
        }

        private string AllColumns()
        {
            var list = new List<string> {OidColumn.ColumnName, GeometryColumn};
            if (Columns != null)
            foreach (var dataColumn in Columns)
            {
                    list.Add(dataColumn.ColumnName);
            }
            return QualifiedColumns(list);
        }

        private string AllParameters(DbCommand cmd, bool where)
        {
            cmd.Parameters.Add(CreateParameter(cmd, OidColumn));
            cmd.Parameters.Add(CreateParameter(cmd, GeometryColumn, DbType.Binary));

            var list = new List<string>
                           {
                               string.Format(ParameterDecorator, OidColumn), 
                               ToStoreGeometry(string.Format(ParameterDecorator, GeometryColumn))
                           };

            if (Columns != null)
            {
                foreach (var column in Columns)
                {
                    cmd.Parameters.Add(CreateParameter(cmd, column));
                    list.Add(string.Format(ParameterDecorator, OidColumn));
                }
            }

            if (where)
            {
                var par = CreateParameter(cmd, "_Where_", DbType.UInt32);
                par.SourceVersion = DataRowVersion.Original;
                cmd.Parameters.Add(par);
            }

            return string.Join(", ", list);
        }

        private DbCommand GetUpdateCommand(DbConnection conn)
        {
            var res = conn.CreateCommand();
            res.CommandText = string.Format("DELETE FROM {0} WHERE {1}=@POid;", DecorateEntities(EntityDecorator, new[] { Table }));
            res.Parameters.Add(CreateParameter(res, "POid", DbType.UInt32));
            return res;
        }

        #endregion
    }
}
