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
        public const string Scientific = @"[0-9\.\-e\+]*";
        
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
            return String.Format(@"{0}\s'?(?<{0}>" + Characters + @")'?\s?", name);
        }

        public static string GetInteger(string name)
        {
            return String.Format(@"{0}\s'?(?<{0}>" + Integer+ @")'?\s?", name);
        }

        public static string GetFloat(string name)
        {
            return String.Format(@"{0}\s(?<{0}>" + Float + @")\s?", name);
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

        public static double ParseFieldAsDouble(string field, string record)
        {
            var matches = GetMatches(GetFloat(field), record);
            return matches.Count > 0 ? ConversionHelper.ToDouble(matches[0].Groups[field].Value) : 0.0;
        }
    }
}
