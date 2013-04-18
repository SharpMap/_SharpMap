namespace DelftTools.Units
{
    /// <summary>
    /// Transformed unit constructed using base unit with the next formula:
    /// 
    /// TransformedUnit = Multiplier . BaseUnit ^ Power + Offset
    /// 
    /// u1 - "m"
    /// tu1 - "km"
    /// 
    /// tu1.BaseUnit.Symbol = "m"
    /// tu1.Symbol = "km"
    /// 
    /// </summary>
    public interface ITransformedUnit : IUnit
    {
        IUnit BaseUnit { get; }
        double Power { get; }
        double Multiplier { get; }
        double Offset { get;}
    }
}
