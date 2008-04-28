using SharpMap.Styles;
namespace SharpMap.Rendering.Thematics
{
    public interface IGradientThemeGdi<TStyle> : IGradientTheme<TStyle>
          where TStyle : IStyle
    {
        ColorBlend FillColorBlend { get; set; }
        ColorBlend LineColorBlend { get; set; }
    }
}
