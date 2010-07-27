using System;

namespace DelftTools.Units
{
    /// <summary>
    /// Interface for physical units
    /// </summary>
    public interface IUnit : ICloneable
    {
        /// <summary>
        /// One of base dimensions for the current unit system.
        /// </summary>
        IDimension Dimension { get; }

        /// <summary>
        /// Meter
        /// </summary>
        string Name { get; }

        /// <summary>
        /// m, m3/s...
        /// </summary>
        string Symbol { get; }
        
        /// <summary>
        /// Gets convertor for specific
        /// </summary>
        /// <param name="unit"></param>
        /// <returns></returns>
        IUnitConvertor GetConvertor(IUnit unit);
    }
}
