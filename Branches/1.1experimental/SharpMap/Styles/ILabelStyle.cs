using System.Drawing;
namespace SharpMap.Styles
{
    public interface ILabelStyle : IStyle
    {
        Brush BackColor { get; set; }
        SizeF CollisionBuffer { get; set; }
        bool CollisionDetection { get; set; }
        Font Font { get; set; }
        Color ForeColor { get; set; }
        Pen Halo { get; set; }
        LabelStyle.HorizontalAlignmentEnum HorizontalAlignment { get; set; }
        PointF Offset { get; set; }
        LabelStyle.VerticalAlignmentEnum VerticalAlignment { get; set; }
    }
}
