using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using DelftTools.Utils;
using DelftTools.Utils.Collections;
using GeoAPI.Geometries;
using NUnit.Framework;

namespace DelftTools.TestUtils.TestReferenceHelper
{
    public static class TestReferenceHelper
    {
        private class ReferenceEqualsComparer<T> : IEqualityComparer<T>
        {
            public bool Equals(T x, T y)
            {
                return ReferenceEquals(x, y);
            }

            public int GetHashCode(T obj)
            {
                return RuntimeHelpers.GetHashCode(obj);
            }
        }
        public static IEnumerable<object> GetObjectsInTree(object graph)
        {
            var root = BuildReferenceTree(graph);

            var comparerNode = new ReferenceEqualsComparer<ReferenceNode>();
            var comparerObj = new ReferenceEqualsComparer<object>();

            var visitedNodes = new HashSet<ReferenceNode>(comparerNode);
            //var queue = new Queue<ReferenceNode>();
            //queue.Enqueue(root);
            return  GetObjectsInTree(root, visitedNodes);
        }

        private static IEnumerable<object> GetObjectsInTree(ReferenceNode node, HashSet<ReferenceNode> visitedNodes)
        {
            //don't visit the same node twice.
            if (visitedNodes.Contains(node))
            {
                yield break;
            }
            visitedNodes.Add(node);

            yield return node.Object;
            
            foreach (var to in node.Links.Select(l=>l.To))
            {
                foreach (var obj in GetObjectsInTree(to,visitedNodes))
                {
                    yield return obj;
                }
            }
            
        }

        private static ReferenceNode BuildReferenceTree(object graph)
        {
            var queue = new Queue<ReferenceNode>();
            var rootNode = new ReferenceNode(graph);
            var referenceMapping = new Dictionary<object, ReferenceNode>(new ReferenceEqualsComparer<object>());

            queue.Enqueue(rootNode); referenceMapping.Add(graph, rootNode);

            while (queue.Count > 0)
            {
                var activeNode = queue.Dequeue();

                var values = new List<Utils.Tuple<object, string>>();
                
                foreach (var propertyInfo in GetAllProperties(activeNode.Object))
                {
                    object value = null;
                    try
                    {
                        if (propertyInfo.GetIndexParameters().Length == 0)
                        {
                            value = propertyInfo.GetValue(activeNode.Object, null);
                        }
                    }
                    catch (Exception)
                    {
                    }

                    if (value == null || value.GetType().IsValueType)
                    {
                        continue;
                    }
                    values.Add(new Utils.Tuple<object, string>(value, propertyInfo.Name));
                }

                if (activeNode.Object is IEnumerable)
                {
                    var enumerable = activeNode.Object as IEnumerable;

                    foreach (var subobject in enumerable)
                    {
                        if (subobject == null || subobject.GetType().IsValueType)
                        {
                            break;
                        }
                        values.Add(new Utils.Tuple<object, string>(subobject, "[?]"));
                    }
                }

                foreach (var valueTuple in values)
                {
                    var value = valueTuple.First;
                    var valueAsCollection = value as ICollection;
                    var valueAsGeometry = value as IGeometry;
                    if (value == null //skip null values
                        || (valueAsCollection != null && valueAsCollection.Count == 0) //skip empty collections
                        || valueAsGeometry != null) //skip geometry && datetime
                    {
                        continue;
                    }

                    ReferenceNode referenceNode = null;
                    bool addPath = false;
                    
                    if (referenceMapping.ContainsKey(value))
                    {
                        referenceNode = referenceMapping[value];
                    }
                    else
                    {
                        referenceNode = new ReferenceNode(value);
                        referenceMapping.Add(value, referenceNode);
                        queue.Enqueue(referenceNode);
                        addPath = true;
                    }

                    var link = new ReferenceLink(activeNode, referenceNode, valueTuple.Second);
                    activeNode.Links.Add(link);

                    if (addPath)
                    {
                        var path = new List<ReferenceLink>(activeNode.Path);
                        path.Add(link);
                        referenceNode.Path = path;
                    }
                }
            }
            return rootNode;
        }

        /// <summary>
        /// UNTESTED!! Use at own risk! Should be used for debugging purposes only.
        /// </summary>
        /// <param name="target"></param>
        /// <param name="graph"></param>
        /// <returns></returns>
        public static List<string> SearchObjectInObjectGraph(object target, object graph)
        {
            var list = new List<string>();
            
            var root = BuildReferenceTree(graph);
            
            var comparerNode = new ReferenceEqualsComparer<ReferenceNode>();
            var comparerObj = new ReferenceEqualsComparer<object>();

            var visitedNodes = new Dictionary<ReferenceNode, object>(comparerNode);
            var queue = new Queue<ReferenceNode>();
            queue.Enqueue(root);
            
            var uniqueFrom = new List<object>();
            
            while (queue.Count > 0)
            {
                var activeNode = queue.Dequeue();

                foreach (var link in activeNode.Links)
                {
                    if (!visitedNodes.ContainsKey(link.To))
                    {
                        queue.Enqueue(link.To);
                        visitedNodes.Add(link.To, null);
                    }

                    if (ReferenceEquals(link.To.Object, target))
                    {
                        if (!uniqueFrom.Contains(link.From.Object, comparerObj))
                        {
                            uniqueFrom.Add(link.From.Object);
                            list.Add(link.ToPathString());
                        }
                    }
                }
            }
            return list;
        }

