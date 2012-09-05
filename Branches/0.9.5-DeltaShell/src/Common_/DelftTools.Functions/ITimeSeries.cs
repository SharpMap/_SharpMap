using System;
using DelftTools.Functions.Generic;

namespace DelftTools.Functions
{
    public interface ITimeSeries: IFunction
    {
        /// <summary>
        /// Gets the interpolated value at the given time step 
        /// </summary>
        /// <typeparam name="T">The type of the value</typeparam>
        /// <param name="timeStep">The time step argument to use in the function</param>
        /// <returns>The (interpolated) value</returns>
        /// <remarks>
        /// Logs exceptions to the logger
        /// </remarks>
        T Evaluate<T>(DateTime timeStep) where T: IComparable;

        IVariable<DateTime> Time { get; }
    }
}