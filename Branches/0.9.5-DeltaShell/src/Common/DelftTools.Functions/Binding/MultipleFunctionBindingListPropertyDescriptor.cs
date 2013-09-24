using System;
using System.ComponentModel;

namespace DelftTools.Functions.Binding
{
    class MultipleFunctionBindingListPropertyDescriptor : PropertyDescriptor
    {
        private readonly int index;
        private readonly Type type;
        private readonly string displayName;

        public MultipleFunctionBindingListPropertyDescriptor(string name, string displayName, Type type, int index)
            : base(name, null)
        {
            this.type = type;
            this.displayName = displayName;
            this.index = index;
        }

        public override string DisplayName
        {
            get { return displayName; }
        }

        public override Type ComponentType
        {
            get { return typeof(MultipleFunctionBindingListRow); }
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
            return ((MultipleFunctionBindingListRow)component).GetColumnValue(index);
        }

        public override void SetValue(object component, object value)
        {
            ((MultipleFunctionBindingListRow)component).SetColumnValue(index, value);
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