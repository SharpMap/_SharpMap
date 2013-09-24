using System;
using System.ComponentModel;

namespace DelftTools.Functions.Tests.TestObjects
{
    public class TestNotifyPropertyChangedObject:INotifyPropertyChanged,INotifyPropertyChanging,IComparable
    {
        //handy for identification
        public string Name { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;
        public event PropertyChangingEventHandler PropertyChanging;

        public void FireChanged()
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs("kees"));    
            }
        }
        public void FireChanging()
        {
            if (PropertyChanging != null)
            {
                PropertyChanging(this, new PropertyChangingEventArgs("kees"));
            }
        }

        public int Value { get; set; }

        public int GetNumPropertyChangedSubscriptions()
        {
            if (PropertyChanged != null)
            {
                var multiPropertyChanged = (PropertyChanged as MulticastDelegate);
                if (multiPropertyChanged != null)
                {
                    return multiPropertyChanged.GetInvocationList().Length;
                }
                return 1;
            }
            return 0;
        }


        public int CompareTo(object obj)
        {

            return Value.CompareTo(((TestNotifyPropertyChangedObject) obj).Value);
        }

        
    }
}