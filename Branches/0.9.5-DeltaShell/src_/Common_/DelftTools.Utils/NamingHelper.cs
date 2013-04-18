using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace DelftTools.Utils
{
    ///<summary>
    ///</summary>
    public static class NamingHelper
    {
        /// <summary>
        /// Extracts an unique name from the item collection
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="filter"></param>
        /// <param name="items"></param>
        /// <param name="t"></param>
        /// <returns></returns>
        public static string GetUniqueName<T>(string filter, IEnumerable<T> items, Type t) where T : INameable
        {
            if (null != filter)
            {
                if (filter.Length == 0)
                {
                    // to do test if filter has format code
                    throw new ArgumentException("Can not create an unique name when filter is empty.");
                }

                if (!filter.Contains("{0}"))
                {
                    throw new ArgumentException("Invalid filter");
                }
            }
            else
            {
                filter = t.Name + "{0}";
            }

            var namesList = items.Select(item => item.Name).Distinct().ToList();

            String unique;
            int id = 1;

            do
            {
                unique = String.Format(filter, id++);
            } while (namesList.Contains(unique));

            return unique;
        }

    }
}