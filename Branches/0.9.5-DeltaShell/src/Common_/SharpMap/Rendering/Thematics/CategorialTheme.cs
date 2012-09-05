using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using DelftTools.Utils.Aop.NotifyPropertyChanged;
using DelftTools.Utils.Collections.Generic;
using GeoAPI.Extensions.Feature;
using NetTopologySuite.Extensions.Features;
using SharpMap.Styles;

namespace SharpMap.Rendering.Thematics
{
    /// <summary>
    /// Theme that finds all distinct values for an attribute, for which it uses a different style. 
    /// Can be used for any attribute. 
    /// </summary>
    [NotifyPropertyChanged(EnableLogging = false)]
    public class CategorialTheme : Theme
    {
        private string attributeName;
        private IStyle defaultStyle;
        private readonly IDictionary<IComparable,Color> colorDictionary = new Dictionary<IComparable, Color>();

        public CategorialTheme()
        {
        }

        public CategorialTheme(string columnName, IStyle defaultStyle)
        {
            attributeName = columnName;
            this.defaultStyle = defaultStyle;
        }

        public string AttributeName
        {
            get { return attributeName; }
            set { attributeName = value; }
        }

        public IStyle DefaultStyle
        {
            get { return defaultStyle; }
            set
            {
                defaultStyle = value;
                colorDictionary.Clear();
            }
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
            string attr = FeatureAttributeAccessorHelper.GetAttributeValue<string>(feature, attributeName);
            if (attr != null)
            {
                foreach (CategorialThemeItem categorialThemeItem in ThemeItems)
                {
                    if (categorialThemeItem.Category.Equals(attr) || (categorialThemeItem.Value!=null&& categorialThemeItem.Value.ToString().Equals(attr)))
                    {
                        return categorialThemeItem.Style;
                    }
                }
            }
            return DefaultStyle;
        }

        public override IStyle GetStyle<T>(T value)
        {
            string attr = value.ToString();

            foreach (CategorialThemeItem categorialThemeItem in ThemeItems)
            {
                if (categorialThemeItem.Category.Equals(attr))
                {
                    return categorialThemeItem.Style;
                }
            }
            return DefaultStyle;
        }

        public void AddThemeItem(CategorialThemeItem categorialThemeItem)
        {
            ThemeItems.Add(categorialThemeItem);
        }

        public override object Clone()
        {
            var categorialTheme = new CategorialTheme(attributeName, defaultStyle);

            foreach (CategorialThemeItem categorialThemeItem in ThemeItems)
            {
                categorialTheme.ThemeItems.Add((CategorialThemeItem) categorialThemeItem.Clone());
            }
            categorialTheme.DefaultStyle = (IStyle) defaultStyle.Clone();
            
            if (NoDataValues != null)
            {
                categorialTheme.NoDataValues = NoDataValues.Cast<object>().ToArray();
            }

            return categorialTheme;
        }

        public override Color GetFillColor<T>(T value)
        {
            if (noDataValues != null && noDataValues.Contains(value))
            {
                return Color.Transparent;
            }

            if (colorDictionary.Count == 0)
            {
                foreach (CategorialThemeItem themeItem in ThemeItems)
                {
                    colorDictionary.Add((IComparable) themeItem.Value,
                                        ((SolidBrush) ((VectorStyle) themeItem.Style).Fill).Color);
                }
            }

            if (colorDictionary.ContainsKey(value))
            {
                return colorDictionary[value];
            }

            return Color.Transparent;
        }
    }
}
