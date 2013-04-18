using System.Collections;
using System.Collections.Generic;

namespace DelftTools.Utils
{
    /// <summary>
    /// Provides functionality of enumerating throught objects.
    /// An object should return all the objects it is composed with
    /// eg.
    /// Folder
    ///     --Models
    ///     --Folders
    ///     --DataItems
    ///
    /// Or Map
    /// Map
    ///     --Layers
    /// Factor the first part into an other (base)class between folder/model/dataitem
    /// 
    /// NOTE: this interface can be better implemented a-la Google Guice, e.g.: [Dependency], to avoid repeatin the same code 100x times
    /// </summary>
    public interface IItemContainer
    {
        //IEnumerable<object> GetAllItemsRecursive();

        IEnumerable<object> GetDirectChildren();
    }

    public static class ItemContainerExtensions
    {
        public static IEnumerable<object> GetAllItemsRecursive(this IItemContainer rootContainer)
        {
            yield return rootContainer;

            var queue = new Queue<object>();
            queue.Enqueue(rootContainer);

            while(queue.Count > 0)
            {
                var obj = queue.Dequeue();
                if (obj is IItemContainer)
                {
                    var container = obj as IItemContainer;
                    foreach(var item in container.GetDirectChildren())
                    {
                        yield return item;
                        queue.Enqueue(item);
                    }
                }
            }
        }
    }
}