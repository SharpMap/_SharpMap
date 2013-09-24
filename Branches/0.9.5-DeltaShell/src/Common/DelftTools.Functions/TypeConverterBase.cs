using System;
using DelftTools.Utils.Data;

namespace DelftTools.Functions
{
    public abstract class TypeConverterBase<T> : Unique<long>, ITypeConverter
    {
        public abstract string[] VariableUnits { get; }

        object[] ITypeConverter.ConvertToStore(object source)
        {
            if (!(source is T))
                throw new InvalidOperationException();
            return ConvertToStore((T) source);
        }

        object ITypeConverter.ConvertFromStore(object o)
        {
            return ConvertFromStore(o);
        }

        protected static char[] ConvertToCharArray(string str)
        {
            //30 is also assumed in the NetCdfFunctionStore!!
            return str.PadRight(30).ToCharArray();
        }

        public virtual Type ConvertedType
        {
            get { return typeof(T); }
        }

        //type specific. Overriden by implementation
        public abstract Type[] StoreTypes { get; }

        /// <summary>
        /// Gets the variable names for the stored types
        /// </summary>
        /// <remarks>
        /// The names are stored in the same order as the types. 
        /// </remarks>
        public abstract string[] VariableNames { get; }

        public abstract string[] VariableStandardNames { get; }

        public abstract T ConvertFromStore(object source);
        public abstract object[] ConvertToStore(T source);
    }
}