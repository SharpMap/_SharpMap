using System;
using System.ComponentModel;
using System.ComponentModel.Design.Serialization;
using System.Globalization;

namespace DelftTools.Utils
{
    /// <summary>
    /// convert the date and time to required format
    /// </summary>
    public class DeltaShellDateTimeConverter : System.ComponentModel.DateTimeConverter
    {
        public DeltaShellDateTimeConverter()
		{
		}

		public override bool CanConvertFrom (ITypeDescriptorContext context, Type sourceType)
		{
			if (sourceType == typeof (string)) 
				return true;
			return base.CanConvertFrom (context, sourceType);
		}

		public override bool CanConvertTo (ITypeDescriptorContext context, Type destinationType)
		{
			if (destinationType == typeof (InstanceDescriptor))
				return true;
			return base.CanConvertTo (context, destinationType);
		}

        /// <summary>
        /// converts datetime to format yyyy/MM/dd HH:mm:ss
        /// </summary>
        /// <param name="context"></param>
        /// <param name="culture"></param>
        /// <param name="value"></param>
        /// <param name="destinationType"></param>
        /// <returns></returns>
        public override object ConvertTo(System.ComponentModel.ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value, Type destinationType)
        {

            DateTime datetime = (DateTime)value;

            return datetime.ToString("yyyy/MM/dd HH:mm:ss", culture);

        }

        /// <summary>
        /// converts datetime from format yyyy/MM/dd HH:mm:ss
        /// </summary>
        /// <param name="context"></param>
        /// <param name="culture"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public override object ConvertFrom(System.ComponentModel.ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value)
        {
            if (value is string)
            {
                string DateString = (string)value;
                try
                {
                    if (culture == null)
                    {
                        return DateTime.Parse(DateString);
                    }
                    else
                    {
                        DateTimeFormatInfo info = (DateTimeFormatInfo)culture.GetFormat(typeof(DateTimeFormatInfo));
                        info.ShortDatePattern = "yyyy/MM/dd HH:mm:ss";
                        return DateTime.Parse(DateString, info);
                    }
                }
                catch
                {
                    throw new FormatException(DateString + " is not a valid DateTime value. The format should be yyyy/MM/dd HH:mm:ss");
                }
            }
            return base.ConvertFrom(context, culture, value);
            
        }
    }
}
