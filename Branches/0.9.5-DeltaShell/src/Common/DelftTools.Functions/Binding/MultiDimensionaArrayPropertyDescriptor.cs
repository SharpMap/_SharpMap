using System;
using System.ComponentModel;
using System.Diagnostics;

namespace DelftTools.Functions.Binding
{
    /// <summary>
    /// Property descriptor for data
    /// </summary>
    public class MultiDimensionaArrayPropertyDescriptor : PropertyDescriptor
    {
        private readonly int index;
        private readonly string name;
        private readonly Type type;

        public MultiDimensionaArrayPropertyDescriptor(string name, Type type, int index) : base(name, null)
        {
            this.name = name;
            this.type = type;
            this.index = index;
        }

        public override string DisplayName
        {
            get { return name; }
        }

        public override Type ComponentType
        {
            get { return typeof (MultiDimensionalArrayBindingListRow); }
        }

        public override bool IsReadOnly
        {
            get { return false; }
        }

        public override Type PropertyType
        {
            get { return type; }
        }

        public override object GetValue(object component)
        {
            return ((MultiDimensionalArrayBindingListRow) component).GetColumn(index);
        }

        public override void SetValue(object component, object value)
        {
            try
            {
                ((MultiDimensionalArrayBindingListRow) component).SetColumnValue(index, value);
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
                Debug.Assert(false);
            }
        }

        public override bool CanResetValue(object component)
        {
            return false;
        }

        public override void ResetValue(object component)
        {
        }

        public override bool ShouldSerializeValue(object component)
        {
            return false;
        }
    }
}