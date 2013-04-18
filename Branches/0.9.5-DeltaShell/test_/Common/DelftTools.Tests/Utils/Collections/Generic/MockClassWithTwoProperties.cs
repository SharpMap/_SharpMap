using System.ComponentModel;

namespace DelftTools.Tests.Utils.Collections.Generic
{
    public class MockClassWithTwoProperties : INotifyPropertyChanged
    {
        private int intField;
        private string stringField;

        public int IntField
        {
            get { return intField; }
            set { intField = value; }
        }

        public string StringProperty
        {
            get { return stringField; }
            set
            {
                stringField = value;
                if(PropertyChanged != null)
                {
                    PropertyChanged(this, new PropertyChangedEventArgs("StringProperty"));
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}