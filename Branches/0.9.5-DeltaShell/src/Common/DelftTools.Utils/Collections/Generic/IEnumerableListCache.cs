namespace DelftTools.Utils.Collections.Generic
{
    public interface IEnumerableListCache
    {
        INotifyCollectionChange CollectionChangeSource { get; set; }
    }
}