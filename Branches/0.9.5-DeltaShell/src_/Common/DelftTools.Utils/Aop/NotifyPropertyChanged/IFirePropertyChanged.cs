using System.Collections;
using System.ComponentModel;

namespace DelftTools.Utils.Aop.NotifyPropertyChanged
{
    public interface IFirePropertyChanged
    {
        void OnPropertyChanged(string propertyName);
        void OnPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e);
        void Unsubscribe(INotifyPropertyChanged changed);
        void Subscribe(INotifyPropertyChanged changed);
        IList ObservedObjects { get; }
        IList ObserversObjects { get; }

        /// <summary>
        /// When more aspects intercept the setter this value will be false if the current setter is not the last 
        /// </summary>
        bool IsLastPropertyNotifier { get; }
    }
}