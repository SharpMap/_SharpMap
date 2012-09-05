using System;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;

// License: The Code Project Open License (CPOL) 1.02
// Original author is: http://www.codeproject.com/KB/buttons/CButton.aspx?msg=3060090

namespace SharpMap.Styles.Shapes
{
    internal class CornerConverter : ExpandableObjectConverter
    {
        // Methods
        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
        {
            return ((sourceType == typeof(string)) || base.CanConvertFrom(context, sourceType));
        }

        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
        {
            throw new NotImplementedException();
/*
            if (value is string)
            {
                try
                {
                    string s = Convert.ToString(value);
                    string[] cornerParts = new string[5];
                    cornerParts = s.Split(',');
                    if (Information.IsNothing(cornerParts))
                    {
                        return base.ConvertFrom(context, culture, RuntimeHelpers.GetObjectValue(value));
                    }
                    if (Information.IsNothing(cornerParts[0]))
                    {
                        cornerParts[0] = Convert.ToString(0);
                    }
                    if (Information.IsNothing(cornerParts[1]))
                    {
                        cornerParts[1] = Convert.ToString(0);
                    }
                    if (Information.IsNothing(cornerParts[2]))
                    {
                        cornerParts[2] = Convert.ToString(0);
                    }
                    if (Information.IsNothing(cornerParts[3]))
                    {
                        cornerParts[3] = Convert.ToString(0);
                    }
                    return new CornersProperty(Convert.ToInt16(cornerParts[0]), Convert.ToInt16(cornerParts[1]), Convert.ToInt16(cornerParts[2]), Convert.ToInt16(cornerParts[3]));
                }
                catch (Exception exception1)
                {
                    ProjectData.SetProjectError(exception1);
                    Exception ex = exception1;
                    throw new ArgumentException(Convert.ToString(Operators.AddObject(Operators.AddObject("Can not convert '", value), "' to type Corners")));
                }
            }
            return new CornersProperty();
*/
        }

        public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
        {
            if ((destinationType == typeof(string)) && (value is CornersProperty))
            {
                CornersProperty _Corners = (CornersProperty)value;
                return string.Format("{0},{1},{2},{3}", new object[] { _Corners.LowerLeft, _Corners.LowerRight, _Corners.UpperLeft, _Corners.UpperRight });
            }
            return base.ConvertTo(context, culture, RuntimeHelpers.GetObjectValue(value), destinationType);
        }
    }
}