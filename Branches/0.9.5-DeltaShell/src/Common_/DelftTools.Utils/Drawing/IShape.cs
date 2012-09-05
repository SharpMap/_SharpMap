using System.Drawing;

namespace DelftTools.Utils.Drawing
{
    public interface IShape
    {
        int Width { get; set; }
        int Height { get; set; }
        Color ColorFillSolid { get; set; }
        float BorderWidth { get; set; }
        Color BorderColor { get; set; }
        ShapeType ShapeType { get; set; }
        void Paint(Graphics graphics);
    }
}