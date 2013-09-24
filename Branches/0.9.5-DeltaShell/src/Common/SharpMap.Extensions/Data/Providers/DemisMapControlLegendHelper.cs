using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using GeoAPI.Extensions.Coverages;
using GeoAPI.Geometries;
using Nini.Ini;
using SharpMap.Api;
using SharpMap.Extensions.Layers;
using SharpMap.Rendering.Thematics;
using SharpMap.Styles;
using GisSharpBlog.NetTopologySuite.Index.Bintree;

namespace SharpMap.Extensions.Data.Providers
{
    public class DemisMapControlLegendHelper
    {
        public static ITheme GenerateDefaultThemeFromFile(GdalRasterLayer layer)
        {
            if (!(layer.DataSource is GdalFeatureProvider))
                return null;

            string filename = (layer.DataSource as GdalFeatureProvider).Path;
            ITheme theme = null;

            if (filename != null)
                theme = ConvertLegendToTheme(filename, layer.Grid.Components[0].Name,
                                                                         layer.Grid);

            return theme;
        }

        /// <summary>
        /// Convert a DemisMapControl legend (.leg) file to an ITheme
        /// </summary>
        /// <remarks>
        /// returns null if legend file is not found
        /// </remarks>
        /// <param name="fileName">The name of the legfile or bil-/bsq-/bipfile</param>
        /// <returns>instance of ITheme</returns>
        public static ITheme ConvertLegendToTheme(string fileName, string columnName, IRegularGridCoverage grid)
        {
            string legendFileNameBase = null;
            string legendFileName = null;
            bool fileIsLegend = false;

            if (Path.GetExtension(fileName.ToLower()).Equals(".bil") ||
                Path.GetExtension(fileName.ToLower()).Equals(".bip") ||
                Path.GetExtension(fileName.ToLower()).Equals(".bsq"))
            {
                string pathName = Path.GetDirectoryName(fileName);
                string baseName = Path.GetFileNameWithoutExtension(fileName);
                legendFileNameBase = Path.Combine(pathName, baseName);
            }
            else
            {
                fileIsLegend = true;
                legendFileName = fileName;
            }

            if (legendFileNameBase != null)
            {
                foreach (string name in 
                    new string[]
                        {
                            legendFileNameBase + ".leg",
                            legendFileNameBase + ".Leg",
                            legendFileNameBase + ".LEG"
                        })
                {
                    if (File.Exists(name))
                    {
                        legendFileName = name;
                        break;
                    }
                }
            }

            Legend legend;
            if (legendFileName == null || (legend = ReadLegend(legendFileName)) == null)
            {
                if (fileIsLegend || grid == null)
                {
                    return null;
                }
                return CreateDefaultGradientTheme(columnName, GetMaxMinFromGridFile(grid));
            }

            legend.columnName = columnName;

            Ranges<double> doubleRanges = null;
            Ranges<int> intRanges = null;

            if (legend.doubles)
                doubleRanges = ParseRanges<double>(legend);
            else
                intRanges = ParseRanges<int>(legend);

            ITheme newTheme;
            if (intRanges != null && intRanges.IsClassification)
            {
                newTheme = CreateCategorialTheme(legend, intRanges);
            }
            else if (legend.doubles)
            {
                newTheme = CreateQuantityTheme<double>(legend, doubleRanges);
            }
            else
            {
                newTheme = CreateQuantityTheme<int>(legend, intRanges);
            }

            return newTheme;
        }

