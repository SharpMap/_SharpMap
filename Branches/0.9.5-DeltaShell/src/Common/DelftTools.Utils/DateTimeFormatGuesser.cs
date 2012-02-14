using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace DelftTools.Utils
{
    /// <summary>
    /// This static helper class tries to guess the datetime format, for example when parsing from a file. It
    /// returns the format itself, not the parsed datetime. This format can be used as an initial guess whenever 
    /// the user must specify the datetime format because the actual format is unknown. It's main method is
    /// TryGuessDateTimeFormat.
    /// 
    /// Should the guessing fail for what you believe is a common date time format, please add the format parts
    /// to the string arrays. Placeholder seperator in formats is '-' (the '-' will be substituted with the strings
    /// from the separator array). 
    /// </summary>
    public static class DateTimeFormatGuesser
    {
        private static readonly string[] LikelyTimeSeperators = {":", "."};
        private static readonly string[] LikelyDateSeperators = { "-", "/", " " };
        private static readonly string[] LikelyTimeFormats = {
                                                                 "hh-mm-ss", "HH-mm-ss", "h-mm-ss", "H-mm-ss", "hh-mm",
                                                                 "HH-mm", "h-mm", "H-mm"
                                                             };
        private static readonly string[] LikelyDateFormats = {
                                                                 "dd-MM-yyyy", "MM-dd-yyyy", "yyyy-MM-dd",
                                                                 "d-M-yyyy", "M-d-yyyy", "yyyy-M-d",
                                                                 "dd-MM-yy", "MM-dd-yy", "yy-MM-dd",
                                                                 "d-M-yy", "M-d-yy", "yy-M-d",
                                                             };

        public static bool TryGuessDateTimeFormat(string dateTimeString, out string outFormat)
        {
            foreach (var format in GetAllFormats(dateTimeString))
            {
                DateTime result;
                if (DateTime.TryParseExact(dateTimeString, format, CultureInfo.InvariantCulture, DateTimeStyles.None, out result))
                {
                    outFormat = format;
                    return true;
                }
            }

            outFormat = "dd-MM-yyyy hh:mm:ss"; //fallback
            return false;
        }

        private static IEnumerable<string> GetAllFormats(string dateTimeString)
        {
            var dateFirst = IsDateFirst(dateTimeString);

            var timePermutations = GeneratePermutations(LikelyTimeFormats, LikelyTimeSeperators);
            var datePermutations = GeneratePermutations(LikelyDateFormats, LikelyDateSeperators);

            foreach (var time in timePermutations)
            {
                foreach (var date in datePermutations)
                {
                    yield return dateFirst ? date + " " + time : time + " " + date;
                }
            }

            //formats without time component
            foreach (var date in datePermutations)
            {
                yield return date;
            }
        }

        private static IEnumerable<string> GeneratePermutations(IEnumerable<string> formats, IEnumerable<string> separators)
        {
            return from format in formats from sep in separators select format.Replace("-", sep);
        }

        private static bool IsDateFirst(string dateTimeString)
        {
            string[] parts = dateTimeString.Split(' ');

            foreach (var part in parts)
            {
                var currentPart = part;

                if (LikelyDateSeperators.Any(currentPart.Contains))
                {
                    return true;
                }
                if (LikelyTimeSeperators.Any(currentPart.Contains))
                {
                    return false;
                }
            }
            return true; //we don't know
        }
    }
}
