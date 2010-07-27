using System;
using System.Collections.Generic;
using System.Data;
using System.Reflection;
using GeoAPI.Extensions.Feature;

namespace NetTopologySuite.Extensions.Features
{
    public class FeatureAttributeAccessorHelper
    {
        public static T GetAttributeValue<T>(IFeature feature, string name)
        {
            return GetAttributeValue<T>(feature, name, null);
        }

        public static T GetAttributeValue<T>(IFeature feature, string name, object noDataValue)
        {
            object value = GetAttributeValue(feature, name);

            if (value == DBNull.Value)
            {
                if (noDataValue == null)
                {
                    return (T) Activator.CreateInstance(typeof (T));
                }

                return (T) Convert.ChangeType(noDataValue, typeof (T));
            }

            if (typeof (T) == typeof (string))
            {
                if(value != null && value.GetType().IsEnum)
                {
                    return (T)(object)value.ToString();
                }
                return (T) (object) string.Format("{0:g4}", value);
            }
            if (typeof (T) == typeof (double))
            {
                return (T) (object) Convert.ToDouble(value);
            }
            if (typeof (T) == typeof (int))
            {
                return (T) (object) Convert.ToInt32(value);
            }
            if (typeof (T) == typeof (short))
            {
                return (T) (object) Convert.ToInt16(value);
            }
            if (typeof (T) == typeof (float))
            {
                return (T) (object) Convert.ToSingle(value);
            }
            if (typeof (T) == typeof (byte))
            {
                return (T) (object) Convert.ToByte(value);
            }
            if (typeof (T) == typeof (long))
            {
                return (T) (object) Convert.ToInt64(value);
            }

            return (T) value;
        }

        public static object GetAttributeValue(IFeature feature, string name)
        {
            if (feature.Attributes != null)
            {
                if (feature.Attributes.ContainsKey(name))
                {
                    return feature.Attributes[name];
                }
            }

            if (feature is DataRow)
            {
                return ((DataRow) feature)[name];
            }

            // search in all properties marked [FeatureAttribute]
            Type featureType = feature.GetType();
            foreach (PropertyInfo info in featureType.GetProperties())
            {
                object[] propertyAttributes = info.GetCustomAttributes(true);

                if (propertyAttributes.Length == 0)
                {
                    continue;
                }

                foreach (object propertyAttribute in propertyAttributes)
                {
                    if (propertyAttribute is FeatureAttributeAttribute && info.Name.Equals(name))
                    {
                        MethodInfo getMethod = info.GetGetMethod(true);
                        return getMethod.Invoke(feature, null);
                    }
                }
            }

            throw new ArgumentOutOfRangeException("Cant find attribute name: " + name);
        }

        public static void SetAttributeValue(IFeature feature, string name, object value)
        {
            if (feature is DataRow)
            {
                ((DataRow) feature)[name] = value;
                return;
            }

            // search in all properties marked [FeatureAttribute]
            Type featureType = feature.GetType();
            foreach (PropertyInfo info in featureType.GetProperties())
            {
                object[] propertyAttributes = info.GetCustomAttributes(true);

                if (propertyAttributes.Length == 0)
                {
                    continue;
                }

                foreach (object propertyAttribute in propertyAttributes)
                {
                    if (propertyAttribute is FeatureAttributeAttribute && info.Name.Equals(name))
                    {
                        MethodInfo setMethod = info.GetSetMethod(true);
                        setMethod.Invoke(feature, new[] {value});
                    }
                }
            }

            throw new ArgumentOutOfRangeException("Cant find attribute name: " + name);
        }

        public static Type GetAttributeType(IFeature feature, string name)
        {
            Type featureType = feature.GetType();

            if (feature.Attributes != null)
            {
                if (feature.Attributes.ContainsKey(name))
                {
                    return feature.Attributes[name].GetType();
                }
            }

            if (feature is DataRow)
            {
                DataRow row = (DataRow) feature;
                if (row.Table.Columns.Contains(name))
                {
                    return row.Table.Columns[name].DataType;
                }
            }

            foreach (PropertyInfo info in featureType.GetProperties())
            {
                object[] propertyAttributes = info.GetCustomAttributes(true);

                if (propertyAttributes.Length == 0)
                {
                    continue;
                }

                foreach (object propertyAttribute in propertyAttributes)
                {
                    if (propertyAttribute is FeatureAttributeAttribute && info.Name.Equals(name))
                    {
                        return info.PropertyType;
                    }
                }
            }
            
            throw new ArgumentOutOfRangeException("Cant find attribute name: " + name);
        }

        public static string[] GetAttributeNames(IFeature feature)
        {
            var attributeNames = new List<string>();
            
            // add dynamic attributes
            if (feature.Attributes != null && feature.Attributes.Count != 0)
            {
                foreach (var name in feature.Attributes.Keys)
                {
                    attributeNames.Add(name);
                }
                return attributeNames.ToArray();
            }

            if (feature is DataRow)
            {
                DataRow row = (DataRow) feature;
                foreach (DataColumn column in row.Table.Columns)
                {
                    attributeNames.Add(column.ColumnName);
                }

                return attributeNames.ToArray();
            }

            Type featureType = feature.GetType();

            foreach(var attributeName in GetAttributeNames(featureType))
            {
                attributeNames.Add(attributeName);
            }

            return attributeNames.ToArray();
        }

        public static IEnumerable<string> GetAttributeNames(Type featureType)
        {
            foreach (PropertyInfo info in featureType.GetProperties())
            {
                object[] propertyAttributes = info.GetCustomAttributes(true);

                if (propertyAttributes.Length == 0)
                {
                    continue;
                }

                foreach (object propertyAttribute in propertyAttributes)
                {
                    if (propertyAttribute is FeatureAttributeAttribute)
                    {
                        yield return info.Name;
                    }
                }
            }
        }
    }
}