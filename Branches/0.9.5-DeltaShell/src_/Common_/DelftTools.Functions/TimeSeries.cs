using System;
using DelftTools.Functions.Filters;
using DelftTools.Functions.Generic;
using DelftTools.Units;
using log4net;

namespace DelftTools.Functions
{
    public class TimeSeries : Function,ITimeSeries
    {
        public TimeSeries()
        {
            //default time argument
            Arguments.Add(new Variable<DateTime>("date time") { Unit = new Unit("yyyy/MM/dd HH:mm:ss", "yyyy/MM/dd HH:mm:ss") });
        }

        public IVariable<DateTime> Time
        {
            get
            {
                return (IVariable<DateTime>) Arguments[0];
            }
        }

        /// <summary>
        /// Declare the logger
        /// </summary>
        private static readonly ILog Log = LogManager.GetLogger(typeof(TimeSeries));


        /// <summary>
        /// Gets the interpolated value at the given time step 
        /// </summary>
        public T Evaluate<T>(DateTime value) where T : IComparable
        {
            return Evaluate<T>(new VariableValueFilter<DateTime>(Time, value));
        }
    }
}