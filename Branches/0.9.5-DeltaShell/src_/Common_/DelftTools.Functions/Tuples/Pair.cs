using System;
using System.Collections;
using System.Collections.Generic;

namespace DelftTools.Functions.Tuples
{
    /// <summary>
    /// also refer to DelftTools.DataObjects.Functions.Tuples.Tuple for another solution.
    /// </summary>
    /// <typeparam name="TFirst"></typeparam>
    /// <typeparam name="TSecond"></typeparam>
    public struct Pair<TFirst, TSecond> : IComparable 
        where TFirst : IComparable<TFirst>
        where TSecond : IComparable<TSecond>
    {
        private TFirst first;
        private TSecond second;

        public Pair(TFirst first, TSecond second)
        {
            this.first = first;
            this.second = second;
        }

        public IList<Type> ComponentTypes
        {
            get { return new[] { typeof(TFirst), typeof(TSecond) }; }
        }

        public IList Components
        {
            get { return new object[] { First, Second }; }
            set { throw new NotImplementedException(); }
        }

        public TFirst First
        {
            get { return first; }
            set { first = value; }
        }

        public TSecond Second
        {
            get { return second; }
            set { second = value; }
        }

        public int CompareTo(object obj)
        {
            var value = (Pair<TFirst, TSecond>)obj;

            int compare1 = value.First.CompareTo(First);

            if(compare1 == 0)
            {
                return value.Second.CompareTo(Second);
            }

            return compare1;
        }

        public override string ToString()
        {
            return string.Format("({0}, {1})", First, Second);
        }
    }
}