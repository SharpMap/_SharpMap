using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Text;
using System.Xml.Serialization;
using GisSharpBlog.NetTopologySuite.Index.Bintree;
using SharpMap.Rendering.Thematics;
using SharpMap.Styles;

namespace DelftShell.Plugins.SharpMapGis.HibernateMappings
{
    internal enum ThemeType
    {
        Custom,
        Categorial,
        Gradient,
        Quantity
    }

    /// <summary>
    /// Converter to serialize <see cref="SharpMap"/> <see cref="ITheme"/> objects into an XML (with CSS) string
    /// </summary>
    public class ThemeTypeConverter : TypeConverter
    {
        ///<summary>
        /// Singleton access to the <see cref="ITheme"/> type to string converter
        ///</summary>
        ///<returns></returns>
        public static StyleTypeConverter GetStyleConverter()
        {
            TypeConverter tc = TypeDescriptor.GetConverter(typeof (IStyle));
            if ((tc as StyleTypeConverter) == null)
            {
                DelftTools.Utils.TypeConverter.RegisterTypeConverter<IStyle, StyleTypeConverter>();
                tc = TypeDescriptor.GetConverter(typeof (IStyle));
            }
            return (StyleTypeConverter) tc;
        }

        ///<summary>
        /// Converts from a XML-like string to a <see cref="SharpMap"/> <see cref="ITheme"/> object
        ///</summary>
        ///<param name="context"></param>
        ///<param name="culture"></param>
        ///<param name="value"></param>
        ///<returns></returns>
        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
        {
            if (!(value is string))
            {
                // Use the base converter if the value was of an unsupported type
                return base.ConvertFrom(context, culture, value);
            }

            if ((string) value == string.Empty)
            {
                // If the xml was an empty string, return null (no Theme set)
                return null;
            }
            
            // Parse the string as XML via a strongly typed dataset
            var stringReader = new StringReader((string) value);
            var xmlSerializer = new XmlSerializer(typeof (theme));
            var sourceTheme = (theme) xmlSerializer.Deserialize(stringReader);
            
            ITheme targetTheme = null;
            TypeConverter styleTC = GetStyleConverter();

            switch (GetThemeType(sourceTheme))
            {
                case ThemeType.Custom:
                    targetTheme = GetCustomTheme(sourceTheme);
                    break;
                case ThemeType.Categorial:
                    targetTheme = GetCategorialTheme(sourceTheme);
                    break;
                case ThemeType.Gradient:
                    targetTheme = GetGradientTheme(sourceTheme);
                    break;
                case ThemeType.Quantity:
                    targetTheme = GetQuantityTheme(sourceTheme);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            return targetTheme;
        }

        private static CustomTheme GetCustomTheme(theme theme)
        {
            return new CustomTheme(null) { DefaultStyle = GetDefaultStyle(theme) };
        }

        private static GradientTheme GetGradientTheme(theme theme)
        {
            var themeGradient = (themeGradient)theme.Item;
            var minStyle = (IStyle)GetStyleConverter().ConvertFrom(themeGradient.minStyle);
            var maxStyle = (IStyle)GetStyleConverter().ConvertFrom(themeGradient.maxStyle);


            var gradTheme = new GradientTheme(themeGradient.columnName,
                                              themeGradient.minValue,
                                              themeGradient.maxValue,
                                              minStyle,
                                              maxStyle,
                                              // Color blend properties
                                              (themeGradient.fillColorBlends != null)
                                                  ? createColorBlendForTheme(themeGradient.fillColorBlends)
                                                  : null,
                                              (themeGradient.lineColorBlends != null)
                                                  ? createColorBlendForTheme(themeGradient.lineColorBlends)
                                                  : null,
                                              (themeGradient.textColorBlends != null)
                                                  ? createColorBlendForTheme(themeGradient.textColorBlends)
                                                  : null)
                                {
                                    NoDataValues = ConvertNoDataValues(themeGradient.noDataValues, themeGradient.noDataValueType)
                                };

            gradTheme.ThemeItems.Clear();

            foreach (themeItem gradThemeItem in themeGradient.gradientThemeItems)
            {
                var themeStyle = GetStyle(gradThemeItem);
                var gradientThemeItem = new GradientThemeItem(themeStyle, gradThemeItem.label, gradThemeItem.intervalMaxValue.ToString());
                
                gradTheme.ThemeItems.Add(gradientThemeItem);
            }

            return gradTheme;
        }

        /// <summary>
        /// Convert the nodatavalues from an array of double to an array of the type noDataValueType
        /// todo find a more generic method via reflection;
        /// </summary>
        /// <param name="noDataValues"></param>
        /// <param name="noDataValueType"></param>
        /// <returns></returns>
        private static IList ConvertNoDataValues(double[] noDataValues, string noDataValueType)
        {
            if (null == noDataValues)
            {
                return null;
            }
            if (0 == noDataValues.Length)
            {
                return null;
            }
            IList returnValues;
            if (noDataValueType == "System.Int32")
            {
                returnValues = new List<int>();
                for (int i = 0; i < noDataValues.Length; i++)
                {
                    returnValues.Add((int)noDataValues[i]);
                }
            }
            else if (noDataValueType == "System.Single")
            {
                returnValues = new List<float>();
                for (int i = 0; i < noDataValues.Length; i++)
                {
                    returnValues.Add((float)noDataValues[i]);
                }
            }
            else
            {
                returnValues = new List<double>(noDataValues);
            }
            return returnValues;
        }

        private static QuantityTheme GetQuantityTheme(theme theme)
        {
            var themeQuantity = (themeQuantity) theme.Item;

            var quanTheme = new QuantityTheme(themeQuantity.columnName, GetDefaultStyle(theme))
                                {
                                    NoDataValues = ConvertNoDataValues(themeQuantity.noDataValues, themeQuantity.noDataValueType)
                                };
                        
            foreach (themeItem quanThemeItem in themeQuantity.quantityThemeItems)
            {
                var themeStyle = GetStyle(quanThemeItem);
                var interval = new Interval(quanThemeItem.intervalMinValue,
                                            quanThemeItem.intervalMaxValue);

                var themeItem = new QuantityThemeItem(interval, themeStyle) {Label = quanThemeItem.label};
                quanTheme.ThemeItems.Add(themeItem);
            }
            
            return quanTheme;
        }

        private static CategorialTheme GetCategorialTheme(theme theme)
        {
            var themeCategory = (themeCategory)theme.Item;
            var defaultStyle = GetDefaultStyle(theme);
            
            var categorialTheme = new CategorialTheme(themeCategory.columnName, defaultStyle);

            foreach (themeItem catThemeItem in themeCategory.categoryThemeItems)
            {
                var categorialThemeItem = new CategorialThemeItem(catThemeItem.label, GetStyle(catThemeItem), null);
                categorialTheme.AddThemeItem(categorialThemeItem);
            }

            return categorialTheme;
        }

        /// <summary>
        /// Converts from a a <see cref="SharpMap"/> <see cref="ITheme"/> object to a XML-like string
        /// </summary>
        /// <param name="context"></param>
        /// <param name="culture"></param>
        /// <param name="value"></param>
        /// <param name="destinationType"></param>
        /// <returns></returns>
        public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
        {
            if (!(value is ITheme) || !destinationType.Equals(typeof (string)))
            {
                // Use the base converter if the target type is unsupported
                return base.ConvertTo(context, culture, value, destinationType);
            }

            // Get the ITheme to convert from
            var to = new theme();
            var from = (ITheme) value;
 
            switch (GetThemeType(from))
            {
                case ThemeType.Custom:
                    to.Item = GetThemeCustom((CustomTheme)from);
                    break;
                case ThemeType.Categorial:
                    to.Item = GetThemeCategorial((CategorialTheme)from);
                    break;
                case ThemeType.Gradient:
                    to.Item = GetThemeGradient((GradientTheme)from);
                    break;
                case ThemeType.Quantity:
                    to.Item = GetThemeQuantity((QuantityTheme) from);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
 
            // Return the theme classes as an xml data string
            var sb = new StringBuilder();
            var xml = new XmlSerializer(typeof (theme));
            xml.Serialize(new StringWriter(sb), to);
            
            return sb.ToString();
        }

        private static themeQuantity GetThemeQuantity(QuantityTheme theme)
        {
            // QuantityTheme properties
            var quanThemeItems = new List<themeItem>();

            foreach (QuantityThemeItem item in theme.ThemeItems)
            {
                // Add theme items (style and label) to the QuantityTheme
                // NOTE: The actual Category of this specific ThemeItem is always equal to the Label (no need to store it twice)
                // NOTE: Symbol isn't stored but generated during rebuilding in the QuantityThemeItem.AddStyle() method
                var quanThemeItem = new themeItem
                                        {
                                            label = item.Label,
                                            style = GetStyleConverter().ConvertToString(item.Style),
                                            intervalMinValue = item.Interval.Min,
                                            intervalMaxValue = item.Interval.Max
                                        };

                quanThemeItems.Add(quanThemeItem);
            }

            var themeQuantity = new themeQuantity
                                    {
                                        columnName = theme.AttributeName,
                                        defaultStyle = GetDefaultStyle(theme),
                                        noDataValues = GetNoDataValues(theme.NoDataValues),
                                        noDataValueType = GetNoDataValueType(theme.NoDataValues),
                                        quantityThemeItems = quanThemeItems.ToArray()
                                    };

            return themeQuantity;
        }

        private static themeGradient GetThemeGradient(GradientTheme theme)
        {
            // GradientTheme properties
            string minStyle = GetStyleConverter().ConvertToString(theme.MinStyle);
            string maxStyle = GetStyleConverter().ConvertToString(theme.MaxStyle);
            
            var gradThemeItems = new List<themeItem>();
            foreach (GradientThemeItem item in theme.ThemeItems)
            {
                // Add theme items (style and label) to the QuantityTheme
                // NOTE: Symbol isn't stored but generated during rebuilding in the QuantityThemeItem.AddStyle() method
                var gradThemeItem = new themeItem
                                        {
                                            label = item.Label,
                                            style = GetStyleConverter().ConvertToString(item.Style),
                                            intervalMaxValue = Convert.ToDouble(item.Range)
                                        };

                gradThemeItems.Add(gradThemeItem);
            }

            var themeGradient = new themeGradient
                                    {
                                        gradientThemeItems = gradThemeItems.ToArray(),
                                        columnName = theme.ColumnName,
                                        minValue = theme.Min,
                                        maxValue = theme.Max,
                                        minStyle = minStyle,
                                        maxStyle = maxStyle,
                                        noDataValues = GetNoDataValues(theme.NoDataValues),
                                        noDataValueType = GetNoDataValueType(theme.NoDataValues),

                                        // Color blends
                                        textColorBlends = (theme.TextColorBlend != null)
                                                ? CreateBlendsFromTheme(theme.TextColorBlend)
                                                : null,
                                        lineColorBlends = (theme.LineColorBlend != null)
                                                ? CreateBlendsFromTheme(theme.LineColorBlend)
                                                : null,
                                        fillColorBlends = (theme.FillColorBlend != null)
                                                ? CreateBlendsFromTheme(theme.FillColorBlend)
                                                : null
                                    };
            return themeGradient;
        }

        private static themeCategory GetThemeCategorial(CategorialTheme theme)
        {
            var themeCategory = new themeCategory
                                    {
                                        columnName = theme.AttributeName,
                                        defaultStyle = GetDefaultStyle(theme)
                                    };

            var catThemeItems = new List<themeItem>();

            foreach (var item in theme.ThemeItems)
            {
                // Add theme items (style and label) to the CategorialTheme
                var catThemeItem = new themeItem
                                       {
                                           label = item.Label, 
                                           style = GetStyle(item)
                                       };

                catThemeItems.Add(catThemeItem);
            }

            themeCategory.categoryThemeItems = catThemeItems.ToArray();
            return themeCategory;
        }

        private static themeCustom GetThemeCustom(CustomTheme theme)
        {
            string defaultStyle = GetDefaultStyle(theme);
            return new themeCustom {defaultStyle = defaultStyle};
        }

        private static string GetStyle(IThemeItem themeItem)
        {
            return GetStyleConverter().ConvertToString(themeItem.Style);
        }

        private static IStyle GetStyle(themeItem themeItem)
        {
            return (IStyle) GetStyleConverter().ConvertFrom(themeItem.style);
        }

        private static string GetDefaultStyle(ITheme theme)
        {
            IStyle style;

            switch (GetThemeType(theme))
            {
                case ThemeType.Custom:
                    style = ((CustomTheme)theme).DefaultStyle;
                    break;
                case ThemeType.Categorial:
                    style = ((CategorialTheme)theme).DefaultStyle;
                    break;
                case ThemeType.Gradient:
                    style = null;
                    break;
                case ThemeType.Quantity:
                    style = ((QuantityTheme)theme).DefaultStyle;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
             
            return (style != null)? GetStyleConverter().ConvertToString(style) : "";
        }

        private static IStyle GetDefaultStyle(theme theme)
        {
            string style = "";

            switch (GetThemeType(theme))
            {
                case ThemeType.Custom:
                    style = ((themeCustom)theme.Item).defaultStyle;
                    break;
                case ThemeType.Categorial:
                    style = ((themeCategory)theme.Item).defaultStyle;
                    break;
                case ThemeType.Gradient:
                    break;
                case ThemeType.Quantity:
                    style = ((themeQuantity)theme.Item).defaultStyle;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            return (style != string.Empty) ? (IStyle)GetStyleConverter().ConvertFrom(style) : null;
        }

        private static double[] GetNoDataValues(IList noDataValues)
        {
            if (noDataValues != null)
            {
                var noDataValuesAsDouble = new List<double>();

                foreach (var noDataValue in noDataValues)
                {
                    noDataValuesAsDouble.Add(float.Parse(noDataValue.ToString()));
                }

                return noDataValuesAsDouble.ToArray();
            }
            return null;
        }

        private static string GetNoDataValueType(IList noDataValues)
        {
            if (null == noDataValues)
            {
                return "";
            }
            return noDataValues.Count == 0 ? "" : noDataValues[0].GetType().ToString();
        }

        private static ThemeType GetThemeType(ITheme theme)
        {
            if (theme is CustomTheme) return ThemeType.Custom;
            if (theme is CategorialTheme) return ThemeType.Categorial;
            if (theme is GradientTheme) return ThemeType.Gradient;
            if (theme is QuantityTheme) return ThemeType.Quantity;

            return ThemeType.Custom;
        }

        private static ThemeType GetThemeType(theme theme)
        {
            var type = theme.Item.GetType();

            if (type.Equals(typeof(themeCustom))) return ThemeType.Custom;
            if (type.Equals(typeof(themeCategory))) return ThemeType.Categorial;
            if (type.Equals(typeof(themeGradient))) return ThemeType.Gradient;
            if (type.Equals(typeof(themeQuantity))) return ThemeType.Quantity;

            return ThemeType.Custom;
        }

        /// <summary>
        /// Creates a <see cref="ColorBlend"/> object (to use in a <see cref="GradientTheme"/>) from a list of XML/CSS-loaded colorBlends
        /// </summary>
        /// <param name="blends">List of color-position pairs stored in an XML/CSS-loaded object</param>
        /// <returns>New <see cref="ColorBlend"/> object with colors and positions</returns>
        private static ColorBlend createColorBlendForTheme(IEnumerable<colorBlend> blends)
        {
            var colors = new List<Color>();
            var positions = new List<float>();
            foreach (colorBlend blend in blends)
            {
                // FromHtml can parse names and hex codes to .NET color objects
                colors.Add(ColorTranslator.FromHtml(blend.color));
                positions.Add(blend.position);
            }
            return new ColorBlend(colors.ToArray(), positions.ToArray());
        }

        /// <summary>
        /// Build a list of CSS/XML-like <see cref="ColorBlend"/> objects from a <see cref="GradientTheme"/> <see cref="ColorBlend"/>
        /// </summary>
        /// <param name="colorBlend">The original <see cref="ColorBlend"/> with colors and positions</param>
        /// <returns>An array of object containing a color and position; to be used to store as XML</returns>
        private static colorBlend[] CreateBlendsFromTheme(ColorBlend colorBlend)
        {
            var fillColorBlends = new List<colorBlend>();
            for (int i = 0; i < colorBlend.Positions.Length; i++)
            {
                var fillBlend = new colorBlend();
                fillBlend.color = ColorTranslator.ToHtml(colorBlend.Colors[i]);
                fillBlend.position = colorBlend.Positions[i];
                fillColorBlends.Add(fillBlend);
            }
            return fillColorBlends.ToArray();
        }

        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
        {
            // Can convert from string
            return sourceType.Equals(typeof (string)) || base.CanConvertFrom(context, sourceType);
        }

        public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
        {
            // Can convert from string
            return destinationType.Equals(typeof (string)) || base.CanConvertTo(context, destinationType);
        }
    }
}