        private static Tuple<double, double> GetMaxMinFromGridFile(IRegularGridCoverage grid)
        {
            Tuple<double, double> tuple = new Tuple<double, double>();

            IList<double> values;
            if (grid.Components[0].ValueType == typeof(double))
            {
                values = grid.GetValues<double>();
            }
            else //convert
            {
                values = new List<double>();
                var nonConvertedValues = grid.GetValues();
                foreach (var obj in nonConvertedValues)
                {
                    values.Add(Convert.ToDouble(obj));    
                }
            }
            
                
            double maxValue = double.MinValue;
            double secondMaxValue = double.MinValue;
            double minValue = double.MaxValue;
            double secondMinValue = double.MaxValue;

            // we need to filter out any non-data values. So we look at two most extreme values
            // on either side, assuming that only one value (but perhaps one for positive and
            // one for negative) is used for non-data. We do this by looking at the difference
            // between the maximum found and the second maximum found, and comparing this to the
            // difference between the second maximum and the second minimum.
            foreach (double value in values)
            {
                maxValue = Math.Max(maxValue, value);
                minValue = Math.Min(minValue, value);
            }

            foreach (double value in values)
            {
                if (value < maxValue)
                    secondMaxValue = Math.Max(secondMaxValue, value);
                if (value > minValue)
                    secondMinValue = Math.Min(secondMinValue, value);
            }

            tuple.value1 = (Math.Abs(minValue - secondMinValue) > Math.Abs(secondMaxValue - secondMinValue))
                               ?
                                   secondMinValue
                               : minValue;
            tuple.value2 = (Math.Abs(maxValue - secondMaxValue) > Math.Abs(secondMaxValue - secondMinValue))
                               ?
                                   secondMaxValue
                               : maxValue;
            return tuple;
        }

        private static Legend ReadLegend(string fileName)
        {
            Legend legend = new Legend();

            legend.ranges = new List<string>();
            legend.dict = new Dictionary<string, string>();

            //FileStream fs = new FileStream(fileName, FileMode.Open);
            //StreamReader sr = new StreamReader(fs);

            StreamReader reader = new StreamReader(fileName);
            //specify inifiletype so full key is read
            IniDocument doc = new IniDocument(reader, IniFileType.WindowsStyle);

            legend.doubles = false;

            //string line;
            //bool headerDetected = false;

            IniSection zdataSection = doc.Sections["Z-data"];
            if (zdataSection != null)
            {
                string[] keys = zdataSection.GetKeys();
                foreach (string key in keys)
                {
                    string value = zdataSection.GetValue(key);
                    if (Regex.IsMatch(key, "^Range[0-9]+$"))
                    {
                        legend.ranges.Add(value);
                        if (value.Split(new char[] {','})[0].Contains("."))
                            legend.doubles = true;
                    }
                    else
                    {
                        legend.dict.Add(key, value);
                    }
                }
            }
            reader.Close();
            // match whitespace.word-of-any-chars-except-equals.equals.
            // word-of-any-chars-except-cr-or-lf.possibly-cr.lf-or-end-of-string
            // store in mem both word-of-any-chars-except-equals and word-of-any-chars-except-cr-or-lf
            //while ((line = sr.ReadLine()) != null)
            //{
            //    if (!headerDetected)
            //        if (line.Equals("[Z-data]"))
            //            headerDetected = true;
            //        else
            //            return null;
            //    else
            //    {
            //        string[] keyValue = line.Split(new char[] { '=' }, 2);
            //        if (Regex.IsMatch(keyValue[0], "^Range[0-9]+$"))
            //        {
            //            legend.ranges.Add(keyValue[1]);
            //            if (keyValue[1].Split(new char[] { ',' })[0].Contains("."))
            //                legend.doubles = true;
            //        }
            //        else
            //            legend.dict.Add(keyValue[0], keyValue[1].Replace("\"", ""));
            //    }
            //}
            //fs.Close();

            return legend;
        }

