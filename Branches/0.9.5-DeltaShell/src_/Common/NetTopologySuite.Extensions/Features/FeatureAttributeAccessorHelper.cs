using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Reflection;
using DelftTools.Utils.Reflection;
using GeoAPI.Extensions.Feature;

namespace NetTopologySuite.Extensions.Features
{
    public class FeatureAttributeAccessorHelper
    {
        public static T GetAttributeValue<T>(IFeature feature, string name)
        {
            return GetAttributeValue<T>(feature, name, null);
        }
        
        public static T GetAttributeValue<T>(IFeature feature, string name, object noDataValue, bool throwOnNotFound = true)
        {
            object value = GetAttributeValue(feature, name, throwOnNotFound);

            if (value == null || value == DBNull.Value)
            {
                if (noDataValue == null)
                {
                    return (T)Activator.CreateInstance(typeof(T));
                }

                return (T)Convert.ChangeType(noDataValue, typeof(T));
            }

            if (typeof(T) == typeof(string))
            {
                if (value != null && value.GetType().IsEnum)
                {
                    return (T)(object)value.ToString();
                }
                return (T)(object)string.Format("{0:g}", value);
            }
            if (typeof(T) == typeof(double))
            {
                return (T)(object)Convert.ToDouble(value, CultureInfo.InvariantCulture);
            }
            if (typeof(T) == typeof(int))
            {
                return (T)(object)Convert.ToInt32(value, CultureInfo.InvariantCulture);
            }
            if (typeof(T) == typeof(short))
            {
                return (T)(object)Convert.ToInt16(value, CultureInfo.InvariantCulture);
            }
            if (typeof(T) == typeof(float))
            {
                return (T)(object)Convert.ToSingle(value, CultureInfo.InvariantCulture);
            }
            if (typeof(T) == typeof(byte))
            {
                return (T)(object)Convert.ToByte(value, CultureInfo.InvariantCulture);
            }
            if (typeof(T) == typeof(long))
            {
                return (T)(object)Convert.ToInt64(value, CultureInfo.InvariantCulture);
            }

            return (T)value;
        }

        private static IDictionary<Type, IDictionary<string, MethodInfo>> getterCache = new Dictionary<Type, IDictionary<string, MethodInfo>>();

        public static object GetAttributeValue(IFeature feature, string name, bool throwOnNotFound=true)
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

            var getter = GetCachedAttributeValueGetter(feature, name, throwOnNotFound);

            if (getter != null)
            {
                return getter.Invoke(feature, null);
            }

            if (throwOnNotFound)
            {
                ThrowOnNotFound(name);
            }
            return null;
        }

        private static MethodInfo GetCachedAttributeValueGetter(IFeature feature, string name, bool throwOnNotFound)
        {
            MethodInfo getter = null;
            IDictionary<string, MethodInfo> gettersForType = null;
            if (getterCache.ContainsKey(feature.GetType()))
            {
                gettersForType = getterCache[feature.GetType()];
            }
            else
            {
                gettersForType = new Dictionary<string, MethodInfo>();
                getterCache.Add(feature.GetType(), gettersForType);
            }

            if (gettersForType.ContainsKey(name))
            {
                getter = gettersForType[name];
            }
            else
            {
                getter = GetAttributeValueGetter(feature, name, throwOnNotFound);
                gettersForType.Add(name, getter);
            }
            return getter;
        }

        private static MethodInfo GetAttributeValueGetter(IFeature feature, string name, bool throwOnNotFound=true)
        {
            // search in all properties marked [FeatureAttribute]
            var featureType = feature.GetType();
            foreach (var info in featureType.GetProperties())
            {
                object[] propertyAttributes = info.GetCustomAttributes(true);

                if (propertyAttributes.Length == 0)
                {
                    continue;
                }

                foreach (var propertyAttribute in propertyAttributes)
                {
                    if (propertyAttribute is FeatureAttributeAttribute && info.Name.Equals(name))
                    {
                        MethodInfo getMethod = info.GetGetMethod(true);
                        return getMethod;
                    }
                }
            }

            if (throwOnNotFound)
            {
                ThrowOnNotFound(name);
            }

            return null;
        }

        private static void ThrowOnNotFound(string name)
        {
            throw new ArgumentOutOfRangeException("Cant find attribute name: " + name);
        }

        public static void SetAttributeValue(IFeature feature, string name, object value)
        {
            if (feature is DataRow)
            {
                ((DataRow)feature)[name] = value;
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
                        setMethod.Invoke(feature, new[] { value });
                    }
                }
            }

            throw new ArgumentOutOfRangeException("Cant find attribute name: " + name);
        }

        public static string GetAttributeDisplayName(Type featureType, string name)
        {
            if (featureType.Implements<DataRow>())
                return name;//no custom stuff for datarows..

            foreach (PropertyInfo info in featureType.GetProperties().Where(pi => pi.Name.Equals(name)))
            {
                var featureAttibute = info.GetCustomAttributes(true).OfType<FeatureAttributeAttribute>().FirstOrDefault();
                if (featureAttibute != null)
                {
                    return !string.IsNullOrEmpty(featureAttibute.DisplayName) ?
                        featureAttibute.DisplayName :  //return displayname of the feature 
                        info.Name;                     //not defined displayname is property name
                }
            }

            throw new InvalidOperationException(string.Format("Attribute named {0} not found on feature type {1}", name, featureType));
        }

        public static string GetAttributeDisplayName(IFeature feature, string name)
        {
            if (feature is DataRow)
                return name;//no custom stuff for datarows..

            foreach (PropertyInfo info in feature.GetType().GetProperties().Where(pi => pi.Name.Equals(name)))
            {
                var featureAttibute = info.GetCustomAttributes(true).OfType<FeatureAttributeAttribute>().FirstOrDefault();
                if (featureAttibute != null)
                {
                    return !string.IsNullOrEmpty(featureAttibute.DisplayName) ?
                        featureAttibute.DisplayName :  //return displayname of the feature 
                        info.Name;                     //not defined displayname is property name
                }
            }

            if (feature.Attributes.ContainsKey(name))
            {
                return name;
            }

            throw new InvalidOperationException(string.Format("Attribute named {0} not found on feature {1}", name, feature.ToString()));
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
                DataRow row = (DataRow)feature;
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

            if (feature is DataRow)
            {
                DataRow row = (DataRow)feature;
                foreach (DataColumn column in row.Table.Columns)
                {
                    attributeNames.Add(column.ColumnName);
                }

                return attributeNames.ToArray();
            }

            // add dynamic attributes
            if (feature.Attributes != null && feature.Attributes.Count != 0)
            {
                foreach (var name in feature.Attributes.Keys)
                {
                    attributeNames.Add(name);
                }
            }

            Type featureType = feature.GetType();

            foreach (var attributeName in GetAttributeNames(featureType))
            {
                attributeNames.Add(attributeName);
            }

            return attributeNames.ToArray();
        }


        public static IEnumerable<string> GetAttributeNames(Type featureType)
        {
            //var result = new List<string>();
            return from info in featureType.GetProperties()
                   let propertyAttributes = info.GetCustomAttributes(true)
                   where propertyAttributes.Any(a => a is FeatureAttributeAttribute)
                   select info.Name;
        }
    }
}