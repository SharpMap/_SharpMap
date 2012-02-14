using System;
using System.ComponentModel;

namespace DelftTools.Functions.Binding
{
    public class FunctionBindingListPropertyDescriptor : PropertyDescriptor
    {
        public int index;
        private readonly string name;
        private readonly Type type;
        private readonly string displayName;
        private bool isReadOnly;

        public FunctionBindingListPropertyDescriptor(string name, string displayName, Type type, int index)
            : this(name, displayName, type, index, false)
        {
        }

        public FunctionBindingListPropertyDescriptor(string name, string displayName, Type type, int index, bool isReadOnly)
            : base(name, null)
        {
            this.name = name;
            this.type = type;
            this.displayName = displayName;
            this.index = index;
            this.isReadOnly = isReadOnly;
        }

        public string Name1
        {
            get { return name; }
        }

        public override string DisplayName
        {
            get { return displayName; }
        }

        public override Type ComponentType
        {
            get { return typeof(FunctionBindingListRow); }
        }

        public override bool IsReadOnly
        {
            get { return isReadOnly; }
        }

        public override Type PropertyType
        {
            get { return type; }
        }

        public override object GetValue(object component)
        {
            return ((FunctionBindingListRow)component)[index];
        }

        public override void SetValue(object component, object value)
        {
            ((FunctionBindingListRow)component)[index] = value;
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