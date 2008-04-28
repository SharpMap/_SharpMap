namespace SharpMap.Rendering.Thematics
{
    public interface IGradientTheme<TStyle>
        : ITheme<TStyle>
     where TStyle : SharpMap.Styles.IStyle
    {
        string ColumnName { get; set; }
        new TStyle GetStyle(SharpMap.Data.FeatureDataRow row);
        double Max { get; set; }
        TStyle MaxStyle { get; set; }
        double Min { get; set; }
        TStyle MinStyle { get; set; }
    }
}
