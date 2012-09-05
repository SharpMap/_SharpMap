#region

using System;
using DelftTools.Utils.Aop.NotifyPropertyChanged;

#endregion

namespace DelftTools.Units
{
    [Serializable]
    [NotifyPropertyChanged]
    public class Parameter : IMeasurable, IEquatable<Parameter>, IComparable<Parameter>, IComparable, ICloneable
    {
        protected object defaultValue;
        protected long id;
        private string name;
        
        private object value;
        private Type valueType;

        public Parameter()
        {
            Unit = new Unit();
            Enabled = true;
        }

        public virtual string Description { get; set; }

        public virtual object Value
        {
            get { return value; }
            set { this.value = value; }
        }

        // HACK: workaround, migrate to Attributes
        public virtual bool Enabled { get; set; }

        #region ICloneable Members

        

        public virtual object Clone()
        {
            //get a parameter of correct type (can be subclassed)
            Parameter clone = (Parameter)Activator.CreateInstance(GetType());
            clone.DefaultValue = (defaultValue is ICloneable) ? ((ICloneable) defaultValue).Clone() : defaultValue;
            clone.ValueType = ValueType;
            clone.Value = (value is ICloneable) ? ((ICloneable) value).Clone() : value;
            clone.Unit= Unit;
            clone.Enabled = Enabled;
            clone.Description = (Description != null) ? Description.Clone() as string : "";
            clone.Name = (Name != null) ? Name.Clone() as string : "";
            return clone;
        }

        #endregion

        #region IComparable Members

        public virtual int CompareTo(object obj)
        {
            var parameter = obj as Parameter;

            if (parameter == null)
            {
                return -1;
            }

            return CompareTo(parameter);
        }

        #endregion

        #region IComparable<Parameter> Members

        public virtual int CompareTo(Parameter other)
        {
            if (Value is IComparable)
            {
                return ((IComparable) Value).CompareTo(other.Value);
            }
            throw new ArgumentException(string.Format("Parameter {0} is not comparable.", Value));
        }

        #endregion

        #region IEquatable<Parameter> Members

        public virtual bool Equals(Parameter parameter)
        {
            if (parameter == null) return false;
            if (!Equals(Unit, parameter.Unit)) return false;
            if (!Equals(Value, parameter.Value)) return false;
            if (!Equals(Name, parameter.Name)) return false;
            return true;
        }

        #endregion

        #region IMeasurable Members

        public virtual long Id
        {
            get { return id; }
            set { id = value; }
        }

        /// <summary>
        /// Gets default type of the parameter depending on type. For value types it is T.MinValue.
        /// </summary>
        public virtual object DefaultValue
        {
            get { return defaultValue; }
            set { defaultValue = value; }
        }
        
        public virtual IUnit Unit
        {
            get; set;
        }

        public virtual Type ValueType
        {
            get { return valueType; }
            set
            {
                valueType = value;
                
                DefaultValue = (ValueType != null && ValueType.IsValueType) ? Activator.CreateInstance(ValueType) : null;
            }
        }

        public virtual string Name
        {
            get { return name; }
            set { name = value; }
        }

        
        IMeasurable IMeasurable.Clone()
        {
            return (IMeasurable) Clone();
        }

        #endregion

        #region IMeasurable Members


        public virtual string DisplayName
        {
            get { return string.Format("{0} [{1}]", Name, ((Unit != null) ? Unit.Symbol : "-")); }
        }
        
        #endregion
    }
}