using System;
using DelftTools.Utils;
using DelftTools.Utils.Aop;

namespace DelftTools.Units.Generics
{
    /// <summary>
    /// Represents any parameter containing single value of some type.
    /// <para/>
    /// "default routhness" = 0.01<para/>
    /// "calculation start time" = 01.01.2005 10:00:00
    /// </summary>
    /// <typeparam name="T">Type of the value class.</typeparam>
    [Serializable]
    //[NotifyPropertyChanged]
    public class Parameter<T> : Parameter, IEquatable<Parameter<T>>, IComparable<Parameter<T>>, IComparable
        where T : IComparable
    {
        private const string DefaultName = "new parameter";
        

        public Parameter() : this(DefaultName)
        {
        }

        public Parameter(string name) : this(name, null)
        {
        }

        public Parameter(string name, IUnit unit)
        {
            DefaultValue = CreateDefaultValue();
            Unit = unit;
            Value = DefaultValue;
            Name = name;
        }
        
        public virtual T Value
        {
            get { return (T) base.Value; }
            set { base.Value = value; }
        }

        /// <summary>
        /// Gets default type of the parameter depending on type. For value types it is T.MinValue.
        /// TODO move to base class and use proper default values 0 &! double.MinValue
        /// </summary>
        public new virtual T DefaultValue
        {
            get { return (T) base.DefaultValue; }
            set { base.DefaultValue = value; }
        }

        private T CreateDefaultValue()
        {
            if (typeof(T) == typeof(DateTime))
            {
                return (T)((object)DateTime.MinValue);
            }
            if (typeof(T) == typeof(TimeSpan))
            {
                return (T)((object)TimeSpan.MinValue);
            }
            if (typeof(T) == typeof(String))
            {
                return (T)((object)String.Empty);
            }
            
            if (typeof(T).IsClass || typeof(T) == typeof(Boolean) || typeof(T) == typeof(Double) || typeof(T) == typeof(Single) || typeof(T) == typeof(int))
            {
                return default(T);
            }

            throw new NotSupportedException("Type is not supported yet: " + typeof(T));
        }
        
        /// <summary>
        /// Compares the current instance with another object of the same type.
        /// </summary>
        /// <returns>
        /// A 32-bit signed integer that indicates the relative order of the objects being compared. The return value has these meanings: Value Meaning Less than zero This instance is less than <paramref name="obj" />. Zero This instance is equal to <paramref name="obj" />. Greater than zero This instance is greater than <paramref name="obj" />. 
        /// </returns>
        /// <param name="obj">An object to compare with this instance. </param>
        /// <exception cref="T:System.ArgumentException"><paramref name="obj" /> is not the same type as this instance. </exception><filterpriority>2</filterpriority>
        public virtual int CompareTo(object obj)
        {
            var parameter = obj as Parameter<T>;

            if (parameter == null)
            {
                return -1;
            }

            return CompareTo(parameter);
        }

        /// <summary>
        /// Compares the current object with another object of the same type.
        /// </summary>
        /// <returns>
        /// A 32-bit signed integer that indicates the relative order of the objects being compared. The return value has the following meanings: Value Meaning Less than zero This object is less than the <paramref name="other" /> parameter.Zero This object is equal to <paramref name="other" />. Greater than zero This object is greater than <paramref name="other" />. 
        /// </returns>
        /// <param name="other">An object to compare with this object.</param>
        public virtual int CompareTo(Parameter<T> other)
        {
            return Value.CompareTo(other.Value);
        }
        
        public virtual bool Equals(Parameter<T> parameter)
        {
            if (parameter == null) return false;
            if (!Equals(Unit, parameter.Unit)) return false;
            if (!Equals(Value, parameter.Value)) return false;
            if (!Equals(Name, parameter.Name)) return false;
            return true;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(this, obj)) return true;
            return Equals(obj as Parameter<T>);
        }

        public override int GetHashCode()
        {
            int result = Unit != null ? Unit.GetHashCode() : 0;
            result = result + Name.GetHashCode();
            result = result + Value.GetHashCode();
            return result;
        }

        public override object Clone()
        {
            var parameterClone = (Parameter<T>)Activator.CreateInstance(GetType());

            parameterClone.ValueType = ValueType;
            parameterClone.Unit = (Unit != null) ? (IUnit)Unit.Clone() : null;
            parameterClone.Enabled = Enabled;
            parameterClone.Description = (Description != null) ? Description.Clone() as string : "";
            parameterClone.Name = (Name != null) ? Name.Clone() as string : "";
            parameterClone.DefaultValue = ObjectHelper.Clone<T>(DefaultValue);
            parameterClone.Value = ObjectHelper.Clone<T>(Value);
            parameterClone.MinValidValue = ObjectHelper.Clone<T>(MinValidValue);
            parameterClone.MaxValidValue = ObjectHelper.Clone<T>(MaxValidValue);
            return parameterClone;
        }

        public override Type ValueType
        {
            get { return typeof (T); }
            set { /* TODO: throw exception?? */ }
        }
    }
}