        private static ITheme CreateDefaultGradientTheme(string columnName, Tuple<double, double> tuple)
        {
            double minValue = tuple.value1;
            double maxValue = tuple.value2;
            string attribute = columnName;
            int numberOfClasses = 10;

            // use polygon for grids so style will be shown nicely in dialog
            var defaultStyle = new VectorStyle {GeometryType = typeof (IPolygon)};
            var blend = new ColorBlend(new Color[] {Color.Black, Color.White}, new float[] {0, 1});
            int size = (int)defaultStyle.SymbolScale;

            GradientTheme gradientTheme = ThemeFactory.CreateGradientTheme(attribute, defaultStyle,
                                                                           blend,
                                                                           (float) Convert.ChangeType(minValue, typeof (float)),
                                                                           (float) Convert.ChangeType(maxValue, typeof (float)),
                                                                           size,
                                                                           size,
                                                                           false,
                                                                           false);
            return gradientTheme;
        }

        private static ITheme CreateCategorialTheme(Legend legend, Ranges<int> ranges)
        {
            int numberOfClasses = ranges.ranges.Count; //legend.ranges.Count;
            Color[] colors = new Color[numberOfClasses];
            float[] positions = new float[numberOfClasses];
            List<IComparable> values = new List<IComparable>();
            string attribute = legend.columnName;
            VectorStyle defaultStyle = null;
            List<string> categories = new List<string>();

            int i = 0;

            foreach (Range<int> range in ranges.ranges)
            {
                categories.Add(range.description);
                colors[i] = range.color;
                positions[i] = ((float) i)/(numberOfClasses - 1);
                values.Add(range.intMaxValue);
                i++;
            }
            var blend = new ColorBlend(colors, positions);
            var theme = ThemeFactory.CreateCategorialTheme(attribute, defaultStyle, blend, numberOfClasses, values, categories, 1, 1);
            return theme;
        }

        private static ITheme CreateQuantityTheme<T>(Legend legend, Ranges<T> ranges)
        {
            QuantityTheme theme = new QuantityTheme(legend.columnName, new VectorStyle());
            foreach (Range<T> range in ranges.ranges)
            {
                Interval interval = null;
                if (typeof (T) == typeof (double))
                {
                    interval = new Interval(range.doubleMinValue, range.doubleMaxValue);
                }
                else
                {
                    interval = new Interval(range.intMinValue, range.intMaxValue);
                }
                VectorStyle style = new VectorStyle();
                style.Fill = new SolidBrush(range.color);
                
                QuantityThemeItem themeItem = new QuantityThemeItem(interval, style);
                theme.ThemeItems.Add(themeItem);
            }

            return theme;
        }

        private static Ranges<T> ParseRanges<T>(Legend legend)
        {
            Ranges<T> ranges = new Ranges<T>();
            ranges.ranges = new List<Range<T>>();

            // We want a unique style for each interval. So we use a temporary
            // dictionary for mapping colors to interval-max-values. We use a dictionary
            // because any additional setting of the same interval value overwrites the
            // previous one (we want to use the last one instead of the first one, which is
            // what happens when using a Set for example; that is if we would define a Range<T>
            // to be equal solely on the interval-value)
            IDictionary<int, Range<T>> intDict = new Dictionary<int, Range<T>>();
            IDictionary<double, Range<T>> doubleDict = new Dictionary<double, Range<T>>();

            // To make the ranges, we need the old max value as the min value for the next
            // range. For the first range this means we are using the minimum value of all
            // values as the 'old max'.. (In case of doubles)
            int oldIntMaxValue = (int) Double.Parse(legend.dict["MinValue"].Replace(".", CultureInfo.InvariantCulture.NumberFormat.NumberDecimalSeparator));
            double oldDoubleMaxValue = Double.Parse(legend.dict["MinValue"].Replace(".", CultureInfo.InvariantCulture.NumberFormat.NumberDecimalSeparator));

            foreach (string rangeString in legend.ranges)
            {
                string[] rangeSpec = rangeString.Split(new char[] {','});
                Range<T> range = new Range<T>();
                range.description = rangeSpec[2].Replace("\"", "");
                range.color = ParseHexStringAsColor(rangeSpec[1]);
                if (typeof (T) == typeof (int))
                {
                    range.intMaxValue = Int32.Parse(rangeSpec[0]);
                    intDict[range.intMaxValue] = range;
                }
                else if (typeof (T) == typeof (double))
                {
                    range.doubleMaxValue = Double.Parse(rangeSpec[0], CultureInfo.InvariantCulture);
                    doubleDict[range.doubleMaxValue] = range;
                }
            }

            if (typeof (T) == typeof (int))
            {
                foreach (int key in intDict.Keys)
                {
                    intDict[key].intMinValue = oldIntMaxValue;
                    oldIntMaxValue = intDict[key].intMaxValue;
                    ranges.ranges.Add(intDict[key]);
                }
            }
            else if (typeof (T) == typeof (double))
            {
                foreach (double key in doubleDict.Keys)
                {
                    doubleDict[key].doubleMinValue = oldDoubleMaxValue;
                    oldDoubleMaxValue = doubleDict[key].doubleMaxValue;
                    ranges.ranges.Add(doubleDict[key]);
                }
            }

            ranges.ranges.Sort(delegate(Range<T> item1, Range<T> item2)
                                   {
                                       if (typeof (T) == typeof (int))
                                           return item1.intMaxValue.CompareTo(item2.intMaxValue);
                                       else if (typeof (T) == typeof (double))
                                           return item1.doubleMaxValue.CompareTo(item2.doubleMaxValue);

                                       return 0;
                                   });

            return ranges;
        }

