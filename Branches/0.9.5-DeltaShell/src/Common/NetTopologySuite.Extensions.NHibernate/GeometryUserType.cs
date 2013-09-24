using System;
using System.Data;
using GeoAPI.Geometries;
using GisSharpBlog.NetTopologySuite.IO;
using NHibernate;
using NHibernate.SqlTypes;
using NHibernate.UserTypes;

namespace NetTopologySuite.Extensions.NHibernate
{
    [Serializable]
    public class GeometryUserType : IUserType
    {
        private static readonly WKBReader wkbReader = new WKBReader();
        private static readonly WKBWriter wkbWriter = new WKBWriter();

        private static WKTReader wktReader = new WKTReader();
        private static WKTWriter wktWriter = new WKTWriter();

        private const bool useWkt = false;

        public SqlType[] SqlTypes
        {
            get
            {
                if (useWkt)
                {
                    return new SqlType[] { new SqlType(DbType.String) };
                }
                else
                {
                    return new SqlType[] { new SqlType(DbType.Binary) };
                }
            }
        }

        public Type ReturnedType
        {
            get { return typeof(IGeometry); }
        }

        public bool Equals(object x, object y)
        {
            if (x == null && y == null)
            {
                return true;
            }

            if (x == null || y == null)
            {
                return false;
            }

            return x.GetHashCode() == y.GetHashCode(); // use hashcode to compare geometries (much faster compare)
        }

        public int GetHashCode(object x)
        {
            return x.GetHashCode();
        }

        /// <summary>
        /// Creates IGeometry instance based on data from a database.
        /// </summary>
        /// <param name="rs"></param>
        /// <param name="names"></param>
        /// <param name="owner"></param>
        /// <returns></returns>
        public object NullSafeGet(IDataReader rs, string[] names, object owner)
        {
            object value = rs.GetValue(rs.GetOrdinal(names[0]));

            if (value == DBNull.Value)
                return null;

            if (useWkt)
            {
                return wktReader.Read((string)value);
            }
            else
            {
                return wkbReader.Read((byte[])value);
            }
        }

        public void NullSafeSet(IDbCommand cmd, object value, int index)
        {
            var geometry = (IGeometry) value;
            
            FixMixedCoordinateStyles(geometry);

            if (useWkt)
            {
                string wellKnownText = null;
                if (value != null)
                {
                    wellKnownText = wktWriter.Write(geometry);
                }
                NHibernateUtil.String.NullSafeSet(cmd, wellKnownText, index);
            }
            else
            {
                byte[] wellKnownBinary = null;
                if (value != null)
                {
                    wellKnownBinary = wkbWriter.Write(geometry);
                }

                NHibernateUtil.Binary.NullSafeSet(cmd, wellKnownBinary, index);
            }
        }

        /// <summary>
        /// Some (2d) coordinates are x,x,0 and others x,x,NaN. This is not ok within the same geometry 
        /// and memory allocation later on will fail. So if we detect this mixed style, we fix the 
        /// entire geometry. Typically (always?) the difference is in the first and last coordinate, so 
        /// we use that assumption for performance reasons
        /// </summary>
        /// <remarks>
        /// This starts failing if we want to store 3D geometries!
        /// </remarks>
        /// <param name="geometry"></param>
        private static void FixMixedCoordinateStyles(IGeometry geometry)
        {
            //todo: add 2d / 3d checks?

            if (geometry == null)
                return;

            if (geometry.Coordinates.Length < 2)
                return;

            var firstCoord = geometry.Coordinates[0];
            var secondCoord = geometry.Coordinates[1];

            var firstIsNaN = double.IsNaN(firstCoord.Z);
            var secondIsNaN = double.IsNaN(secondCoord.Z);
            if (firstIsNaN ^ secondIsNaN) //^ = xor
            {
                //apply 0 to all
                foreach (var coord in geometry.Coordinates)
                {
                    coord.Z = 0;
                }
            }
        }

        public object DeepCopy(object value)
        {
            if (value == null)
            {
                return null;
            }

            return ((IGeometry)value).Clone();
        }

        public bool IsMutable
        {
            get { return true; }
        }

        public object Replace(object original, object target, object owner)
        {
            return original;
        }

        public object Assemble(object cached, object owner)
        {
            return cached;
        }

        public object Disassemble(object value)
        {
            return value;
        }
    }
}