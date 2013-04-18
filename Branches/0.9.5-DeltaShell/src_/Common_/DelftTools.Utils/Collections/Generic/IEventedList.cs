using System.Collections.Generic;
using System.ComponentModel;

namespace DelftTools.Utils.Collections.Generic
{
    /// <summary>
    /// This interface defines a list which observes all changes in the underlying items and
    /// subscribes to their INotifyPropertyChanged and INotifyCollectionChanged if they implement them.
    /// </summary>
    /// <typeparam name="T">The datatype that is being added/removed</typeparam>
    public interface IEventedList<T> : IList<T>, INotifyCollectionChanged
    {
        void AddRange(IEnumerable<T> enumerable);
    }
}