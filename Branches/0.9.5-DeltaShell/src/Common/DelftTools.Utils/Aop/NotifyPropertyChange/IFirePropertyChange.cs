using System.Collections;
using System.ComponentModel;

namespace DelftTools.Utils.Aop.NotifyPropertyChange
{
    public interface IFirePropertyChange 
    {
        void FirePropertyChanging(object sender, PropertyChangingEventArgs e);
        void FirePropertyChanged(object sender, PropertyChangedEventArgs e);
        
        void Unsubscribe(INotifyPropertyChange change);
        void Subscribe(INotifyPropertyChange changed);
        
        IList ObservedObjects { get; }
        IList ObserversObjects { get; }

        /// <summary>
        /// When more aspects intercept the setter this value will be false if the current setter is not the last 
        /// </summary>
        bool IsLastPropertyNotifier { get; }
    }
}