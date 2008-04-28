using SharpMap.Styles;

namespace SharpMap.Rendering.Thematics
{
    public interface IThemeable
    {
        ITheme Theme { get; set; }
    }

    public interface IThemeable<TStyle>
        : IThemeable
        where TStyle : IStyle
    {
        new ITheme<TStyle> Theme { get; set; }
    }
}
