using System;
using System.Text.RegularExpressions;

namespace DelftTools.Utils.RegularExpressions
{
    public static class RegularExpression
    {
        public const string CharactersAndQuote = @"['A-Za-z0-9\s\(\)-\\/\.\+\<\>,\|_&;:\[\]]*";
        public const string Integer = @"[0-9-]*";
        public const string Float = @"[0-9\.-]*";
        public const string Characters = @"[#A-Za-z0-9\s\(\)-\\/\.\+\<\>,\|_&;:\[\]]*";
        // added support for extended ascii characters [192-259] À-ə
        public const string ExtendedCharacters = @"[#A-Za-zÀ-ə0-9\s\(\)-\\/\.\+\<\>,\|_&;:\[\]]*";
        public const string ExtendedCharactersAndQuote = @"['#A-Za-zÀ-ə0-9\s\(\)-\\/\.\+\<\>,\|_&;:\[\]]*";
        public const string AnyNonGreedy = @".*?";
        public const string Scientific = @"[0-9\.\-eE\+]*";
        public const string FileName = @"[#A-Za-zÀ-ə0-9\s\(\)-\\/\.\|_&~]*";
        
        public static Match GetFirstMatch(string pattern,string sourceText)
        {
            return GetMatches(pattern,sourceText)[0];
        }

        public static MatchCollection GetMatches(string pattern,string sourceText)
        {
            var regex = new Regex(pattern, RegexOptions.Singleline);

            MatchCollection matches = regex.Matches(sourceText);
            return matches;
        }

        public static string GetCharacters(string name)
        {
            return String.Format(@"{0}\s*'?(?<{0}>" + Characters + @")'?\s?", name);
        }

        public static string GetExtendedCharacters(string name)
        {
            return String.Format(@"{0}\s*'?(?<{0}>" + ExtendedCharacters + @")'?\s?", name);
        }

        /// <summary>
        /// parses integer variable optionally followed by a string : ' quote is required to avoid greediness
        /// </summary>
        /// <param name="integer"></param>
        /// <param name="characters"></param>
        /// <returns></returns>
        public static string GetIntegerOptionallyExtendedCharacters(string integer, string characters)
        {
            return String.Format(@"{0}\s*(?<{0}>" + Integer + @")\s?('(?<{1}>" + ExtendedCharacters + @")')?", 
                integer, characters);
        }

        public static string GetInteger(string name)
        {
            //return String.Format(@"{0}\s'?(?<{0}>" + Integer + @")'?\s?", name);
            return String.Format(@"{0}\s*'?(?<{0}>" + Integer + @")'?\s?", name);
        }

        // replaced \s with \s* to support multiple spaces
        public static string GetFloat(string name)
        {
            return String.Format(@"{0}\s*(?<{0}>" + Float + @")\s?", name);
        }

        public static string GetScientific(string name)
        {
            return String.Format(@"{0}\s*(?<{0}>" + Scientific + @")\s?", name);
        }

        public static string GetFloatOptional(string name)
        {
            return String.Format(@"({0}\s(?<{0}>" + Float + @")\s?)?", name);
        }

        public static string ParseFieldAsString(string field, string record)
        {
            var matches = GetMatches(GetCharacters(field), record);
            return matches.Count > 0 ? matches[0].Groups[field].Value : "";
        }
        
        public static string ParseFieldAsIntegerOrString(string field, string record)
        {
            var matches = GetMatches(GetInteger(field), record);
            if (matches.Count > 0)
            {
                return int.Parse(matches[0].Groups[field].Value).ToString();
            }
            else
            {
                return ParseFieldAsString(field, record);
            }
        }

        public static double ParseFieldAsDouble(string field, string record)
        {
            var matches = GetMatches(GetScientific(field), record);
            return matches.Count > 0 ? ConversionHelper.ToDouble(matches[0].Groups[field].Value) : 0.0;
        }

        public static int ParseFieldAsInt(string field, string record)
        {
            var matches = GetMatches(GetInteger(field), record);
            return matches.Count > 0 ? int.Parse(matches[0].Groups[field].Value) : 0;
        }

        /// <summary>
        /// Retrieves a float value if it is available in the math.
        /// example:
        ///  culvert.BedLevelRight = RegularExpression.ParseSingle(match, "rl", culvert.BedLevelRight);
        /// </summary>
        /// <param name="match"></param>
        /// <param name="field">name of the field </param>
        /// <param name="defaultValue">default value</param>
        /// <returns>Value of the field if found else default value</returns>
        public static float ParseSingle(Match match, string field, float defaultValue)
        {
            return match.Groups[field].Success ? ConversionHelper.ToSingle(match.Groups[field].Value) : defaultValue;
        }

        public static double ParseDouble(Match match, string field, double defaultValue)
        {
            return match.Groups[field].Success ? ConversionHelper.ToDouble(match.Groups[field].Value) : defaultValue;
        }


        /// <summary>
        /// Retrieves a float value if it is available in the math.
        /// example:
        ///  culvert.Direction = RegularExpression.ParseInt(match, "rt", culvert.Direction);
        /// </summary>
        /// <param name="match"></param>
        /// <param name="field">name of the field </param>
        /// <param name="defaultValue">default value</param>
        /// <returns>Value of the field if found else default value</returns>
        public static int ParseInt(Match match, string field, int defaultValue)
        {
            return match.Groups[field].Success ? Convert.ToInt32(match.Groups[field].Value) : defaultValue;
        }

        /// <summary>
        /// Retrieves a float value if it is available in the math.
        /// example:
        ///  culvert.CrossSectionId = RegularExpression.ParseString(match, "si", culvert.CrossSectionId);
        /// </summary>
        /// <param name="match"></param>
        /// <param name="field">name of the field </param>
        /// <param name="defaultValue">default value</param>
        /// <returns>Value of the field if found else default value</returns>
        public static string ParseString(Match match, string field, string defaultValue)
        {
            return match.Groups[field].Success ? match.Groups[field].Value : defaultValue;
        }
    }
}
