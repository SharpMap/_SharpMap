using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;

namespace DelftTools.Utils.Binding
{
    
    /// <summary>
    /// Class to get a list Key,Value of an enum where the keys is the enum value and the value the description
    /// Copied from:
    /// http://geekswithblogs.net/sdorman/archive/2007/08/02/Data-Binding-an-Enum-with-Descriptions.aspx
    /// </summary>
    /// <example>
    /// ComboBox combo = new ComboBox();
    /// combo.DataSource = EnumBindingHelper.ToList(typeof(SimpleEnum));
    /// combo.DisplayMember = "Value";
    /// combo.ValueMember = "Key";
    /// </example>
    public class EnumBindingHelper
    {
        /// <summary>
        /// Gets the <see cref="DescriptionAttribute"/> of an <see cref="Enum"/> type value.
        /// </summary>
        /// <param name="value">The <see cref="Enum"/> type value.</param>
        /// <returns>A string containing the text of the <see cref="DescriptionAttribute"/>.</returns>
        private static string GetDescription(Enum value)
        {
            if (value == null)
            {
                throw new ArgumentNullException("value");
            }

            string description = value.ToString();
            FieldInfo fieldInfo = value.GetType().GetField(description);
            var attributes = (DescriptionAttribute[])fieldInfo.GetCustomAttributes(typeof(DescriptionAttribute), false);

            if (attributes.Length > 0)
            {
                description = attributes[0].Description;
            }
            return description;
        }

        /// <summary>
        /// Converts the <see cref="Enum"/> type to an <see cref="IList{T}"/> compatible object.
        /// </summary>
        /// <param name="type">The <see cref="Enum"/> type.</param>
        /// <returns>An <see cref="IList{T}"/> containing the enumerated type value and description.</returns>
        public static IList<KeyValuePair<TEnumType,string>> ToList<TEnumType>() where TEnumType:struct
        {
            Type type = typeof(TEnumType);
            if (!type.IsEnum)
            {
                throw new InvalidOperationException("Type is not an enum type.");
            }

            var list = new List<KeyValuePair<TEnumType,string>>();
            /*Array enumValues = Enum.GetValues(type);

            foreach (Enum value in enumValues)
            {
                list.Add(new KeyValuePair<TEnumType, string>((TEnumType)value, GetDescription(value)));
            }*/
            Array enumValArray = Enum.GetValues(type);

            //List<T> enumValList = new List<T>(enumValArray.Length);

            foreach (Enum val in enumValArray)
            {
                var description = GetDescription(val);
                list.Add(new KeyValuePair<TEnumType, string>((TEnumType)Enum.Parse(type, val.ToString()),description));
            }
            return list;
        }
    }
}