        private static Color ParseHexStringAsColor(string colorString)
        {
            StringBuilder colorSb = new StringBuilder();
            while ((colorString.Length + colorSb.Length) < 6)
            {
                colorSb.Append("0");
            }
            colorSb.Append(colorString);

            Color color = Color.FromArgb(
                ParseHexStringAsInt(colorSb.ToString().Substring(4, 2)),
                ParseHexStringAsInt(colorSb.ToString().Substring(2, 2)),
                ParseHexStringAsInt(colorSb.ToString().Substring(0, 2))
                );

            return color;
        }

        private static int ParseHexStringAsInt(string hexNumber)
        {
            List<string> hexDigits =
                new List<string>(new string[]
                                     {"0", "1", "2", "3", "4", "5", "6", "7", "8", "9", "A", "B", "C", "D", "E", "F"});

            int upperDigit = hexDigits.IndexOf(hexNumber.Substring(0, 1).ToUpper());
            int lowerDigit = hexDigits.IndexOf(hexNumber.Substring(1, 1).ToUpper());

            return upperDigit*16 + lowerDigit;
        }

        internal class Tuple<T, U>
        {
            public T value1;
            public U value2;
        }

        // TODO: refactor to nice field names (_Ranges, etc) and property usage
        internal class Legend
        {
            public IList<string> ranges;
            public Dictionary<string, string> dict;
            public bool doubles;
            public string columnName;
        }

        // TODO: refactor to nice field names and property usage
        internal class Setting<T>
        {
            public string name;
            public T value;
        }

        // TODO: refactor to nice field names and property usage
        internal class Ranges<T>
        {
            // List is needed for sorting, IList doesn't export Sort method
            public List<Range<T>> ranges;

            public bool IsClassification
            {
                get
                {
                    bool isClassification = true;

                    foreach (Range<T> range in ranges)
                    {
                        isClassification &= range.IsClassificationInt;
                    }

                    return isClassification;
                }
            }
        }

        // TODO: refactor to nice field names and property usage
        internal class Range<T>
        {
            public Color color;
            public string description;
            // TODO: change to maxValue field of type T. But how do I convert the result of
            // Int32.Parse or Double.Parse to type T?
            public int intMinValue;
            public int intMaxValue;
            public double doubleMaxValue;
            public double doubleMinValue;

            public bool IsClassificationInt
            {
                get { return typeof (T) == typeof (int) && intMaxValue - intMinValue <= 1; }
            }
        }
    }
}