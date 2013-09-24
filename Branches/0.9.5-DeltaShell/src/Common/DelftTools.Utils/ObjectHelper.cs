using System;
using System.Collections;
using System.Collections.Generic;
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

        /// <exception cref="NotSupportedException">when <paramref name="value"/> does not inherit from <see cref="ICloneable"/>, is not null, or is not a value type.</exception>
        public static T Clone<T>(object value)
        {
            var clone = Clone(value);
            if(clone == null)
            {
                return default(T);
            }

            return (T) clone;
        }

        /// <exception cref="NotSupportedException">when <paramref name="value"/> does not inherit from <see cref="ICloneable"/>, is not null, or is not a value type.</exception>
        public static object Clone(object value)
        {
            if (value == null)
            {
                return null;
            }

            var cloneable = value as ICloneable;
            if (cloneable != null)
            {
                return cloneable.Clone();
            }

            var deepCloneable = value as IDeepCloneable;
            if (deepCloneable != null)
            {
                return deepCloneable.DeepClone();
            }
            
            var valueType = value.GetType();
            if (valueType.IsValueType)
            {
                return value;
            }

            throw new NotSupportedException(String.Format("Value of type {0} object can't be cloned, implement ICloneable", valueType));
        }

        public static void RefreshItemsInList<T>(IList<T> itemsToRefresh, IList<T> itemsBefore, IList<T> itemsAfter)
        {
            RefreshItemsInList((IList) itemsToRefresh, (IList) itemsBefore, (IList) itemsAfter);
        }

        public static void RefreshItemsInList(IList itemsToRefresh, IList itemsBefore, IList itemsAfter)
        {
            for (var i = 0; i < itemsToRefresh.Count; i++)
            {
                var indexBefore = itemsBefore.IndexOf(itemsToRefresh[i]);

                if (indexBefore == -1)
                {
                    throw new InvalidOperationException(
                        string.Format(
                            "Attepting to replace features in feature coverage, but could not find feature in original context: {0}",
                            itemsToRefresh[i]));
                }

                var featureAfter = itemsAfter[indexBefore];
                itemsToRefresh[i] = featureAfter;
            }
        }
    }
}