        public static void AssertStringRepresentationOfGraphIsEqual(object network, object clone)
        {
            var objectsInReal = GetObjectsInTree(network).ToList();
            var objectsInClone = GetObjectsInTree(clone).ToList();
            
            //Assert.AreEqual(objectsInReal.Count(), objectsInClone.Count());
            for (int i = 0; i < objectsInReal.Count;i++ )
            {
                var expected = objectsInReal[i].ToString();
                var actual = objectsInClone[i].ToString();
                if (expected != actual)
                {
                    Assert.Fail(String.Format("Unexpected object:\n {0}\n{1}\n", expected, actual));
                }
            }
        }

        private static IList<int> visitedObjects = new List<int>();

        /// <summary>
        /// NOTE: Debug only. Does _not_ work for PostSharp events!!!
        /// </summary>
        /// <param name="target"></param>
        /// <param name="subscriptions"></param>
        /// <param name="depth"></param>
        /// <returns></returns>
        public static int FindEventSubscriptions(object target, IList<string> subscriptions, int depth=4)
        {
            visitedObjects.Clear();
            return FindEventSubscriptionsCore("", target, subscriptions, depth);
        }

        private static int FindEventSubscriptionsCore(string path, object target, IList<string> subscriptions, int depth)
        {
            var hashCode = RuntimeHelpers.GetHashCode(target);
            if (visitedObjects.Contains(hashCode))
            {
                return 0;
            }
            visitedObjects.Add(hashCode);

            var allEvents = GetAllEventNames(target);

            int count = 0;

            foreach(var eventName in allEvents)
            {
                var invokeList = GetEventSubscribers(target, eventName);
                var invokeListLength = invokeList.Length;
                count += invokeListLength;
                if (invokeListLength > 0)
                {
                    var subscrib = path + "." + eventName + (invokeListLength > 1 ? " (x" + invokeListLength + ")" : "");
                    subscriptions.Add(subscrib);
                }
            }

            if (depth == 0)
                return count;

            foreach (var propertyInfo in GetAllProperties(target))
            {
                try
                {
                    if (propertyInfo.GetIndexParameters().Length == 0)
                    {
                        var value = propertyInfo.GetValue(target, null);

                        var values = new List<object>();

                        if (value is IEnumerable)
                        {
                            count += FindEventSubscriptionsCore(path + "." + propertyInfo.Name, value, subscriptions, 0); //depth 0, only search direct events
                            var enumerable = value as IEnumerable;

                            values.AddRange(enumerable.OfType<object>());
                        }
                        else
                        {
                            values.Add(value);
                        }

                        foreach (var item in values)
                        {
                            if (item != null && !(item is IGeometry))
                            {
                                var newPath = path + "." + propertyInfo.Name;
                                if (values.Count > 1)
                                {
                                    newPath += "[]";
                                }

                                count += FindEventSubscriptionsCore(newPath, item, subscriptions, depth - 1);
                            }
                        }
                    }
                }
                catch (Exception)
                {
                }
            }

            return count;
        }

        private static IEnumerable<string> GetAllEventNames(object target, bool includeHidden=true)
        {
            var events = target.GetType().GetEvents().Select(e => e.Name).ToList();

            if (includeHidden) //not working yet
            {
                if (target is INotifyPropertyChange)
                {
                    events.Add("PropertyChanging");
                    events.Add("PropertyChanged");
                }
                if (target is INotifyCollectionChange)
                {
                    events.Add("CollectionChanging");
                    events.Add("CollectionChanged");
                }
            }

            return events.Distinct();
        }

        //copyright bob powell, http://www.bobpowell.net/eventsubscribers.htm
        private static Delegate[] GetEventSubscribers(object target, string eventName)
        {
            var t = target.GetType();
            
            do
            {
                var fields = t.GetFields(BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic);
                
                foreach (FieldInfo fi in fields)
                {
                    if (fi.Name == eventName)
                    {
                        var d = fi.GetValue(target) as Delegate;
                        if (d != null)
                        {
                            return d.GetInvocationList();
                        }

                    }
                }
                t = t.BaseType;
            } 
            while (t != null);

            return new Delegate[] { };
        }

        private static IEnumerable<PropertyInfo> GetAllProperties(object target)
        {
            var objectType = target.GetType();
            var publicProperties = objectType.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy);
            var nonpublicProperties = objectType.GetProperties(BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.FlattenHierarchy);
            return publicProperties.Concat(nonpublicProperties);
        }
    }
}
