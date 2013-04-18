using System;
using System.ComponentModel;
using DelftTools.Utils;

namespace DelftTools.Tests.Core.Mocks
{
    /// <summary>
    /// added INameable to test for name updates after linking / unlinking dataitem
    /// </summary>
    public class CloneableClassWithThreeProperties : INotifyPropertyChanged, INameable, IDeepCloneable, ICloneable
    {

        private int intField;
        private string stringField;
        private string name;

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
                if (PropertyChanged != null)
                {
                    PropertyChanged(this, new PropertyChangedEventArgs("StringProperty"));
                }
            }
        }

        public string Name
        {
            get { return name; }
            set
            {
                name = value;
                if (PropertyChanged != null)
                {
                    PropertyChanged(this, new PropertyChangedEventArgs("Name"));
                }
            }
        }


        public event PropertyChangedEventHandler PropertyChanged;
        public object DeepClone()
        {
            return new CloneableClassWithThreeProperties
            {
                IntField = IntField,
                Name = Name,
                StringProperty = StringProperty
            };
        }

        public object Clone()
        {
            return DeepClone();
        }
    }
}