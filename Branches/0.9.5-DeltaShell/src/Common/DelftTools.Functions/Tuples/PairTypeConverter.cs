using System;
using System.ComponentModel;
using System.Globalization;
using DelftTools.Utils;
using TypeConverter = System.ComponentModel.TypeConverter;

namespace DelftTools.Functions.Tuples
{
    public class PairTypeConverter<T1, T2>: TypeConverter where T2 : IComparable<T2> where T1 : IComparable<T1>
    {
        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
        {
            return (sourceType == typeof(string));
        }

        public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
        {
            if(destinationType == typeof(string))
            {
                return true;
            }

            return false;
        }

        public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
        {
            if(destinationType == typeof(string))
            {
                return value.ToString();
            }

            throw new InvalidOperationException(String.Format("Can't convert from Pair<{0}, {1}> to type: {2}", typeof(T1), typeof(T2), destinationType));
        }

        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
        {
            var item =  (string)value;

            var pair = new Pair<T1, T2>();

            item = item.Replace("(", "").Replace(")","");

            var items = item.Split(',');
            pair.First = (T1)Convert.ChangeType(items[0], typeof (T1));
            pair.Second = (T2)Convert.ChangeType(items[1], typeof(T2));

            return pair;
        }
    }
}