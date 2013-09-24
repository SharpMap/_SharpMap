using System;
using System.Data;
using NHibernate;
using NHibernate.SqlTypes;
using NHibernate.UserTypes;

namespace NetTopologySuite.Extensions.NHibernate
{
    using System.IO;

    using GeoAPI.Extensions.Feature;

    [Serializable]
    public class FeatureAttributesUserType : IUserType
    {
        public FeatureAttributesUserType()
        {
            this.SqlTypes = new[] { new SqlType(DbType.Binary) };
            this.ReturnedType = typeof(IFeatureAttributeCollection);
            this.IsMutable = true;
        }

        public SqlType[] SqlTypes { get; private set; }

        public Type ReturnedType { get; private set; }

        public bool IsMutable { get; private set; }

        public object NullSafeGet(IDataReader rs, string[] names, object owner)
        {
            var bytes = (byte[])rs.GetValue(rs.GetOrdinal(names[0]));

            IFeatureAttributeCollection attrs = null;

            using (var stream = new MemoryStream(bytes))
            {
                using (var reader = new BinaryReader(stream))
                {
                    var count = reader.ReadInt32();

                    if (count > 0)
                    {
                        this.ReadAllValues(reader, count, out attrs);
                    }
                }
            }

            return attrs;
        }

        public void NullSafeSet(IDbCommand cmd, object value, int index)
        {
            var attrs = (IFeatureAttributeCollection)value;

            if (attrs.Count > 0)
            {
                using (var stream = new MemoryStream())
                {
                    using (var writer = new BinaryWriter(stream))
                    {
                        writer.Write(attrs.Count);
                        this.WriteAllValues(writer, attrs);
                    }

                    NHibernateUtil.Binary.NullSafeSet(cmd, stream.GetBuffer(), index);
                }
            }
        }

        private void ReadAllValues(BinaryReader reader, int count, out IFeatureAttributeCollection attrs)
        {
            /*
                    if (typeof(T) == typeof(double))
                    {
                        var listDouble = (IList<double>)list;
                        for (var i = 0; i < count; i++)
                        {
                            listDouble.Add(reader.ReadDouble());
                        }
                    }
                    else if (typeof(T) == typeof(float))
                    {
                        var listFloat = (IList<float>)list;
                        for (var i = 0; i < count; i++)
                        {
                            listFloat.Add(reader.ReadSingle());
                        }
                    }
                    else if (typeof(T) == typeof(string))
                    {
                        var listString = (IList<string>)list;
                        for (var i = 0; i < count; i++)
                        {
                            listString.Add(reader.ReadString());
                        }
                    }
                    else if (typeof(T) == typeof(bool))
                    {
                        var listBoolean = (IList<bool>)list;
                        for (var i = 0; i < count; i++)
                        {
                            listBoolean.Add(reader.ReadBoolean());
                        }
                    }
                    else if (typeof(T) == typeof(int))
                    {
                        var listInt = (IList<int>)list;
                        for (var i = 0; i < count; i++)
                        {
                            listInt.Add(reader.ReadInt32());
                        }
                    }
                    else if (typeof(T) == typeof(DateTime))
                    {
                        var listDateTime = (IList<DateTime>)list;
                        for (var i = 0; i < count; i++)
                        {
                            listDateTime.Add(new DateTime(reader.ReadInt64()));
                        }
                    }
                }
         */
            attrs = null;
        }

        private void WriteAllValues(BinaryWriter writer, IFeatureAttributeCollection list)
        {
            /*
                    if (typeof(T) == typeof(double))
                    {
                        var listDouble = (IList<double>)list;
                        for (var i = 0; i < list.Count; i++)
                        {
                            writer.Write(listDouble[i]);
                        }
                    }
                    else if (typeof(T) == typeof(float))
                    {
                        var listFloat = (IList<float>)list;
                        for (var i = 0; i < list.Count; i++)
                        {
                            writer.Write(listFloat[i]);
                        }
                    }
                    else if (typeof(T) == typeof(string))
                    {
                        var listString = (IList<string>)list;
                        for (var i = 0; i < list.Count; i++)
                        {
                            writer.Write(listString[i]);
                        }
                    }
                    else if (typeof(T) == typeof(bool))
                    {
                        var listBoolean = (IList<bool>)list;
                        for (var i = 0; i < list.Count; i++)
                        {
                            writer.Write(listBoolean[i]);
                        }
                    }
                    else if (typeof(T) == typeof(int))
                    {
                        var listInt = (IList<int>)list;
                        for (var i = 0; i < list.Count; i++)
                        {
                            writer.Write(listInt[i]);
                        }
                    }
                    else if (typeof(T) == typeof(DateTime))
                    {
                        var listDateTime = (IList<DateTime>)list;
                        for (var i = 0; i < list.Count; i++)
                        {
                            writer.Write(listDateTime[i].Ticks);
                        }
        */
        }

        public bool Equals(object x, object y)
        {
            if (ReferenceEquals(x, y))
            {
                return true;
            }

            if (x == null || y == null)
            {
                return false;
            }

            return x.Equals(y);
        }

        public int GetHashCode(object x)
        {
            return x.GetHashCode();
        }

        public object DeepCopy(object value)
        {
            return value;
        }

        public object Replace(object original, object target, object owner)
        {
            return original;
        }

        public object Assemble(object cached, object owner)
        {
            return this.DeepCopy(cached);
        }

        public object Disassemble(object value)
        {
            return this.DeepCopy(value);
        }
    }
}