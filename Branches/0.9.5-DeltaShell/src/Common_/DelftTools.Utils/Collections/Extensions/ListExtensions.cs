using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DelftTools.Utils.Collections.Extensions
{
    /// <summary>
    /// Extension methods for IList 
    /// </summary>
    public static class ListExtensions
    {
        /// <summary>
        /// Adds a range of items to the list
        /// </summary>
        /// <typeparam name="T">The type of the elements in the list</typeparam>
        /// <param name="destination">The target list where item need to be added</param>
        /// <param name="collection">The items to be added</param>
        public static void AddRange<T>(this IList<T> destination, IEnumerable<T> collection)
        {
            if (destination == null)
                throw new ArgumentNullException("destination");

            if (collection == null)
                throw new ArgumentNullException("collection");

            foreach (T item in collection)
                destination.Add(item);
        }

        /// <summary>
        /// Adds a range of items to the list but leaving null items out
        /// </summary>
        /// <typeparam name="T">The type of the elements in the list</typeparam>
        /// <param name="destination">The target list where item need to be added</param>
        /// <param name="collection">The items to be added</param>
        public static void AddRangeLeavingNullElementsOut<T>(this IList<T> destination, IEnumerable<T> collection)
            where T : class
        {
            AddRangeConditionally(destination, collection, x => x != null);
        }

        /// <summary>
        /// Adds a range of items to the list conditionaly. ie. When the predicate for an item evaluates to true it will be added
        /// </summary>
        /// <typeparam name="T">The type of the elements in the list</typeparam>
        /// <param name="destination">The target list where item need to be added</param>
        /// <param name="collection">The items to be added</param>
        /// <param name="predicate">The items predicate or requirement that needs to be true before the item can be added</param>
        public static void AddRangeConditionally<T>(this IList<T> destination, IEnumerable<T> collection,
                                                    Func<T, bool> predicate)
        {
            if (destination == null)
                throw new ArgumentNullException("destination");

            if (collection == null)
                throw new ArgumentNullException("collection");

            if (predicate == null)
                throw new ArgumentNullException("predicate");

            foreach (T item in collection)
                if (predicate(item))
                    destination.Add(item);
        }

    }
}
