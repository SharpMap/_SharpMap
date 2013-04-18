using System;
using DelftTools.Functions.Filters;
using DelftTools.Functions.Generic;
using DelftTools.Units;
using DelftTools.Utils.Globalization;
using log4net;

namespace DelftTools.Functions
{
    // can not seal this class due to NHibernate (laziness) but it should be treated that way!
    public class TimeSeries : Function,ITimeSeries
    {
        public TimeSeries()
        {
            //default time argument
            Arguments.Add(new Variable<DateTime>("date time") { Unit = new Unit(RegionalSettingsManager.DateTimeFormat, RegionalSettingsManager.DateTimeFormat) });
            Arguments[0].InterpolationType = InterpolationType.Linear;
            Arguments[0].ExtrapolationType = ExtrapolationType.Constant;
        }

        public virtual IVariable<DateTime> Time
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
        public virtual T Evaluate<T>(DateTime value)
        {
            return Evaluate<T>(new VariableValueFilter<DateTime>(Time, value));
        }
    }
}