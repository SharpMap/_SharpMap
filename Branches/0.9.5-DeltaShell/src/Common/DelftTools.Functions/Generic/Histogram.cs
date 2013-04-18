using System;
using System.Collections.Generic;
using System.Linq;

namespace DelftTools.DataObjects.Functions.Generic
{
    /// <summary>
    /// Defines closed interval, [from, to]
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class Interval<T> where T : IComparable<T>
    {
        public T From { get; set; }

        public T To { get; set; }
    }

    /// <summary>
    /// Defines a histogram on a set of values using predefined set of intervals. 
    /// It is actually an extension around dictionary of Interval to Frequency.
    /// Allows querying frequency by value.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface IHistogram<T> where T : IComparable<T>
    {
        void Update(IEnumerable<T> values, int intervalCount);

        void Update(IEnumerable<T> values, IEnumerable<Interval<T>> intervals);

        IDictionary<Interval<T>, long> Frequencies { get; set; }

        long GetFrequency(T value);
    }

    public class Histogram<T>: IHistogram<T> where T : IComparable<T>
    {
        public void Update(IEnumerable<T> values, int intervalCount)
        {
            throw new NotImplementedException();
        }

        public void Update(IEnumerable<T> values, IEnumerable<Interval<T>> intervals)
        {
            throw new NotImplementedException();
        }

        public IDictionary<Interval<T>, long> Frequencies
        {
            get; set;
        }

        public long GetFrequency(T value)
        {
            if(value.CompareTo(Frequencies.First().Key.From) < 0)
            {
                return 0; // outside of the histogram
            }

            var frequency = Frequencies.FirstOrDefault(kv => value.CompareTo(kv.Key.From) >= 0);

            if(value.CompareTo(frequency.Key.To) <= 0)
            {
                return frequency.Value;
            }

            return 0; // outside of the histogram
        }
    }
}