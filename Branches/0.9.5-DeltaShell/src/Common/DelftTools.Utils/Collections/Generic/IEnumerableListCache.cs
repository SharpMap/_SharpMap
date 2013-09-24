namespace DelftTools.Utils.Collections.Generic
{
    public interface IEnumerableListCache
    {
        INotifyCollectionChange CollectionChangeSource { get; set; }
        INotifyPropertyChange PropertyChangeSource { get; set; }
    }
}