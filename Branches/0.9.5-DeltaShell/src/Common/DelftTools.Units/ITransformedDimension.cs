namespace DelftTools.Units
{
    public interface ITransformedDimension : IDimension
    {
        IDimension BaseDimension { get; }
        double Power { get; }
        double Multiplier { get; }
    }
}
