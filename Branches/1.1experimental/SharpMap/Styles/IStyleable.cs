
namespace SharpMap.Styles
{
    public interface IStyleable
    {
        IStyle Style { get; set; }
    }

    public interface IStyleable<TStyle> : IStyleable where TStyle : IStyle
    {
        new TStyle Style { get; set; }
    }
}
