using System.Drawing;
namespace SharpMap.Styles
{
    public interface IVectorStyle : IStyle
    {
        bool EnableOutline { get; set; }
        Brush Fill { get; set; }
        Pen Line { get; set; }
        Pen Outline { get; set; }
        Bitmap Symbol { get; set; }
        PointF SymbolOffset { get; set; }
        float SymbolRotation { get; set; }
        float SymbolScale { get; set; }
    }
}
