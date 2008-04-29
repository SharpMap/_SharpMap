using System.Drawing.Drawing2D;
using System.Drawing.Text;
using SharpMap.Rendering;
using SharpMap.Rendering.Thematics;
using SharpMap.Styles;
namespace SharpMap.Layers
{
    public interface ILabelLayer
        : IDataLayer,
        ITransformableLayer,
        IStyleable<ILabelStyle>,
        IThemeable<ILabelStyle>
    {
        string LabelColumn { get; set; }
        LabelCollisionDetection.LabelFilterMethod LabelFilter { get; set; }
        LabelLayer.GetLabelMethod LabelStringDelegate { get; set; }
        LabelLayer.MultipartGeometryBehaviourEnum MultipartGeometryBehaviour { get; set; }
        int Priority { get; set; }
        string RotationColumn { get; set; }
        SmoothingMode SmoothingMode { get; set; }
        TextRenderingHint TextRenderingHint { get; set; }
    }
}
