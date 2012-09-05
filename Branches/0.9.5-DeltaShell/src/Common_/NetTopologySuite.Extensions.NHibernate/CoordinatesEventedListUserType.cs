using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Xml;
using System.Xml.Serialization;
using DelftTools.Utils.Collections.Generic;
using GeoAPI.Geometries;
using GisSharpBlog.NetTopologySuite.Geometries;
using NHibernate;
using NHibernate.SqlTypes;
using NHibernate.UserTypes;

namespace NetTopologySuite.Extensions.NHibernate
{
    /// <summary>
    /// UserType to map a IList<ICoordinate>. It is returned as a List<Coordinate>
    /// </summary>
    [Serializable]
    public class CoordinatesEventedListUserType:IUserType
    {
        public SqlType[] SqlTypes
        {
            get
            {
                return new[] { new SqlType(DbType.Binary) };
            }
        }

        public Type ReturnedType
        {
            get { return typeof(EventedList<ICoordinate>); }
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

            return FromByteArray((byte[]) value);
        }

        public void NullSafeSet(IDbCommand cmd, object value, int index)
        {
            var newList = new EventedList<ICoordinate>((IEnumerable<ICoordinate>) value);
            var wellKnownBinary = ToByteArray(newList);
            NHibernateUtil.Binary.NullSafeSet(cmd, wellKnownBinary, index);
        }

        public object DeepCopy(object value)
        {
            if (value == null)
            {
                return null;
            }

            var result = new EventedList<ICoordinate>();
            foreach (ICoordinate c in (EventedList<ICoordinate>)value)
            {
                result.Add(new Coordinate(c.X,c.Y,c.Z));
            }
            return result;
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

        private static byte[] ToByteArray(object o)
        {
            var stream = new MemoryStream();
            var formatter = new BinaryFormatter();
            formatter.Serialize(stream, o);
            stream.Close();
            return stream.ToArray();
        }

        private static object FromByteArray(byte[] bytes)
        {
            var stream = new MemoryStream(bytes);
            var formatter = new BinaryFormatter();
            var o = formatter.Deserialize(stream);
            stream.Close();
            return o;
        }

    }
}