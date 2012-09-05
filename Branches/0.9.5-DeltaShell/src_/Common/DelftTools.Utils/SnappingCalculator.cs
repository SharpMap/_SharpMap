using System;
using System.Collections.Generic;
using System.Linq;

namespace DelftTools.Utils
{
    public static class SnappingCalculator // TODO: the class has very bad name, move it next to TimeSeriesNavigator
    {
        /// <summary>
        /// Get nearest value in times for value.
        /// </summary>
        /// <param name="times"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public static DateTime GetNearestDefinedTime(IEnumerable<DateTime> times, DateTime value)
        {
            
            //minimun
            var minDuration = times.Select(t=>(t - value).Duration()).Min();
            //selection the time with this minimum
            var q = from t in times
                    where (t - value).Duration()== minDuration
                    select t;


            return q.FirstOrDefault();
        }

        public static DateTime? GetLastTimeInRange(IEnumerable<DateTime> times, DateTime? start, DateTime? end)
        {
            var time = times.OrderByDescending(t=>t).FirstOrDefault(t => t >= start && t<=end);
            return time == default(DateTime) ? (DateTime?)null : time;
        }

        public static DateTime? GetFirstTimeInRange(IEnumerable<DateTime> times, DateTime? start, DateTime? end)
        {
            var time = times.OrderBy(t => t).FirstOrDefault(t => t >= start && t <= end);
            return time == default(DateTime) ? (DateTime?) null : time;
        }

        public static DateTime? GetFirstTimeLeftOfValue(IEnumerable<DateTime> times, DateTime? start)
        {
            var time = times.OrderByDescending(t => t).FirstOrDefault(t => t <= start);
            return time == default(DateTime) ? (DateTime?)null : time;
        }

        /// <summary>
        /// Returns starttime for this navigatable. Taking snapping into account
        /// </summary>
        /// <param name="timeNavigatable"></param>
        /// <param name="start">Start time of selected range</param>
        /// <param name="end">End time of selected range</param>
        /// <returns>Start time to send to navigatable. This might be a snapped value of <para>start</para></returns>
        public static DateTime GetStartTime(ITimeNavigatable timeNavigatable, DateTime start, DateTime? end)
        {
            return GetStartTime(timeNavigatable, timeNavigatable.SnappingMode, start, end);
        }

        /// <summary>
        /// Returns starttime for this navigatable. Taking snapping into account
        /// </summary>
        /// <param name="timeNavigatable"></param>
        /// <param name="mode">Custom snapping mode to use</param>
        /// <param name="start">Start time of selected range</param>
        /// <param name="end">End time of selected range</param>
        /// <returns>Start time to send to navigatable. This might be a snapped value of <para>start</para></returns>
        public static DateTime GetStartTime(ITimeNavigatable timeNavigatable, SnappingMode mode, DateTime start, DateTime? end)
        {
            //No snapping is easy
            if (mode == SnappingMode.None)
            {
                return start;
            }

            IEnumerable<DateTime> dateTimes = timeNavigatable.Times;

            DateTime? startTime;

            if (mode == SnappingMode.Interval)
            {
                startTime = GetFirstTimeLeftOfValue(dateTimes, start);
            }
            else
            {
                startTime = GetFirstTimeInRange(dateTimes, start, end);
            }
            return startTime != null ? startTime.Value : GetNearestDefinedTime(dateTimes, start);
        }

        /// <summary>
        /// Returns end for this navigatable. Taking snapping into account
        /// </summary>
        /// <param name="timeNavigatable"></param>
        /// <param name="start">Start time of selected range</param>
        /// <param name="end">End time of selected range</param>
        /// <returns>End time to send to navigatable. This might be a snapped value of <para>end</para></returns>
        public static DateTime GetEndTime(ITimeNavigatable timeNavigatable, DateTime start, DateTime end)
        {
            //No snapping is easy
            if (timeNavigatable.SnappingMode == SnappingMode.None)
            {
                return end;
            }
            //snapping stuff
            IEnumerable<DateTime> dateTimes = timeNavigatable.Times;

            DateTime? endTime;

            if (timeNavigatable.SnappingMode == SnappingMode.Interval)
            {
                endTime = GetFirstTimeLeftOfValue(dateTimes, end);
            }
            else
            {
                endTime = GetLastTimeInRange(dateTimes, start, end);
            }

            return endTime != null ? endTime.Value : GetNearestDefinedTime(dateTimes, end);
        }
    }
}