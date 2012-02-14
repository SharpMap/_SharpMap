using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using DelftTools.Utils.Reflection;
using NUnit.Framework;

namespace DelftTools.TestUtils
{
    public static class ReflectionTestHelper
    {
        public static void AssertPublicPropertiesAreEqual<T>(T obj1, T obj2)
        {
            var properties = GetPublicInstanceProperties(obj1);
            foreach (var p in properties)
            {

                var value1 = TypeUtils.GetPropertyValue(obj1, p.Name);
                var value2 = TypeUtils.GetPropertyValue(obj2, p.Name);
                string message = String.Format("Property {0} is not equal.", p.Name);
                if (value1 is double) 
                {
                    Assert.AreEqual((double)value1,(double)value2,0.000001,message);
                }
                else
                {
                    Assert.AreEqual(value1, value2, message);    
                }
                
            }
        }

        public static void FillRandomValuesForValueTypeProperties(object obj,params string[] excludedProperties)
        {
            IEnumerable<PropertyInfo> properties = GetPublicInstanceProperties(obj);
            foreach (var p in properties.Where(p=>!excludedProperties.Contains(p.Name))){
                    
                //set random value 
                var randomValue = GetRandomValue(p.PropertyType);
                MethodInfo methodInfo = p.GetSetMethod();
                if (methodInfo != null)
                {
                    methodInfo.Invoke(obj, new[] {randomValue});
                }
            }
        }

        public static IEnumerable<PropertyInfo> GetPublicInstanceProperties(object obj)
        {
            return obj.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public)
                .Where(p => p.Name != "Id").Where(p => p.PropertyType.IsValueType || p.PropertyType == typeof (string));
        }

        public static IEnumerable<PropertyInfo> GetPublicListProperties(object obj)
        {
            var propertyInfos = obj.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public);
            return propertyInfos.Where(p => p.Name != "Id" && IsList(p));
        }

        private static bool IsList(PropertyInfo p)
        {
            if (p.PropertyType is IList)
            {
                return true;
            }
            var genericParameterType = TypeUtils.GetFirstGenericTypeParameter(p.PropertyType);
            if (genericParameterType != null)
            {
                var genericList = typeof (IList<>).MakeGenericType(genericParameterType);
                return genericList.IsAssignableFrom(p.PropertyType);
            }
            return false;
        }

        private static readonly Random rnd = new Random();

        private static object GetRandomValue(Type propertyType)
        {
            if (propertyType == typeof(double))
            {
                return (double) rnd.Next(100)/10;
            }
            if (propertyType == typeof(int))
            {
                return rnd.Next(100) ;
            }
            if (propertyType == typeof(uint))
            {
                return Convert.ToUInt32(rnd.Next(100));
            }
            if (propertyType == typeof(string))
            {
                return rnd.Next(100).ToString();
            }
            if (propertyType == typeof(bool))
            {
                return rnd.Next(100) < 50;
            }
            if (propertyType == typeof(TimeSpan))
            {
                return new TimeSpan(rnd.Next(100));
            }
            if (propertyType == typeof(DateTime))
            {
                return new DateTime(2000, 1, 1).AddDays(rnd.Next(100));
            }
            
            throw new NotImplementedException();
        }
    }
}