using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using DelftTools.Utils.Aop.NotifyPropertyChange;
using DelftTools.Utils.Collections.Generic;
using GeoAPI.Extensions.Feature;
using GisSharpBlog.NetTopologySuite.Index.Bintree;
using NetTopologySuite.Extensions.Features;
using SharpMap.Styles;

namespace SharpMap.Rendering.Thematics
{
    /// <summary>
    /// Theme that divides the available data in classes, for which it uses a different style. 
    /// Can only be used for numerical attributes. 
    /// </summary>
    [NotifyPropertyChange(EnableLogging = false)]
    public class QuantityTheme : Theme
    {
        private string attributeName;
        private IStyle defaultStyle;
        private readonly IDictionary<Interval, Color> colorDictionary = new Dictionary<Interval, Color>();

        public QuantityTheme(string attributeName, IStyle defaultStyle)
        {
            this.attributeName = attributeName;
            this.defaultStyle = (IStyle) defaultStyle.Clone();
        }

        public override string AttributeName
        {
            get { return attributeName; }
            set { attributeName = value; }
        }

        public override IEventedList<IThemeItem> ThemeItems
        {
            get { return base.ThemeItems; }
            set 
            { 
                base.ThemeItems = value;
                colorDictionary.Clear();
            }
        }

        public override IStyle GetStyle(IFeature feature)
        {
            double attr;
            try
            {
                attr = FeatureAttributeAccessorHelper.GetAttributeValue<double>(feature, attributeName);
            }
            catch
            {
                throw new ApplicationException(
                    "Invalid Attribute type in Quantity Theme - Couldn't parse attribute (must be numerical)");
            }

            foreach (QuantityThemeItem quantityThemeItem in ThemeItems)
            {
                if (quantityThemeItem.Interval.Contains(attr))
                {
                    return quantityThemeItem.Style;
                }
            }

            return DefaultStyle;
        }

        public override IStyle GetStyle<T>(T attributeValue)
        {
            if (NoDataValues != null && NoDataValues.Contains(attributeValue))
            {
                return DefaultStyle;
            }

            foreach (QuantityThemeItem quantityThemeItem in ThemeItems)
            {
                if (typeof(T) == typeof(double))
                {
                    if (quantityThemeItem.Interval.Contains(Convert.ToDouble(attributeValue)))
                    {
                        return quantityThemeItem.Style;
                    }
                }
            }
            return DefaultStyle;
        }

        public IStyle DefaultStyle
        {
            get { return defaultStyle; }
            set { defaultStyle = value; }
        }

        public void AddStyle(IStyle style, Interval interval)
        {
            var quantityThemeItem = new QuantityThemeItem(interval, style);
            ThemeItems.Add(quantityThemeItem);
        }

        public override object Clone()
        {
            var quantityTheme = new QuantityTheme(attributeName, defaultStyle);

            foreach (QuantityThemeItem quantityThemeItem in ThemeItems)
            {
                quantityTheme.ThemeItems.Add((QuantityThemeItem)quantityThemeItem.Clone());
            }

            quantityTheme.DefaultStyle = (IStyle) defaultStyle.Clone();

            if (NoDataValues != null)
            {
                quantityTheme.NoDataValues = NoDataValues.Cast<object>().ToArray();
            }

            return quantityTheme;
        }

        public override Color GetFillColor<T>(T value)
        {
            if (noDataValues != null && noDataValues.Contains(value))
            {
                return Color.Transparent;
            }

            if(colorDictionary.Count == 0)
            {
                foreach (QuantityThemeItem themeItem in ThemeItems)
                {
                    colorDictionary.Add(themeItem.Interval,
                                                ((SolidBrush) ((VectorStyle) themeItem.Style).Fill).Color);
                }
            }

            var defaultKeyValuePair = new KeyValuePair<Interval, Color>(new Interval(),Color.Transparent);

            return colorDictionary.Where(c => c.Key.Contains(Convert.ToDouble(value)))
                                    .DefaultIfEmpty(defaultKeyValuePair)
                                    .FirstOrDefault()
                                    .Value;            
        }
    }
}
