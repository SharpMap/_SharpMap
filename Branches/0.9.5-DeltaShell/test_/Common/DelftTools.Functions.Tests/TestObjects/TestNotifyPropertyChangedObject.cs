using System;
using System.ComponentModel;
using log4net;

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


        public int CompareTo(object obj)
        {

            return Value.CompareTo(((TestNotifyPropertyChangedObject) obj).Value);
        }

        
    }
}