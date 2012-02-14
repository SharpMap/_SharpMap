using System.Collections;
using DelftTools.Utils.Collections;

namespace DelftTools.Utils.Aop.NotifyCollectionChange
{
    public interface IFireCollectionChange
    {
        void OnCollectionChanged(object sender, NotifyCollectionChangingEventArgs e);
        void OnCollectionChanging(object sender, NotifyCollectionChangingEventArgs e);

        void Subscribe(INotifyCollectionChange change);
        void Unsubscribe(INotifyCollectionChange observed);

        IList ObservedObjects { get; }
        IList ObserversObjects { get; }
    }
}