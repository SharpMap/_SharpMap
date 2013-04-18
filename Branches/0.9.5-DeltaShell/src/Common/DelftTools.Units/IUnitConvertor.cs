namespace DelftTools.Units
{
    /// <summary>
    /// Interface to convert values from one physical unit to another
    /// </summary>
    public interface IUnitConvertor
    {
        //T Convert<T>(T value, IUnit fromUnit, IUnit toUnit);
        ///<summary>
        /// Converts value from one unit to another
        ///</summary>
        ///<param name="value"></param>
        ///<param name="fromUnit"></param>
        ///<param name="toUnit"></param>
        ///<returns></returns>
        double Convert(double value, IUnit fromUnit, IUnit toUnit);
    }
}
