using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace DelftTools.Utils
{
    public static class ObjectHelper
    {
        public static byte[] ToByteArray(object o)
        {
            var stream = new MemoryStream();
            var formatter = new BinaryFormatter();
            formatter.Serialize(stream, o);
            stream.Close();
            return stream.ToArray();
        }

        public static object FromByteArray(byte[] bytes)
        {
            var stream = new MemoryStream(bytes);
            var formatter = new BinaryFormatter();
            var o = formatter.Deserialize(stream);
            stream.Close();
            return o;
        }

        public static T Clone<T>(object value)
        {
            var clone = Clone(value);
            if(clone == null)
            {
                return default(T);
            }

            return (T) clone;
        }

        public static object Clone(object value)
        {
            if (value == null)
            {
                return null;
            }

            if (value is ICloneable)
            {
                return ((ICloneable)value).Clone();
            }
            
            var valueType = value.GetType();
            if (valueType.IsValueType)
            {
                return value;
            }

            throw new NotSupportedException(string.Format("Value of type {0} object can't be cloned, implement ICloneable", valueType));
        }
    }
}