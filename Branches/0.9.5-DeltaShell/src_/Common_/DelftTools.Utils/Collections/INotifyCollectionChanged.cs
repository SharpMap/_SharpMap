

namespace DelftTools.Utils.Collections
{
    /// <summary>
    /// Delegate belonging to INotifyCollectionChanged Implementation
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    public delegate void NotifyCollectionChangedEventHandler(object sender, NotifyCollectionChangedEventArgs e);

    /// <summary>
    /// Mimicks interface that is available in .net 3.0 
    /// </summary>
    public interface INotifyCollectionChanged
    {
        event NotifyCollectionChangedEventHandler CollectionChanged;
        event NotifyCollectionChangedEventHandler CollectionChanging;
    }
}