using System;

namespace DelftTools.Functions
{
    public abstract class TypeConverterBase<T>:ITypeConverter
    {
        public virtual long Id { get; set; }
        object[] ITypeConverter.ConvertToStore(object source)
        {
            if (!typeof(T).IsAssignableFrom(source.GetType()))
                throw new InvalidOperationException();
            return ConvertToStore((T) source);
        }

        object ITypeConverter.ConvertFromStore(object o)
        {
            return ConvertFromStore(o);
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
        
        public abstract T ConvertFromStore(object source);
        public abstract object[] ConvertToStore(T source);
    }

    public class TypeConverterException : Exception
    {
    }
}